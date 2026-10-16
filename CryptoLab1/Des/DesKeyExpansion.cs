using CryptoLab1.Core;

namespace CryptoLab1.Des;

/// <summary>Задание 4. Расширение ключа DES: 64-битный ключ -> 16 раундовых ключей по 48 бит.</summary>
public class DesKeyExpansion : IKeyExpansion
{
    public const int KeySizeBytes = 8;

    public byte[][] Expand(byte[] key)
    {
        if (key == null || key.Length != KeySizeBytes)
            throw new ArgumentException($"Ключ DES должен быть {KeySizeBytes} байт.");

        // PC-1: 64 бита → 56 бит (исключая биты чётности), через функцию из задания 1
        byte[] permuted = BitOperations.PermuteBits(key, DesTables.Pc1, BitIndexing.MsbToLsb, BitStartNumber.OneBased);
        bool[] cdBits = BitOperations.ToBits(permuted, BitIndexing.MsbToLsb); // 56 бит

        bool[] c = new bool[28]; // C_0
        bool[] d = new bool[28]; // D_0
        Array.Copy(cdBits, 0, c, 0, 28);
        Array.Copy(cdBits, 28, d, 0, 28);

        byte[][] roundKeys = new byte[16][];
        for (int round = 0; round < 16; round++)
        {
            c = BitOperations.RotateLeft(c, DesTables.Shifts[round]);
            d = BitOperations.RotateLeft(d, DesTables.Shifts[round]);

            byte[] cd = BitOperations.FromBits(BitOperations.Concat(c, d), BitIndexing.MsbToLsb);
            roundKeys[round] = BitOperations.PermuteBits(cd, DesTables.Pc2, BitIndexing.MsbToLsb, BitStartNumber.OneBased);
        }
        return roundKeys;
    }
}