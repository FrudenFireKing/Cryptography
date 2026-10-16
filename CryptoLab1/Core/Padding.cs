namespace CryptoLab1.Core;

/// <summary>Режимы набивки данных до размера блока (задание 2.4).</summary>
public static class Padding
{
    public static byte[] Apply(byte[] data, int blockSize, BlockPaddingMode mode)
    {
        int remainder = data.Length % blockSize;
        // Zeros дополняет только неполный блок; остальные схемы всегда добавляют набивку,
        // чтобы при дешифровании её можно было однозначно убрать.
        int padLength = mode == BlockPaddingMode.Zeros
            ? (remainder == 0 ? 0 : blockSize - remainder)
            : blockSize - remainder;

        byte[] padded = new byte[data.Length + padLength];
        Array.Copy(data, padded, data.Length);

        switch (mode)
        {
            case BlockPaddingMode.Zeros:
                break; // байты и так нулевые
            case BlockPaddingMode.AnsiX923:
                padded[^1] = (byte)padLength; // нули + последний байт = длина набивки
                break;
            case BlockPaddingMode.Pkcs7:
                Array.Fill(padded, (byte)padLength, data.Length, padLength); // все байты = длина
                break;
            case BlockPaddingMode.Iso10126:
            {
                byte[] randomTail = new byte[padLength - 1]; // случайные байты
                SimpleRandom.NextBytes(randomTail);
                Array.Copy(randomTail, 0, padded, data.Length, padLength - 1);
                padded[^1] = (byte)padLength;                // последний = длина
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
        return padded;
    }

    public static byte[] Remove(byte[] data, int blockSize, BlockPaddingMode mode)
    {
        if (data.Length % blockSize != 0)
            throw new InvalidOperationException("Длина данных не кратна размеру блока.");

        int padLength;
        if (mode == BlockPaddingMode.Zeros)
        {
            // у Zeros нет маркера длины: убираем хвостовые нули (известное ограничение схемы)
            padLength = CountTrailingZeros(data);
        }
        else
        {
            padLength = data[^1];
            if (padLength < 1 || padLength > blockSize)
                throw new InvalidOperationException("Нарушен формат набивки: данные повреждены.");
        }

        if (mode == BlockPaddingMode.Pkcs7)
        {
            for (int i = data.Length - padLength; i < data.Length; i++)
                if (data[i] != padLength)
                    throw new InvalidOperationException("Нарушен формат набивки PKCS#7.");
        }

        if (mode == BlockPaddingMode.AnsiX923)
        {
            for (int i = data.Length - padLength; i < data.Length - 1; i++)
                if (data[i] != 0)
                    throw new InvalidOperationException("Нарушен формат набивки ANSI X.923.");
        }

        byte[] result = new byte[data.Length - padLength];
        Array.Copy(data, result, result.Length);
        return result;
    }

    private static int CountTrailingZeros(byte[] data)
    {
        int count = 0;
        for (int i = data.Length - 1; i >= 0 && data[i] == 0; i--) count++;
        return count;
    }
}