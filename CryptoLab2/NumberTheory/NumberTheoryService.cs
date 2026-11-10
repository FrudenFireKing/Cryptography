using System.Numerics;
using CryptoLab2.Common;

namespace CryptoLab2.NumberTheory;

/// <summary>
/// Задание 1. Реализация stateless-сервиса теории чисел.
/// Класс не имеет полей и состояния: результат каждой операции зависит только от аргументов,
/// поэтому сервис потокобезопасен и может использоваться из любого числа потребителей.
/// Готовые аналоги из ядра (BigInteger.GreatestCommonDivisor, BigInteger.ModPow и т.п.)
/// сознательно не используются — все алгоритмы реализованы вручную. Длинные целые
/// (System.Numerics.BigInteger) применяются согласно прямому разрешению условия задания.
/// </summary>
public sealed class NumberTheoryService : INumberTheoryService
{
    // ------------------------- Символ Лежандра -------------------------

    /// <inheritdoc/>
    /// <remarks>Критерий Эйлера: для нечётного простого p и a ≠ 0 (mod p):
    /// a^((p−1)/2) ≡ 1 (mod p) ⇔ a — квадратичный вычет, иначе ≡ −1 (mod p).</remarks>
    public int LegendreSymbol(BigInteger a, BigInteger p)
    {
        if (p <= 2 || p.IsEven)
            throw new ArgumentException("Символ Лежандра определён для нечётного простого p > 2.", nameof(p));

        a %= p;
        if (a < 0) a += p; // нормализация к [0, p)
        if (a == 0) return 0;

        BigInteger residue = ModPow(a, (p - 1) / 2, p);
        return residue == 1 ? 1 : -1; // для простого p корень из ±1 другой не встречается
    }

    // ------------------------- Символ Якоби -------------------------

    /// <inheritdoc/>
    /// <remarks>
    /// Классический алгоритм с квадратичным законом взаимности: обнуление множителя 2
    /// по правилу (2|n) = (−1)^((n²−1)/8), перестановка аргументов по взаимности с
    /// поправкой при a ≡ n ≡ 3 (mod 4). Вычисляется за O(log n) делений.
    /// </remarks>
    public int JacobiSymbol(BigInteger a, BigInteger n)
    {
        if (n <= 0 || n.IsEven)
            throw new ArgumentException("Символ Якоби определён для нечётного положительного n.", nameof(n));
        if (n == 1) return 1;

        a %= n;
        if (a < 0) a += n;

        int result = 1;
        while (a != 0)
        {
            // выносим все степени двойки: (2·a | n) = (2|n)·(a|n), (2|n) = −1 при n ≡ ±3 (mod 8)
            while (a.IsEven)
            {
                a >>= 1;
                BigInteger r = n & 7; // n mod 8
                if (r == 3 || r == 5) result = -result;
            }

            // квадратичный закон взаимности: (a|n)·(n|a) = −1 при a ≡ n ≡ 3 (mod 4)
            (a, n) = (n, a);
            if ((a & 3) == 3 && (n & 3) == 3) result = -result;

            a %= n;
        }

        // здесь a = 0: если n > 1, то gcd(a₀, n) > 1 → символ равен 0
        return n == 1 ? result : 0;
    }

    // ------------------------- Алгоритмы Евклида -------------------------

    /// <inheritdoc/>
    public BigInteger GcdEuclid(BigInteger a, BigInteger b)
    {
        a = BigInteger.Abs(a);
        b = BigInteger.Abs(b);
        while (b != 0)
            (a, b) = (b, a % b);
        return a;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Итеративная схема: ведём остатки r и коэффициенты s, t так, что
    /// a·sᵢ + b·tᵢ = rᵢ на каждом шаге. Инвариант сохраняется прямой подстановкой:
    /// новый остаток rᵢ₊₁ = rᵢ₋₁ − q·rᵢ даёт sᵢ₊₁ = sᵢ₋₁ − q·sᵢ, tᵢ₊₁ = tᵢ₋₁ − q·tᵢ.
    /// </remarks>
    public (BigInteger Gcd, BigInteger X, BigInteger Y) GcdExtended(BigInteger a, BigInteger b)
    {
        BigInteger r0 = BigInteger.Abs(a), r1 = BigInteger.Abs(b);
        BigInteger s0 = 1, s1 = 0;
        BigInteger t0 = 0, t1 = 1;

        while (r1 != 0)
        {
            BigInteger q = r0 / r1;
            (r0, r1) = (r1, r0 - q * r1);
            (s0, s1) = (s1, s0 - q * s1);
            (t0, t1) = (t1, t0 - q * t1);
        }

        // |a|·s0 + |b|·t0 = r0 — восстанавливаем знаки исходных аргументов
        return (r0, a.Sign * s0, b.Sign * t0);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Бинарный алгоритм опирается на свойства чётности:
    /// НОД(2a, 2b) = 2·НОД(a, b); НОД(2a, b) = НОД(a, b) при b нечётном;
    /// НОД(a, b) = НОД(b, |a − b|) при a, b нечётных.
    /// Использует только вычитания, сравнения и сдвиги.
    /// </remarks>
    public BigInteger GcdBinary(BigInteger a, BigInteger b)
    {
        a = BigInteger.Abs(a);
        b = BigInteger.Abs(b);
        if (a == 0) return b;
        if (b == 0) return a;

        int commonTwos = 0;
        while (((a | b) & 1) == 0) // оба чётные
        {
            a >>= 1;
            b >>= 1;
            commonTwos++;
        }

        while (a.IsEven) a >>= 1; // делаем a нечётным
        do
        {
            while (b.IsEven) b >>= 1; // делаем b нечётным
            if (a > b) (a, b) = (b, a); // гарантируем a ≤ b
            b -= a; // b становится чётным либо нулём
        } while (b != 0);

        return a << commonTwos;
    }

    // ------------------------- Модульная арифметика -------------------------

    /// <inheritdoc/>
    /// <remarks>
    /// Быстрое возведение в степень («возведи в квадрат и умножь»): разложение
    /// показателя в двоичную систему даёт O(log exponent) умножений по модулю.
    /// </remarks>
    public BigInteger ModPow(BigInteger value, BigInteger exponent, BigInteger modulus)
    {
        if (modulus <= 0)
            throw new ArgumentOutOfRangeException(nameof(modulus), "Модуль должен быть положительным.");

        if (exponent < 0) // value^(−e) = (value^(−1))^e
            return ModPow(ModularInverse(value, modulus), -exponent, modulus);

        BigInteger result = 1 % modulus; // корректно и для modulus = 1
        BigInteger baseValue = value % modulus;
        if (baseValue < 0) baseValue += modulus;

        while (exponent > 0)
        {
            if (!exponent.IsEven)
                result = result * baseValue % modulus;
            baseValue = baseValue * baseValue % modulus;
            exponent >>= 1;
        }

        return result;
    }

    /// <inheritdoc/>
    public BigInteger ModularInverse(BigInteger value, BigInteger modulus)
    {
        if (modulus <= 0)
            throw new ArgumentOutOfRangeException(nameof(modulus), "Модуль должен быть положительным.");

        (BigInteger gcd, BigInteger x, _) = GcdExtended(value, modulus);
        if (gcd != 1)
            throw new ArgumentException("Обратный элемент не существует: НОД(value, modulus) ≠ 1.", nameof(value));

        BigInteger inverse = x % modulus;
        if (inverse < 0) inverse += modulus;
        return inverse;
    }

    // ------------------------- Функция Эйлера -------------------------

    /// <inheritdoc/>
    public BigInteger EulerFunctionByDefinition(BigInteger n)
    {
        if (n <= 0)
            throw new ArgumentOutOfRangeException(nameof(n), "φ(n) определена для n ≥ 1.");

        BigInteger coprimeCount = 0;
        for (BigInteger k = 1; k <= n; k++)
            if (GcdBinary(k, n) == 1)
                coprimeCount++;
        return coprimeCount;
    }

    /// <inheritdoc/>
    public BigInteger EulerFunctionByFactorization(BigInteger n)
    {
        if (n <= 0)
            throw new ArgumentOutOfRangeException(nameof(n), "φ(n) определена для n ≥ 1.");

        BigInteger phi = 1;
        foreach ((BigInteger prime, int power) in PrimeFactorization(n))
            phi *= IntegerPower(prime, power - 1) * (prime - 1);
        return phi;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Тождество фон Штернека (1902): φ(n) = Σ_{k=1..n} gcd(k, n)·cos(2πk/n).
    /// Доказывается через разложение gcd(k, n) = Σ_{d | gcd(k,n)} φ(d) и фильтр
    /// корней из единицы (см. теоретическую часть). Вычисление в double:
    /// накопленная погрешность много меньше 0,5 при n ≲ 10^6, поэтому результат
    /// уверенно восстанавливается округлением до ближайшего целого.
    /// </remarks>
    public BigInteger EulerFunctionByFourierTransform(BigInteger n)
    {
        if (n <= 0)
            throw new ArgumentOutOfRangeException(nameof(n), "φ(n) определена для n ≥ 1.");
        if (n == 1) return 1;

        double sum = 0.0;
        double twoPi = 2.0 * Math.PI;
        for (BigInteger k = 1; k <= n; k++)
            sum += (double)GcdBinary(k, n) * Math.Cos(twoPi * (double)k / (double)n);

        return (BigInteger)Math.Round(sum, MidpointRounding.AwayFromZero);
    }

    // ------------------------- Функция Кармайкла -------------------------

    /// <inheritdoc/>
    /// <remarks>
    /// λ(n) = НОК по всем простым степеням из канонического разложения, где
    /// λ(p^a) = φ(p^a) = p^(a−1)(p−1) для нечётного p и
    /// λ(2) = 1, λ(4) = 2, λ(2^a) = 2^(a−2) при a ≥ 3.
    /// </remarks>
    public BigInteger CarmichaelFunction(BigInteger n)
    {
        if (n <= 0)
            throw new ArgumentOutOfRangeException(nameof(n), "λ(n) определена для n ≥ 1.");
        if (n == 1) return 1;

        BigInteger lambda = 1;
        foreach ((BigInteger prime, int power) in PrimeFactorization(n))
        {
            BigInteger lambdaPrimePower = prime == 2
                ? power switch
                {
                    1 => 1,
                    2 => 2,
                    _ => BigInteger.One << (power - 2)
                }
                : IntegerPower(prime, power - 1) * (prime - 1);

            lambda = LeastCommonMultiple(lambda, lambdaPrimePower);
        }

        return lambda;
    }

    // ------------------------- Разложение и корни -------------------------

    /// <inheritdoc/>
    /// <remarks>
    /// Перебор делителей 2, 3 и чисел вида 6k±1 до √n (остальных простых делителей
    /// быть не может). Предназначено для учебных чисел; стойкость RSA на этом не строится.
    /// </remarks>
    public IReadOnlyList<(BigInteger Prime, int Power)> PrimeFactorization(BigInteger n)
    {
        if (n <= 0)
            throw new ArgumentOutOfRangeException(nameof(n), "Разложение определено для n ≥ 1.");

        var factors = new List<(BigInteger Prime, int Power)>();
        if (n == 1) return factors;

        int power = 0;
        while (n.IsEven) { n >>= 1; power++; }
        if (power > 0) factors.Add((2, power));

        power = 0;
        while (n % 3 == 0) { n /= 3; power++; }
        if (power > 0) factors.Add((3, power));

        for (BigInteger divisor = 5; divisor * divisor <= n; divisor += (divisor & 3) == 1 ? 2 : 4)
        {
            power = 0;
            while (n % divisor == 0) { n /= divisor; power++; }
            if (power > 0) factors.Add((divisor, power));
        }

        if (n > 1) factors.Add((n, 1)); // остаток — простое
        return factors;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Метод Ньютона для целого корня: итерация x₊ = ((root−1)·x + value / x^(root−1)) / root
    /// монотонно убывает к ⌊value^(1/root)⌋; в конце — контрольная коррекция на ±1.
    /// </remarks>
    public BigInteger IntegerRoot(BigInteger value, int root)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Корень определён для неотрицательных значений.");
        if (root < 1)
            throw new ArgumentOutOfRangeException(nameof(root), "Степень корня должна быть натуральной.");
        if (value == 0 || root == 1) return value;
        if (value == 1) return 1;

        BigInteger x = BigInteger.One << ((GetBitLength(value) + root - 1) / root); // верхняя оценка
        while (true)
        {
            BigInteger next = ((root - 1) * x + value / IntegerPower(x, root - 1)) / root;
            if (next >= x) break;
            x = next;
        }

        while (IntegerPower(x, root) > value) x--;
        while (IntegerPower(x + 1, root) <= value) x++;
        return x;
    }

    // ------------------------- Внутренние помощники -------------------------

    private static BigInteger IntegerPower(BigInteger value, int power)
    {
        BigInteger result = 1;
        while (power > 0)
        {
            if ((power & 1) != 0) result *= value;
            value *= value;
            power >>= 1;
        }
        return result;
    }

    private BigInteger LeastCommonMultiple(BigInteger a, BigInteger b) =>
        a / GcdEuclid(a, b) * b;

    /// <summary>Битовая длина положительного числа: позиция старшего единичного бита плюс один.</summary>
    private static int GetBitLength(BigInteger value)
    {
        byte[] littleEndian = value.ToByteArray(); // little-endian, дополнение до двух
        int length = littleEndian.Length;
        if (length > 0 && littleEndian[length - 1] == 0) length--; // убрать знаковый ноль

        int bits = (length - 1) * 8;
        byte top = littleEndian[length - 1];
        for (int bit = 7; bit >= 0; bit--)
        {
            if ((top & (1 << bit)) != 0)
            {
                bits += bit + 1;
                break;
            }
        }
        return bits;
    }
}
