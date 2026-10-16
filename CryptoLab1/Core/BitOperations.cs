namespace CryptoLab1.Core;

/// <summary>
/// Задание 1. Перестановка битов (P-блок) в переданном значении (массив байтов).
/// </summary>
public static class BitOperations
{
    /// <summary>
    /// Выполняет перестановку битов по правилу <paramref name="pBlock"/>:
    /// pBlock[i] — номер (в заданной системе индексации) бита исходного значения,
    /// который становится i-м битом результата.
    /// </summary>
    /// <param name="value">Значение для перестановки (массив байтов).</param>
    /// <param name="pBlock">Правило перестановки (P-блок).</param>
    /// <param name="indexing">Биты индексируются от младшего к старшему или наоборот.</param>
    /// <param name="start">Номер начального бита: 0 или 1.</param>
    public static byte[] PermuteBits(byte[] value, int[] pBlock, BitIndexing indexing, BitStartNumber start)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(pBlock);

        byte[] result = new byte[(pBlock.Length + 7) / 8];
        int firstOutputNumber = start == BitStartNumber.OneBased ? 1 : 0;

        for (int i = 0; i < pBlock.Length; i++)
        {
            bool bit = GetBit(value, pBlock[i], indexing, start);
            SetBit(result, i + firstOutputNumber, bit, indexing, start);
        }
        return result;
    }

    /// <summary>Возвращает бит под номером bitNumber согласно правилам индексации.</summary>
    public static bool GetBit(byte[] value, int bitNumber, BitIndexing indexing, BitStartNumber start)
    {
        ArgumentNullException.ThrowIfNull(value);
        int index = bitNumber - (start == BitStartNumber.OneBased ? 1 : 0);
        int totalBits = value.Length * 8;
        if (index < 0 || index >= totalBits)
            throw new ArgumentOutOfRangeException(nameof(bitNumber), $"Бит {bitNumber} вне диапазона [1..{totalBits}].");

        int byteIndex = index / 8;
        int bitInByte = index % 8;
        int mask = indexing == BitIndexing.LsbToMsb ? 1 << bitInByte : 0x80 >> bitInByte;
        return (value[byteIndex] & mask) != 0;
    }

    /// <summary>Устанавливает бит под номером bitNumber согласно правилам индексации.</summary>
    public static void SetBit(byte[] value, int bitNumber, bool bit, BitIndexing indexing, BitStartNumber start)
    {
        ArgumentNullException.ThrowIfNull(value);
        int index = bitNumber - (start == BitStartNumber.OneBased ? 1 : 0);
        int totalBits = value.Length * 8;
        if (index < 0 || index >= totalBits)
            throw new ArgumentOutOfRangeException(nameof(bitNumber), $"Бит {bitNumber} вне диапазона [1..{totalBits}].");

        int byteIndex = index / 8;
        int bitInByte = index % 8;
        int mask = indexing == BitIndexing.LsbToMsb ? 1 << bitInByte : 0x80 >> bitInByte;
        if (bit) value[byteIndex] |= (byte)mask;
        else value[byteIndex] &= (byte)~mask;
    }

    /// <summary>Разворачивает байты в последовательность битов (bits[0] — первый бит согласно indexing).</summary>
    public static bool[] ToBits(byte[] value, BitIndexing indexing)
    {
        bool[] bits = new bool[value.Length * 8];
        for (int i = 0; i < bits.Length; i++)
            bits[i] = GetBit(value, i + 1, indexing, BitStartNumber.OneBased);
        return bits;
    }

    /// <summary>Собирает массив байтов из последовательности битов.</summary>
    public static byte[] FromBits(bool[] bits, BitIndexing indexing)
    {
        byte[] value = new byte[(bits.Length + 7) / 8];
        for (int i = 0; i < bits.Length; i++)
            if (bits[i]) SetBit(value, i + 1, true, indexing, BitStartNumber.OneBased);
        return value;
    }

    /// <summary>Побитовый XOR массивов байтов одинаковой длины.</summary>
    public static byte[] Xor(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Длины массивов для XOR должны совпадать.");
        byte[] result = new byte[a.Length];
        for (int i = 0; i < a.Length; i++)
            result[i] = (byte)(a[i] ^ b[i]);
        return result;
    }

    /// <summary>Циклический сдвиг битовой последовательности влево.</summary>
    public static bool[] RotateLeft(bool[] bits, int shift)
    {
        int n = bits.Length;
        bool[] result = new bool[n];
        for (int i = 0; i < n; i++)
            result[i] = bits[(i + shift) % n];
        return result;
    }

    /// <summary>Соединяет две битовые последовательности.</summary>
    public static bool[] Concat(bool[] first, bool[] second)
    {
        bool[] result = new bool[first.Length + second.Length];
        first.CopyTo(result, 0);
        second.CopyTo(result, first.Length);
        return result;
    }
}