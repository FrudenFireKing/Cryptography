using System.Numerics;

namespace CryptoLab2.Common;

/// <summary>
/// Собственный генератор псевдослучайных чисел (xorshift64*).
/// Применяется для выбора случайных оснований в вероятностных тестах простоты
/// и при генерации ключей RSA. Генератор не является криптографическим ГСЧ и не
/// относится к «реализациям алгоритмов защиты информации» — он лишь поставляет
/// случайные биты общего назначения, вся криптографическая логика реализована отдельно.
/// </summary>
public static class PseudoRandom
{
    private static readonly object Gate = new();
    private static ulong _state = ((ulong)Environment.TickCount64 << 17) ^ 0x9E3779B97F4A7C15UL | 1UL;

    /// <summary>Фиксирует состояние генератора — делает демонстрации воспроизводимыми.</summary>
    public static void SetSeed(ulong seed) => _state = seed | 1UL;

    /// <summary>Заполняет буфер псевдослучайными байтами.</summary>
    public static void NextBytes(byte[] buffer)
    {
        lock (Gate)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                _state ^= _state >> 12;
                _state ^= _state << 25;
                _state ^= _state >> 27;
                _state *= 0x2545F4914F6CDD1DUL;
                buffer[i] = (byte)(_state >> 56);
            }
        }
    }

    /// <summary>Случайное значение из диапазона [minInclusive, maxExclusive) методом отбрасывания.</summary>
    public static BigInteger NextInRange(BigInteger minInclusive, BigInteger maxExclusive)
    {
        BigInteger range = maxExclusive - minInclusive;
        if (range <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Верхняя граница должна быть больше нижней.");
        int byteCount = (GetBitLength(range) + 7) / 8;
        while (true)
        {
            byte[] random = new byte[byteCount];
            NextBytes(random);
            byte[] signed = new byte[byteCount + 1]; // старший (знаковый) байт = 0 → значение неотрицательное
            Array.Copy(random, signed, byteCount);
            BigInteger candidate = new BigInteger(signed);
            if (candidate < range)
                return minInclusive + candidate;
        }
    }

    /// <summary>
    /// Случайное значение ровно из <paramref name="bitLength"/> бит: биты выше заданной длины
    /// отбрасываются; при <paramref name="setHighBit"/> старший бит устанавливается в 1
    /// (значение попадает в [2^(bitLength−1), 2^bitLength)), при <paramref name="setLowBit"/>
    /// младший бит устанавливается в 1 (значение нечётно).
    /// </summary>
    public static BigInteger RandomBits(int bitLength, bool setHighBit, bool setLowBit)
    {
        if (bitLength < 2)
            throw new ArgumentOutOfRangeException(nameof(bitLength), "Требуется минимум 2 бита.");
        int byteCount = (bitLength + 7) / 8;
        byte[] random = new byte[byteCount];
        NextBytes(random);
        byte[] signed = new byte[byteCount + 1];
        Array.Copy(random, signed, byteCount);
        BigInteger value = new BigInteger(signed);
        value &= (BigInteger.One << bitLength) - 1; // сброс битов выше bitLength
        if (setHighBit) value |= BigInteger.One << (bitLength - 1);
        if (setLowBit) value |= BigInteger.One;
        return value;
    }

    /// <summary>Битовая длина положительного числа: позиция старшего единичного бита плюс один.</summary>
    private static int GetBitLength(BigInteger value)
    {
        byte[] littleEndian = value.ToByteArray(); // little-endian, дополнение до двух
        int length = littleEndian.Length;
        if (length > 0 && littleEndian[length - 1] == 0) length--; // убрать знаковый ноль

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
}
