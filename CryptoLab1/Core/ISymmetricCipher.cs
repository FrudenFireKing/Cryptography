namespace CryptoLab1.Core;

/// <summary>
/// Задание 2.3. Шифрование/дешифрование симметричным алгоритмом
/// с преднастроенными отдельным методом раундовыми ключами.
/// </summary>
public interface ISymmetricCipher
{
    /// <summary>Размер блока алгоритма в байтах (нужно контексту из задания 2.4).</summary>
    int BlockSize { get; }

    /// <summary>Преднастройка раундовых ключей по ключу [де]шифрования (массив байтов).</summary>
    void SetKey(byte[] key);

    byte[] EncryptBlock(byte[] block);

    byte[] DecryptBlock(byte[] block);
}