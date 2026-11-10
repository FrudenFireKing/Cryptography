using System.Numerics;
using CryptoLab2.Common;
using CryptoLab2.NumberTheory;
using CryptoLab2.Primality;

namespace CryptoLab2.Rsa;

/// <summary>
/// Задание 3. Объектный сервис шифрования и дешифрования данных алгоритмом RSA.
/// Содержит вложенный (nested) сервис <see cref="KeyGenerationService"/> для генерации
/// ключей и вложенное перечисление <see cref="PrimalityTestType"/> выбора вероятностного
/// теста простоты. Параметры конструктора обёртки делегируются в конструктор вложенного сервиса.
/// </summary>
/// <remarks>
/// Формат шифртекста: [4 байта исходной длины (big-endian)] + [блоки по keyCipherBytes],
/// где каждый блок — результат возведения в степень e модуля n одного блока открытого
/// текста длиной ≤ (bitLen(n) − 1)/8 байт.
/// </remarks>
public sealed class RsaCipherService
{
    private readonly KeyGenerationService _keyGenerationService;
    private readonly INumberTheoryService _numberTheory = new NumberTheoryService();

    public RsaCipherService(
        PrimalityTestType primalityTestType,
        double minPrimalityProbability,
        int primeBitLength)
    {
        // параметры делегируются из конструктора обёртки в конструктор вложенного сервиса
        _keyGenerationService = new KeyGenerationService(primalityTestType, minPrimalityProbability, primeBitLength);
    }

    /// <summary>Тип вероятностного теста простоты, применяемого при генерации ключей (nested enum).</summary>
    public enum PrimalityTestType
    {
        Fermat,
        SolovayStrassen,
        MillerRabin
    }

    /// <summary>
    /// Задание 3. Вложенный (nested) сервис генерации ключей RSA.
    /// Контракт конструктора: используемый тест простоты (nested-перечисление обёртки),
    /// минимальная вероятность простоты в [0.5, 1), битовая длина генерируемых простых чисел.
    /// </summary>
    public sealed class KeyGenerationService
    {
        private static readonly int[] SmallPrimes =
        {
            3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73,
            79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157,
            163, 167, 173, 179, 181, 191, 193, 197, 199, 211, 223, 227, 229, 233, 239, 241, 251
        };

        private readonly PrimalityTestType _primalityTestType;
        private readonly double _minPrimalityProbability;
        private readonly int _primeBitLength;
        private readonly INumberTheoryService _numberTheory;
        private readonly IProbabilisticPrimalityTest _primalityTest;

        public KeyGenerationService(
            PrimalityTestType primalityTestType,
            double minPrimalityProbability,
            int primeBitLength)
        {
            if (minPrimalityProbability < 0.5 || minPrimalityProbability >= 1.0)
                throw new ArgumentOutOfRangeException(nameof(minPrimalityProbability),
                    "Минимальная вероятность простоты должна лежать в [0.5, 1).");
            if (primeBitLength < 16)
                throw new ArgumentOutOfRangeException(nameof(primeBitLength),
                    "Битовая длина простых должна быть не меньше 16 (для практики рекомендуется ≥ 512).");

            _primalityTestType = primalityTestType;
            _minPrimalityProbability = minPrimalityProbability;
            _primeBitLength = primeBitLength;
            _numberTheory = new NumberTheoryService();
            _primalityTest = primalityTestType switch
            {
                PrimalityTestType.Fermat => new FermatPrimalityTest(_numberTheory),
                PrimalityTestType.SolovayStrassen => new SolovayStrassenPrimalityTest(_numberTheory),
                PrimalityTestType.MillerRabin => new MillerRabinPrimalityTest(_numberTheory),
                _ => throw new ArgumentOutOfRangeException(nameof(primalityTestType))
            };
        }

        /// <summary>
        /// Генерирует новую ключевую пару RSA. Может вызываться произвольное число раз.
        /// Обеспечивает неприменимость атаки Ферма (|p − q| больше порога) и атаки Винера
        /// (d больше n^(1/4)/3).
        /// </summary>
        public RsaKeyPair GenerateKeyPair()
        {
            const int maxAttempts = 1000;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                BigInteger p = GeneratePrime();
                BigInteger q = GeneratePrime();
                if (p == q) continue;

                // Защита от атаки Ферма. Число итераций метода Ферма равно
                // (p+q)/2 - ceil(sqrt(n)) ~= (p-q)^2 / (8*sqrt(n)). При |p-q| = O(n^(1/4)) =
                // = O(2^(b/2)) атака полиномиальна (константное число итераций), поэтому
                // требуется заведомо больший зазор: |p - q| > 2^(3b/4) гарантирует
                // (p-q)^2/(8*sqrt(n)) > 2^(b/2 - 3) итераций — экспоненциальный запас.
                BigInteger fermatThreshold = BigInteger.One << (3 * _primeBitLength / 4);
                if (BigInteger.Abs(p - q) <= fermatThreshold) continue;

                BigInteger n = p * q;
                BigInteger phi = (p - 1) * (q - 1);
                BigInteger lambda = LeastCommonMultiple(p - 1, q - 1);

                BigInteger e = SelectPublicExponent(lambda);

                // Классическая схема: e·d ≡ 1 (mod φ(n)) — это делает применимой и теорию,
                // и атаку Винера в её стандартной форме (e·d − 1 = k·φ). Функция Кармайкла λ(n)
                // сохраняется в закрытом ключе справочно (с ней d был бы ещё меньше).
                BigInteger d = _numberTheory.ModularInverse(e, phi);

                // Защита от атаки Винера: при d < n^(1/4)/3 закрытая экспонента восстанавливается
                // из непрерывной дроби e/n. Для «нормальных» ключей d ≈ λ(n) ≈ n, поэтому
                // проверка почти всегда истинна, но она включена явно, как требует задание.
                if (3 * d <= _numberTheory.IntegerRoot(n, 4)) continue;

                return new RsaKeyPair(
                    new RsaPublicKey(n, e),
                    new RsaPrivateKey(n, e, d, p, q, phi, lambda));
            }

            throw new InvalidOperationException("Не удалось сгенерировать ключевую пару за отведённое число попыток.");
        }

        private BigInteger GeneratePrime()
        {
            while (true)
            {
                // кандидат с гарантированной битовой длиной и нечётный
                BigInteger candidate = PseudoRandom.RandomBits(_primeBitLength, setHighBit: true, setLowBit: true);

                if (IsDivisibleBySmallPrime(candidate)) continue; // быстрая фильтрация

                if (_primalityTest.IsProbablyPrime(candidate, _minPrimalityProbability))
                    return candidate;
            }
        }

        private static bool IsDivisibleBySmallPrime(BigInteger candidate)
        {
            foreach (int prime in SmallPrimes)
                if (candidate % prime == 0)
                    return true;
            return false;
        }

        private BigInteger SelectPublicExponent(BigInteger lambda)
        {
            // классический выбор 65537 = 2^16 + 1 (простое число Ферма F4)
            if (lambda > 65537 && _numberTheory.GcdEuclid(65537, lambda) == 1)
                return 65537;

            for (BigInteger e = 3; e < lambda; e += 2)
                if (_numberTheory.GcdEuclid(e, lambda) == 1)
                    return e;

            throw new InvalidOperationException("Не найдена открытая экспонента, взаимно простая с λ(n).");
        }

        private BigInteger LeastCommonMultiple(BigInteger a, BigInteger b) =>
            a / _numberTheory.GcdEuclid(a, b) * b;
    }

    /// <summary>Генерация новой ключевой пары (делегируется вложенному сервису).</summary>
    public RsaKeyPair GenerateKeyPair() => _keyGenerationService.GenerateKeyPair();

    /// <summary>Шифрование данных произвольной длины открытым ключом.</summary>
    public byte[] Encrypt(byte[] data, RsaPublicKey publicKey)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(publicKey);

        int modulusBitLength = GetBitLength(publicKey.Modulus);
        int blockDataBytes = (modulusBitLength - 1) / 8;  // длина блока открытого текста: значение блока < n
        int blockCipherBytes = (modulusBitLength + 7) / 8; // длина блока шифртекста
        int blockCount = (data.Length + blockDataBytes - 1) / blockDataBytes; // 0 и для пустых данных

        byte[] result = new byte[4 + blockCount * blockCipherBytes];
        WriteInt32BigEndian(result, 0, data.Length);

        for (int i = 0; i < blockCount; i++)
        {
            int offset = i * blockDataBytes;
            int chunkLength = Math.Min(blockDataBytes, data.Length - offset);
            byte[] chunk = new byte[chunkLength];
            Array.Copy(data, offset, chunk, 0, chunkLength);

            BigInteger message = FromBigEndian(chunk);                    // m < n гарантировано
            BigInteger cipher = _numberTheory.ModPow(message, publicKey.Exponent, publicKey.Modulus); // c = m^e mod n
            byte[] blockBytes = ToBigEndian(cipher, blockCipherBytes);

            Array.Copy(blockBytes, 0, result, 4 + i * blockCipherBytes, blockCipherBytes);
        }

        return result;
    }

    /// <summary>Дешифрование данных закрытым ключом (восстанавливает исходную длину из заголовка).</summary>
    public byte[] Decrypt(byte[] data, RsaPrivateKey privateKey)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(privateKey);
        if (data.Length < 4)
            throw new ArgumentException("Шифртекст должен содержать 4-байтовый заголовок длины.", nameof(data));

        int modulusBitLength = GetBitLength(privateKey.Modulus);
        int blockDataBytes = (modulusBitLength - 1) / 8;
        int blockCipherBytes = (modulusBitLength + 7) / 8;
        int originalLength = ReadInt32BigEndian(data, 0);
        int payloadLength = data.Length - 4;
        if (payloadLength % blockCipherBytes != 0)
            throw new ArgumentException("Длина блочной части шифртекста некорректна.", nameof(data));
        if (originalLength > payloadLength / blockCipherBytes * blockDataBytes)
            throw new ArgumentException("Заголовок длины не согласуется с размером шифртекста.", nameof(data));

        int blockCount = payloadLength / blockCipherBytes;
        byte[] result = new byte[originalLength];

        for (int i = 0; i < blockCount; i++)
        {
            int offset = 4 + i * blockCipherBytes;
            byte[] cipherBlock = new byte[blockCipherBytes];
            Array.Copy(data, offset, cipherBlock, 0, blockCipherBytes);

            BigInteger cipher = FromBigEndian(cipherBlock);
            BigInteger message = _numberTheory.ModPow(cipher, privateKey.DecryptionExponent, privateKey.Modulus); // m = c^d mod n
            byte[] blockBytes = ToBigEndian(message, blockDataBytes);

            // последний блок может быть неполным: исходный кусок соответствует ПОСЛЕДНИМ
            // chunkLength байтам big-endian представления (перед ними — ведущие нули)
            int chunkLength = Math.Min(blockDataBytes, originalLength - i * blockDataBytes);
            Array.Copy(blockBytes, blockDataBytes - chunkLength, result, i * blockDataBytes, chunkLength);
        }

        return result;
    }

    // ------------------------- Помощники формата блоков -------------------------

    internal static int GetBitLength(BigInteger value)
    {
        byte[] littleEndian = value.ToByteArray();
        int length = littleEndian.Length;
        if (length > 0 && littleEndian[length - 1] == 0) length--;
        int bits = (length - 1) * 8;
        byte top = littleEndian[length - 1];
        for (int bit = 7; bit >= 0; bit--)
        {
            if ((top & (1 << bit)) != 0)
            {
                bits += bit + 1;
                break;
            }
        }
        return bits;
    }

    /// <summary>big-endian представление неотрицательного value фиксированной длины (value < 2^(8·length)).</summary>
    internal static byte[] ToBigEndian(BigInteger value, int length)
    {
        byte[] littleEndian = value.ToByteArray();
        int len = littleEndian.Length;
        if (len > 0 && littleEndian[len - 1] == 0) len--; // знаковый ноль
        if (len > length)
            throw new InvalidOperationException("Значение не помещается в блок заданной длины.");

        byte[] result = new byte[length];
        for (int i = 0; i < len; i++) result[i] = littleEndian[i];
        Array.Reverse(result);
        return result;
    }

    /// <summary>Восстановление неотрицательного числа из big-endian представления.</summary>
    internal static BigInteger FromBigEndian(byte[] bigEndian)
    {
        byte[] littleEndian = new byte[bigEndian.Length + 1]; // знаковый 0 → гарантия положительности
        for (int i = 0; i < bigEndian.Length; i++)
            littleEndian[i] = bigEndian[bigEndian.Length - 1 - i];
        return new BigInteger(littleEndian);
    }

    private static void WriteInt32BigEndian(byte[] target, int offset, int value)
    {
        target[offset] = (byte)(value >> 24);
        target[offset + 1] = (byte)(value >> 16);
        target[offset + 2] = (byte)(value >> 8);
        target[offset + 3] = (byte)value;
    }

    private static int ReadInt32BigEndian(byte[] source, int offset) =>
        (source[offset] << 24) | (source[offset + 1] << 16) | (source[offset + 2] << 8) | source[offset + 3];
}
