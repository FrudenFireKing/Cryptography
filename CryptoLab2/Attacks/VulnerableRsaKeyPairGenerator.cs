using System.Numerics;
using CryptoLab2.Common;
using CryptoLab2.NumberTheory;
using CryptoLab2.Primality;
using CryptoLab2.Rsa;

namespace CryptoLab2.Attacks;

/// <summary>
/// Задание 4. Генератор ключевых пар RSA, уязвимых к атаке Винера.
/// Разложение модуля выполняется обычным вероятностным тестом простоты
/// (Миллер–Рабин), но закрытая экспонента выбирается МАЛОЙ:
/// d берётся из [2^(B−1), 2^B), где B = ⌊битовая_длина_простых/4⌋ − 2, откуда
/// гарантированно d &lt; n^(1/4)/3. Открытая экспонента вычисляется как
/// e = d^(−1) (mod φ(n)) и оказывается величиной порядка n.
/// </summary>
public sealed class VulnerableRsaKeyPairGenerator
{
    private static readonly int[] SmallPrimes =
    {
        3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73,
        79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157,
        163, 167, 173, 179, 181, 191, 193, 197, 199, 211, 223, 227, 229, 233, 239, 241, 251
    };

    private readonly INumberTheoryService _numberTheory;
    private readonly IProbabilisticPrimalityTest _primalityTest;
    private readonly double _minPrimalityProbability;

    public VulnerableRsaKeyPairGenerator(double minPrimalityProbability = 0.99)
    {
        if (minPrimalityProbability < 0.5 || minPrimalityProbability >= 1.0)
            throw new ArgumentOutOfRangeException(nameof(minPrimalityProbability));

        _numberTheory = new NumberTheoryService();
        _primalityTest = new MillerRabinPrimalityTest(_numberTheory);
        _minPrimalityProbability = minPrimalityProbability;
    }

    /// <summary>Генерирует ключевую пару с малым d, заведомо уязвимую к атаке Винера.</summary>
    public RsaKeyPair GenerateVulnerableKeyPair(int primeBitLength)
    {
        if (primeBitLength < 16)
            throw new ArgumentOutOfRangeException(nameof(primeBitLength), "Требуется минимум 16 бит.");

        // B = ⌊b/4⌋ − 2 обеспечивает d < 2^B ≤ 2^(b/4−1) ≪ n^(1/4)/3 ≈ 2^(b/2)/3
        int dBitLength = Math.Max(primeBitLength / 4 - 2, 2);

        const int maxAttempts = 1000;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            BigInteger p = GeneratePrime(primeBitLength);
            BigInteger q = GeneratePrime(primeBitLength);
            if (p == q) continue;

            BigInteger n = p * q;
            BigInteger phi = (p - 1) * (q - 1);
            BigInteger lambda = LeastCommonMultiple(p - 1, q - 1);

            BigInteger d = PseudoRandom.RandomBits(dBitLength, setHighBit: true, setLowBit: false);
            if (d < 3) continue;
            if (_numberTheory.GcdEuclid(d, phi) != 1) continue; // e существует только при взаимной простоте

            BigInteger e = _numberTheory.ModularInverse(d, phi); // e·d ≡ 1 (mod φ(n))

            // контрольная проверка условия уязвимости d < n^(1/4)/3
            if (3 * d >= _numberTheory.IntegerRoot(n, 4)) continue;

            return new RsaKeyPair(
                new RsaPublicKey(n, e),
                new RsaPrivateKey(n, e, d, p, q, phi, lambda));
        }

        throw new InvalidOperationException("Не удалось сгенерировать уязвимую ключевую пару за отведённое число попыток.");
    }

    private BigInteger GeneratePrime(int bitLength)
    {
        while (true)
        {
            BigInteger candidate = PseudoRandom.RandomBits(bitLength, setHighBit: true, setLowBit: true);
            if (IsDivisibleBySmallPrime(candidate)) continue;
            if (_primalityTest.IsProbablyPrime(candidate, _minPrimalityProbability))
                return candidate;
        }
    }

    private static bool IsDivisibleBySmallPrime(BigInteger candidate)
    {
        foreach (int prime in SmallPrimes)
            if (candidate % prime == 0)
                return true;
        return false;
    }

    private BigInteger LeastCommonMultiple(BigInteger a, BigInteger b) =>
        a / _numberTheory.GcdEuclid(a, b) * b;
}
