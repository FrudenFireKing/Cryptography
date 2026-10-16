namespace CryptoLab1.Core;

/// <summary>Правила индексирования битов (задание 1).</summary>
public enum BitIndexing
{
    /// <summary>От младшего к старшему: бит 0 — младший бит первого байта.</summary>
    LsbToMsb,
    /// <summary>От старшего к младшему: бит 0 — старший бит первого байта (нумерация DES).</summary>
    MsbToLsb
}

/// <summary>Номер начального бита (задание 1).</summary>
public enum BitStartNumber
{
    ZeroBased,
    OneBased
}