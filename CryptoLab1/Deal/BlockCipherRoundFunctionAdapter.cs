using System.Collections.Concurrent;
using CryptoLab1.Core;

namespace CryptoLab1.Deal;

/// <summary>
/// Задание 6. Адаптер, позволяющий использовать любой блочный шифр
/// (реализацию ISymmetricCipher, например наш DES) в качестве раундовой
/// функции F сети Фейстеля: F(block, K) = EncryptBlock_с_ключом_K(block).
/// </summary>
public class BlockCipherRoundFunctionAdapter : IRoundFunction
{
    private readonly Func<ISymmetricCipher> _cipherFactory;
    private readonly ConcurrentDictionary<string, ISymmetricCipher> _ciphers = new();

    public BlockCipherRoundFunctionAdapter(Func<ISymmetricCipher> cipherFactory)
    {
        _cipherFactory = cipherFactory ?? throw new ArgumentNullException(nameof(cipherFactory));
    }

    public byte[] Transform(byte[] block, byte[] roundKey)
    {
        // Готовый экземпляр шифра с уже настроенным ключом кешируется:
        // в DEAL раундовых ключей всего 6 (или 8), и они переиспользуются на каждом блоке.
        ISymmetricCipher cipher = _ciphers.GetOrAdd(Convert.ToHexString(roundKey), _ =>
        {
            ISymmetricCipher prepared = _cipherFactory();
            prepared.SetKey(roundKey);
            return prepared;
        });
        return cipher.EncryptBlock(block);
    }
}