using System.Numerics;
using CryptoLab2.NumberTheory;
using CryptoLab2.Rsa;

namespace CryptoLab2.Attacks;

/// <summary>
/// Задание 4. Сервис атаки Винера на открытый ключ RSA (n, e).
/// Атака применима, когда закрытая экспонента мала: d &lt; (1/3)·n^(1/4).
/// Идея: из e·d − k·φ(n) = 1 следует |e/n − k/d| &lt; 1/(2d²), поэтому дробь k/d —
/// подходящая дробь разложения e/n в непрерывную дробь (теорема Лежандра).
/// Атака перебирает подходящие дроби k/d: по каждому кандидату восстанавливается
/// кандидат φ(n) = (e·d − 1)/k, который верифицируется решением уравнения
/// x² − (n − φ + 1)x + n = 0 (дискриминант обязан быть полным квадратом).
/// </summary>
public sealed class WienerAttackService
{
    private readonly INumberTheoryService _numberTheory;

    public WienerAttackService(INumberTheoryService numberTheory)
    {
        _numberTheory = numberTheory ?? throw new ArgumentNullException(nameof(numberTheory));
    }

    /// <summary>Выполняет атаку; возвращает найденные d, φ(n) и все вычисленные подходящие дроби.</summary>
    public WienerAttackResult Attack(RsaPublicKey publicKey)
    {
        ArgumentNullException.ThrowIfNull(publicKey);

        BigInteger n = publicKey.Modulus;
        BigInteger e = publicKey.Exponent;

        // Неполные частные цепной дроби e/n совпадают с частными алгоритма Евклида для (e, n)
        List<BigInteger> coefficients = ContinuedFractionCoefficients(e, n);

        var convergents = new List<WienerConvergent>(coefficients.Count);

        // рекуррентные формулы подходящих дробей:
        //   h_i = a_i·h_{i−1} + h_{i−2}, k_i = a_i·k_{i−1} + k_{i−2},
        //   h_{−1} = 1, h_{−2} = 0, k_{−1} = 0, k_{−2} = 1
        BigInteger hPrev2 = 0, hPrev1 = 1;
        BigInteger kPrev2 = 1, kPrev1 = 0;
        int verifiedCandidates = 0;

        for (int i = 0; i < coefficients.Count; i++)
        {
            BigInteger a = coefficients[i];
            BigInteger h = a * hPrev1 + hPrev2;
            BigInteger k = a * kPrev1 + kPrev2;
            hPrev2 = hPrev1;
            hPrev1 = h;
            kPrev2 = kPrev1;
            kPrev1 = k;
            convergents.Add(new WienerConvergent(h, k));

            // в записи Винера дробь k/d: числитель — k, знаменатель — кандидат в d
            BigInteger candidateD = k;
            BigInteger candidateK = h;
            if (candidateK == 0 || candidateD <= 0) continue;

            BigInteger edMinusOne = e * candidateD - 1;
            if (edMinusOne % candidateK != 0) continue;

            BigInteger candidatePhi = edMinusOne / candidateK;
            verifiedCandidates++;

            if (PhiMatchesModulus(n, candidatePhi) && FunctionalCheck(n, e, candidateD))
            {
                return new WienerAttackResult
                {
                    Success = true,
                    DecryptionExponent = candidateD,
                    EulerPhi = candidatePhi,
                    Convergents = convergents,
                    VerifiedCandidates = verifiedCandidates
                };
            }
        }

        return new WienerAttackResult
        {
            Success = false,
            Convergents = convergents,
            VerifiedCandidates = verifiedCandidates
        };
    }

    /// <summary>Неполные частные непрерывной дроби a/b — частные из алгоритма Евклида.</summary>
    private static List<BigInteger> ContinuedFractionCoefficients(BigInteger a, BigInteger b)
    {
        var coefficients = new List<BigInteger>();
        BigInteger x = a, y = b;
        while (y != 0)
        {
            coefficients.Add(x / y);
            (x, y) = (y, x % y);
        }
        return coefficients;
    }

    /// <summary>
    /// Проверка кандидата φ(n): p + q = n − φ + 1 и p·q = n, значит p, q — корни
    /// x² − (n − φ + 1)x + n; уравнение имеет целые корни ⇔ дискриминант — полный квадрат.
    /// </summary>
    private bool PhiMatchesModulus(BigInteger modulus, BigInteger phi)
    {
        if (phi <= 0 || phi >= modulus) return false;

        BigInteger sum = modulus - phi + 1;             // p + q
        BigInteger discriminant = sum * sum - 4 * modulus; // (p − q)²
        if (discriminant < 0) return false;

        BigInteger root = _numberTheory.IntegerRoot(discriminant, 2);
        if (root * root != discriminant) return false;
        if ((sum + root) % 2 != 0) return false;

        BigInteger p = (sum + root) / 2;
        BigInteger q = (sum - root) / 2;
        return p >= 2 && q >= 2 && p * q == modulus;
    }

    /// <summary>Функциональная проверка: расшифрование (m^e)^d ≡ m (mod n) на пробном сообщении.</summary>
    private bool FunctionalCheck(BigInteger modulus, BigInteger e, BigInteger d)
    {
        BigInteger testMessage = 12345;
        if (testMessage >= modulus) return true; // тривиальный случай малых модулей не проверяем
        BigInteger cipher = _numberTheory.ModPow(testMessage, e, modulus);
        return _numberTheory.ModPow(cipher, d, modulus) == testMessage;
    }
}
