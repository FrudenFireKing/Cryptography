using CryptoLab1.Core;
using CryptoLab1.Feistel;

namespace CryptoLab1.Des;

/// <summary>
/// Задание 4. Алгоритм DES: сеть Фейстеля (задание 3) + реализации интерфейсов
/// 2.1/2.2 + перестановки IP/FP через функцию из задания 1.
/// </summary>
public class DesCipher : ISymmetricCipher
{
    public const int BlockSizeBytes = 8;

    private readonly FeistelNetwork _feistel =
        new(new DesKeyExpansion(), new DesRoundFunction(), rounds: 16, blockSize: BlockSizeBytes);

    public int BlockSize => BlockSizeBytes;

    public void SetKey(byte[] key) => _feistel.SetKey(key);

    public byte[] EncryptBlock(byte[] block)
    {
        // C = FP( Feistel( IP(P) ) )
        byte[] afterIp = BitOperations.PermuteBits(block, DesTables.Ip, BitIndexing.MsbToLsb, BitStartNumber.OneBased);
        byte[] preOutput = _feistel.EncryptBlock(afterIp);
        return BitOperations.PermuteBits(preOutput, DesTables.Fp, BitIndexing.MsbToLsb, BitStartNumber.OneBased);
    }

    public byte[] DecryptBlock(byte[] block)
    {
        // P = FP( Feistel^{-1} ( IP(C) ) ) — те же раунды, ключи в обратном порядке
        byte[] afterIp = BitOperations.PermuteBits(block, DesTables.Ip, BitIndexing.MsbToLsb, BitStartNumber.OneBased);
        byte[] preOutput = _feistel.DecryptBlock(afterIp);
        return BitOperations.PermuteBits(preOutput, DesTables.Fp, BitIndexing.MsbToLsb, BitStartNumber.OneBased);
    }
}