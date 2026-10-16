namespace CryptoLab1.Core;

/// <summary>
/// Задание 2.4. Контекст выполнения симметричного криптографического алгоритма:
/// реализация ISymmetricCipher (п. 2.3) + один из режимов шифрования + один из
/// режимов набивки. Шифрование/дешифрование данных произвольной длины (массив
/// байтов или файл), асинхронно, с распараллеливанием там, где блоки независимы.
/// </summary>
public class CipherContext
{
    private readonly ISymmetricCipher _cipher;
    private readonly BlockCipherMode _mode;
    private readonly BlockPaddingMode _padding;
    private readonly int _blockSize;
    private readonly byte[]? _iv;    // вектор инициализации (CBC, PCBC, CFB, OFB, CTR, RandomDelta)
    private readonly byte[]? _delta; // шаг счётчика для режима RandomDelta

    public CipherContext(
        ISymmetricCipher cipher,
        byte[] key,
        BlockCipherMode mode,
        BlockPaddingMode padding,
        byte[]? iv = null,
        params object[] extraParams)
    {
        _cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
        _mode = mode;
        _padding = padding;
        _blockSize = _cipher.BlockSize;
        _cipher.SetKey(key ?? throw new ArgumentNullException(nameof(key)));

        bool ivRequired = mode is BlockCipherMode.CBC or BlockCipherMode.PCBC or BlockCipherMode.CFB
                                 or BlockCipherMode.OFB or BlockCipherMode.CTR or BlockCipherMode.RandomDelta;
        if (ivRequired)
            _iv = iv is { Length: > 0 } ? CheckIv(iv) : RandomBlock(); // не задан — генерируем
        else if (iv is { Length: > 0 })
            throw new ArgumentException("Режиму ECB вектор инициализации не требуется.");

        if (mode == BlockCipherMode.RandomDelta)
        {
            // шаг можно передать дополнительным параметром (byte[] длиной с блок), иначе — случайный;
            // при шифровании он сохраняется в заголовке шифртекста
            _delta = (extraParams ?? Array.Empty<object>())
                .OfType<byte[]>()
                .FirstOrDefault(p => p.Length == _blockSize)
                ?? RandomBlock();
        }
    }

    // v v v Перегрузка из задания: данные + ссылка на результирующий массив v v v

    public void Encrypt(byte[] data, out byte[] result) => result = Encrypt(data);
    public void Decrypt(byte[] data, out byte[] result) => result = Decrypt(data);

    public byte[] Encrypt(byte[] data)
    {
        byte[] padded = Padding.Apply(data ?? throw new ArgumentNullException(nameof(data)), _blockSize, _padding);
        byte[] body = TransformBlocks(padded, true, _iv!, _delta);
        // RandomDelta: IV и delta идут в заголовке, чтобы дешифрование было самодостаточным
        return _mode == BlockCipherMode.RandomDelta ? Concat(_iv!, _delta!, body) : body;
    }

    public byte[] Decrypt(byte[] data)
    {
        byte[] payload = data ?? throw new ArgumentNullException(nameof(data));
        byte[] iv = _iv!;
        byte[]? delta = _delta;

        if (_mode == BlockCipherMode.RandomDelta)
        {
            if (payload.Length < 2 * _blockSize)
                throw new InvalidOperationException("Шифртекст RandomDelta должен содержать заголовок IV + delta.");
            iv = Slice(payload, 0, _blockSize);
            delta = Slice(payload, _blockSize, _blockSize);
            payload = Slice(payload, 2 * _blockSize, payload.Length - 2 * _blockSize);
        }

        if (payload.Length % _blockSize != 0)
            throw new InvalidOperationException($"Длина шифртекста ({payload.Length}) не кратна размеру блока ({_blockSize}).");

        byte[] padded = TransformBlocks(payload, false, iv, delta);
        return Padding.Remove(padded, _blockSize, _padding);
    }

    // v v v Перегрузка из задания: входной файл + файл с результатом v v v

    public void Encrypt(string inputPath, string outputPath) =>
        File.WriteAllBytes(outputPath, Encrypt(File.ReadAllBytes(inputPath)));

    public void Decrypt(string inputPath, string outputPath) =>
        File.WriteAllBytes(outputPath, Decrypt(File.ReadAllBytes(inputPath)));

    // v v v Асинхронные версии (в C# async-методы не могут иметь out-параметров, поэтому для массивов асинхронный метод возвращает Task<byte[]>) v v v

    public Task<byte[]> EncryptAsync(byte[] data) => Task.Run(() => Encrypt(data));
    public Task<byte[]> DecryptAsync(byte[] data) => Task.Run(() => Decrypt(data));
    public Task EncryptAsync(string inputPath, string outputPath) => Task.Run(() => Encrypt(inputPath, outputPath));
    public Task DecryptAsync(string inputPath, string outputPath) => Task.Run(() => Decrypt(inputPath, outputPath));

    // v v v Реализация режимов шифрования v v v

    private byte[] TransformBlocks(byte[] data, bool encrypting, byte[] iv, byte[]? delta)
    {
        int blocks = data.Length / _blockSize;
        byte[] output = new byte[data.Length];

        switch (_mode)
        {
            case BlockCipherMode.ECB:
            {
                // блоки независимы — распараллеливаем
                Parallel.For(0, blocks, i => WriteBlock(output, i, encrypting
                    ? _cipher.EncryptBlock(BlockAt(data, i))
                    : _cipher.DecryptBlock(BlockAt(data, i))));
                break;
            }

            case BlockCipherMode.CBC when encrypting:
            {
                byte[] previous = (byte[])iv.Clone();
                for (int i = 0; i < blocks; i++)
                {
                    byte[] cipherBlock = _cipher.EncryptBlock(BitOperations.Xor(BlockAt(data, i), previous));
                    WriteBlock(output, i, cipherBlock);
                    previous = cipherBlock;
                }
                break;
            }

            case BlockCipherMode.CBC:
            {
                // P_i = D(C_i) XOR C_{i-1}: дешифрования блоков независимы — распараллеливаем
                Parallel.For(0, blocks, i =>
                {
                    byte[] previous = i == 0 ? iv : BlockAt(data, i - 1);
                    WriteBlock(output, i, BitOperations.Xor(_cipher.DecryptBlock(BlockAt(data, i)), previous));
                });
                break;
            }

            case BlockCipherMode.PCBC:
            {
                // P_i зависит от P_{i-1} — строго последовательно в обе стороны
                byte[] previousCipher = (byte[])iv.Clone();
                byte[] previousPlain = new byte[_blockSize];
                for (int i = 0; i < blocks; i++)
                {
                    byte[] block = BlockAt(data, i);
                    if (encrypting)
                    {
                        byte[] cipherBlock = _cipher.EncryptBlock(
                            BitOperations.Xor(BitOperations.Xor(block, previousCipher), previousPlain));
                        WriteBlock(output, i, cipherBlock);
                        previousPlain = block;
                        previousCipher = cipherBlock;
                    }
                    else
                    {
                        byte[] plainBlock = BitOperations.Xor(
                            BitOperations.Xor(_cipher.DecryptBlock(block), previousCipher), previousPlain);
                        WriteBlock(output, i, plainBlock);
                        previousPlain = plainBlock;
                        previousCipher = block;
                    }
                }
                break;
            }

            case BlockCipherMode.CFB when encrypting:
            {
                byte[] previous = (byte[])iv.Clone();
                for (int i = 0; i < blocks; i++)
                {
                    byte[] cipherBlock = BitOperations.Xor(BlockAt(data, i), _cipher.EncryptBlock(previous));
                    WriteBlock(output, i, cipherBlock);
                    previous = cipherBlock;
                }
                break;
            }

            case BlockCipherMode.CFB:
            {
                // P_i = C_i ⊕ E(C_{i-1}): шифрования блоков независимы — распараллеливаем
                Parallel.For(0, blocks, i =>
                {
                    byte[] previous = i == 0 ? iv : BlockAt(data, i - 1);
                    WriteBlock(output, i, BitOperations.Xor(BlockAt(data, i), _cipher.EncryptBlock(previous)));
                });
                break;
            }

            case BlockCipherMode.OFB:
            {
                // гамма строго последовательна: O_1 = E(IV), O_{i+1} = E(O_i)
                byte[] gamma = _cipher.EncryptBlock(iv);
                for (int i = 0; i < blocks; i++)
                {
                    WriteBlock(output, i, BitOperations.Xor(BlockAt(data, i), gamma));
                    gamma = _cipher.EncryptBlock(gamma);
                }
                break;
            }

            case BlockCipherMode.CTR:
            {
                // C_i = P_i ⊕ E(IV + i): все блоки независимы — распараллеливаем
                Parallel.For(0, blocks, i =>
                {
                    byte[] keystream = _cipher.EncryptBlock(CounterAt(iv, i));
                    WriteBlock(output, i, BitOperations.Xor(BlockAt(data, i), keystream));
                });
                break;
            }

            case BlockCipherMode.RandomDelta:
            {
                // CTR со случайным шагом: счётчик_i = IV + i·delta (по модулю 2^(8·размер_блока))
                byte[][] counters = new byte[blocks][];
                if (blocks > 0)
                {
                    counters[0] = (byte[])iv.Clone();
                    for (int i = 1; i < blocks; i++)
                        counters[i] = AddBytes(counters[i - 1], delta!);
                }
                Parallel.For(0, blocks, i =>
                {
                    byte[] keystream = _cipher.EncryptBlock(counters[i]);
                    WriteBlock(output, i, BitOperations.Xor(BlockAt(data, i), keystream));
                });
                break;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(_mode), _mode, "Неизвестный режим шифрования.");
        }
        return output;
    }

    // v v v Вспомогательные методы v v v

    private byte[] CheckIv(byte[] iv)
    {
        if (iv.Length != _blockSize)
            throw new ArgumentException($"Вектор инициализации должен иметь длину {_blockSize} байт.");
        return (byte[])iv.Clone();
    }

    private byte[] RandomBlock()
    {
        byte[] block = new byte[_blockSize];
        SimpleRandom.NextBytes(block);
        return block;
    }

    private byte[] BlockAt(byte[] data, int index)
    {
        byte[] block = new byte[_blockSize];
        Array.Copy(data, index * _blockSize, block, 0, _blockSize);
        return block;
    }

    private static void WriteBlock(byte[] output, int index, byte[] block) =>
        Array.Copy(block, 0, output, index * block.Length, block.Length);

    private static byte[] Slice(byte[] data, int start, int length)
    {
        byte[] part = new byte[length];
        Array.Copy(data, start, part, 0, length);
        return part;
    }

    private static byte[] Concat(params byte[][] arrays)
    {
        int total = arrays.Sum(a => a.Length);
        byte[] result = new byte[total];
        int offset = 0;
        foreach (byte[] array in arrays)
        {
            Array.Copy(array, 0, result, offset, array.Length);
            offset += array.Length;
        }
        return result;
    }

    /// <summary>Счётчик = IV + index (big-endian, переполнение по модулю 2^(8·длина)).</summary>
    private static byte[] CounterAt(byte[] iv, long index)
    {
        byte[] counter = (byte[])iv.Clone();
        long carry = index;
        for (int i = counter.Length - 1; i >= 0 && carry > 0; i--)
        {
            long sum = counter[i] + (carry & 0xFF);
            counter[i] = (byte)sum;
            carry = (carry >> 8) + (sum >> 8);
        }
        return counter;
    }

    /// <summary>a + b по модулю 2^(8·len).</summary>
    private static byte[] AddBytes(byte[] a, byte[] b)
    {
        byte[] result = new byte[a.Length];
        int carry = 0;
        for (int i = a.Length - 1; i >= 0; i--)
        {
            int sum = a[i] + b[i] + carry;
            result[i] = (byte)sum;
            carry = sum >> 8;
        }
        return result;
    }
}