using CryptoLab1.Core;
using CryptoLab1.Feistel;
using CryptoLab1.Des;

namespace CryptoLab1.Deal;

/// <summary>
/// Задание 6. DEAL — 128-битный блочный шифр: сеть Фейстеля (задание 3),
/// в которой раундовой функцией F служит наш DES через адаптер.
/// Ключ 128/192 бит — 6 раундов, ключ 256 бит — 8 раундов.
/// </summary>
public class DealCipher : ISymmetricCipher
{
    public const int BlockSizeBytes = 16;

    private readonly FeistelNetwork _feistel;

    /// <param name="rounds">6 для ключей 128/192 бит, 8 для ключа 256 бит.</param>
    public DealCipher(int rounds = 6)
    {
        if (rounds is not (6 or 8))
            throw new ArgumentException("У DEAL может быть 6 раундов (ключ 128/192 бита) или 8 (ключ 256 бит).");
        _feistel = new FeistelNetwork(
            new DealKeyExpansion(),
            new BlockCipherRoundFunctionAdapter(() => new DesCipher()),
            rounds,
            BlockSizeBytes);
    }

    public int BlockSize => BlockSizeBytes;
    public int Rounds => _feistel.Rounds;

    public void SetKey(byte[] key) => _feistel.SetKey(key);
    public byte[] EncryptBlock(byte[] block) => _feistel.EncryptBlock(block);
    public byte[] DecryptBlock(byte[] block) => _feistel.DecryptBlock(block);
}