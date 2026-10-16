namespace CryptoLab1.Core;

/// <summary>Режимы шифрования (задание 2.4).</summary>
public enum BlockCipherMode
{
    ECB, CBC, PCBC, CFB, OFB, CTR, RandomDelta
}

/// <summary>Режимы набивки (задание 2.4).</summary>
public enum BlockPaddingMode
{
    Zeros, AnsiX923, Pkcs7, Iso10126
}