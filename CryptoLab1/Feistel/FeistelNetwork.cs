using CryptoLab1.Core;

namespace CryptoLab1.Feistel;

/// <summary>
/// Задание 3. Классическая сеть Фейстеля. Конструктор принимает реализации
/// интерфейсов 2.1 (расширение ключа) и 2.2 (раундовая функция) — конкретные
/// алгоритмы (DES, DEAL) получаются подстановкой своих реализаций.
/// </summary>
public class FeistelNetwork : ISymmetricCipher
{
    private readonly IKeyExpansion _keyExpansion;
    private readonly IRoundFunction _roundFunction;
    private readonly int _rounds;
    private readonly int _blockSize;

    private byte[][] _roundKeys = Array.Empty<byte[]>();
    private byte[][] _roundKeysReversed = Array.Empty<byte[]>();

    public FeistelNetwork(IKeyExpansion keyExpansion, IRoundFunction roundFunction, int rounds, int blockSize)
    {
        _keyExpansion = keyExpansion ?? throw new ArgumentNullException(nameof(keyExpansion));
        _roundFunction = roundFunction ?? throw new ArgumentNullException(nameof(roundFunction));
        if (rounds <= 0) throw new ArgumentException("Число раундов должно быть положительным.");
        if (blockSize <= 0 || blockSize % 2 != 0)
            throw new ArgumentException("Размер блока должен быть чётным числом байтов (половины L и R).");
        _rounds = rounds;
        _blockSize = blockSize;
    }

    public int BlockSize => _blockSize;
    public int Rounds => _rounds;

    public void SetKey(byte[] key)
    {
        byte[][] roundKeys = _keyExpansion.Expand(key ?? throw new ArgumentNullException(nameof(key)));
        if (roundKeys.Length != _rounds)
            throw new InvalidOperationException(
                $"Процедура расширения ключа вернула {roundKeys.Length} раундовых ключей, ожидалось {_rounds}.");
        foreach (byte[] roundKey in roundKeys)
            if (roundKey.Length == 0)
                throw new InvalidOperationException("Раундовый ключ не может быть пустым.");

        _roundKeys = roundKeys;
        _roundKeysReversed = roundKeys.Reverse().ToArray();
    }

    public byte[] EncryptBlock(byte[] block) => ProcessBlock(block, _roundKeys);

    public byte[] DecryptBlock(byte[] block) => ProcessBlock(block, _roundKeysReversed);

    /// <summary>
    /// Раунды: L_{i} = R_{i-1}, R_{i} = L_{i-1} XOR F(R_{i-1}, K_i).
    /// После последнего раунда половины меняются местами — именно это делает сеть
    /// самообратимой: дешифрование отличается только обратным порядком ключей.
    /// </summary>
    private byte[] ProcessBlock(byte[] block, byte[][] keys)
    {
        ValidateBlock(block);
        EnsureKeySet();

        int half = _blockSize / 2;
        byte[] left = Slice(block, 0, half);
        byte[] right = Slice(block, half, half);

        for (int round = 0; round < _rounds; round++)
        {
            byte[] fValue = _roundFunction.Transform(right, keys[round]);
            byte[] newRight = BitOperations.Xor(left, fValue);
            left = right;
            right = newRight;
        }

        // обмен половин после последнего раунда: результат = R_n || L_n
        byte[] result = new byte[_blockSize];
        Array.Copy(right, 0, result, 0, half);
        Array.Copy(left, 0, result, half, half);
        return result;
    }

    private void EnsureKeySet()
    {
        if (_roundKeys.Length == 0)
            throw new InvalidOperationException("Ключ не задан: сначала вызовите SetKey.");
    }

    private void ValidateBlock(byte[] block)
    {
        if (block == null) throw new ArgumentNullException(nameof(block));
        if (block.Length != _blockSize)
            throw new ArgumentException($"Размер блока должен быть {_blockSize} байт, получено {block.Length}.");
    }

    private static byte[] Slice(byte[] data, int start, int length)
    {
        byte[] part = new byte[length];
        Array.Copy(data, start, part, 0, length);
        return part;
    }
}