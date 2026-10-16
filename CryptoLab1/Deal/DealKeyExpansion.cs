using CryptoLab1.Core;
using CryptoLab1.Des;

namespace CryptoLab1.Deal;

/// <summary>
/// Задание 6. Расширение ключа DEAL: ключ 128/192 бита → 6 раундовых ключей,
/// 256 бит → 8 раундовых ключей (по 64 бита). Схема по статье L. Knudsen
/// «DEAL — a 128-bit block cipher»: раундовые ключи вырабатываются шифрованием
/// DESом констант по предыдущему раундовому ключу:
///   k0 = DES_{K1}(K2)              — ключ 128 бит
///   k0 = DES_{K3}(K1 XOR K2)         — ключ 192 бита
///   k0 = DES_{K4}(K1 XOR K2 XOR K3)    — ключ 256 бит
///   k_i = DES_{k_{i-1}}(k_{i-1} XOR (i-1)), i = 1..N
/// Если в материалах курса приведены точные константы — правится только этот класс.
/// </summary>
public class DealKeyExpansion : IKeyExpansion
{
    public byte[][] Expand(byte[] key)
    {
        if (key is not { Length: 16 or 24 or 32 })
            throw new ArgumentException("Ключ DEAL должен быть 16, 24 или 32 байта.");
        int rounds = key.Length == 32 ? 8 : 6;

        byte[] k0 = key.Length switch
        {
            16 => DesEncrypt(Slice(key, 0, 8), Slice(key, 8, 8)),
            24 => DesEncrypt(Slice(key, 16, 8), BitOperations.Xor(Slice(key, 0, 8), Slice(key, 8, 8))),
            _  => DesEncrypt(Slice(key, 24, 8),
                     BitOperations.Xor(BitOperations.Xor(Slice(key, 0, 8), Slice(key, 8, 8)), Slice(key, 16, 8)))
        };

        byte[][] roundKeys = new byte[rounds][];
        byte[] previous = k0;
        for (int i = 0; i < rounds; i++)
        {
            byte[] input = BitOperations.Xor(previous, RoundConstant(i)); // k_{i-1} XOR (i-1)
            byte[] roundKey = DesEncrypt(previous, input);                // DES_{k_{i-1}}(...)
            roundKeys[i] = roundKey;
            previous = roundKey;
        }
        return roundKeys;
    }

    private static byte[] DesEncrypt(byte[] key, byte[] block)
    {
        DesCipher des = new();
        des.SetKey(key);
        return des.EncryptBlock(block);
    }

    /// <summary>Константа раунда: 8-байтовое big-endian значение (i-1) = 0, 1, 2, …</summary>
    private static byte[] RoundConstant(int roundIndex)
    {
        byte[] constant = new byte[8];
        constant[6] = (byte)((roundIndex >> 8) & 0xFF);
        constant[7] = (byte)(roundIndex & 0xFF);
        return constant;
    }

    private static byte[] Slice(byte[] data, int start, int length)
    {
        byte[] part = new byte[length];
        Array.Copy(data, start, part, 0, length);
        return part;
    }
}