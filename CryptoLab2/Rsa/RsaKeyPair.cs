using System.Numerics;

namespace CryptoLab2.Rsa;

/// <summary>Открытый ключ RSA: модуль n = p·q и открытая экспонента e.</summary>
public sealed class RsaPublicKey
{
    public BigInteger Modulus { get; }     // n
    public BigInteger Exponent { get; }    // e

    public RsaPublicKey(BigInteger modulus, BigInteger exponent)
    {
        if (modulus <= 0) throw new ArgumentOutOfRangeException(nameof(modulus));
        if (exponent <= 0) throw new ArgumentOutOfRangeException(nameof(exponent));
        Modulus = modulus;
        Exponent = exponent;
    }

    public override string ToString() => $"(n, e)";
}

/// <summary>Закрытый ключ RSA: полный набор параметров, включая разложение модуля.</summary>
public sealed class RsaPrivateKey
{
    public BigInteger Modulus { get; }            // n
    public BigInteger PublicExponent { get; }     // e
    public BigInteger DecryptionExponent { get; } // d = e^(-1) (mod φ(n))
    public BigInteger PrimeP { get; }             // p
    public BigInteger PrimeQ { get; }             // q
    public BigInteger EulerPhi { get; }           // φ(n) = (p−1)(q−1)
    public BigInteger CarmichaelLambda { get; }   // λ(n) = НОК(p−1, q−1)

    public RsaPrivateKey(BigInteger modulus, BigInteger publicExponent, BigInteger decryptionExponent,
        BigInteger primeP, BigInteger primeQ, BigInteger eulerPhi, BigInteger carmichaelLambda)
    {
        Modulus = modulus;
        PublicExponent = publicExponent;
        DecryptionExponent = decryptionExponent;
        PrimeP = primeP;
        PrimeQ = primeQ;
        EulerPhi = eulerPhi;
        CarmichaelLambda = carmichaelLambda;
    }
}

/// <summary>Сгенерированная ключевая пара RSA.</summary>
public sealed class RsaKeyPair
{
    public RsaPublicKey Public { get; }
    public RsaPrivateKey Private { get; }

    public RsaKeyPair(RsaPublicKey publicKey, RsaPrivateKey privateKey)
    {
        Public = publicKey ?? throw new ArgumentNullException(nameof(publicKey));
        Private = privateKey ?? throw new ArgumentNullException(nameof(privateKey));
    }
}
