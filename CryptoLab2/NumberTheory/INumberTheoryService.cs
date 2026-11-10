using System.Numerics;

namespace CryptoLab2.NumberTheory;

/// <summary>
/// Задание 1. Контракт stateless-сервиса с компонентным функционалом теории чисел:
/// символы Лежандра и Якоби, три варианта алгоритма Евклида, возведение в степень
/// по модулю, функция Эйлера (три способа), функция Кармайкла.
/// </summary>
public interface INumberTheoryService
{
    /// <summary>Символ Лежандра (a | p) для нечётного простого p (корректность гарантирована только для простого p).</summary>
    int LegendreSymbol(BigInteger a, BigInteger p);

    /// <summary>Символ Якоби (a | n) для нечётного положительного n (n не обязано быть простым).</summary>
    int JacobiSymbol(BigInteger a, BigInteger n);

    /// <summary>НОД двух целых чисел алгоритмом Евклида (последовательные деления с остатком).</summary>
    BigInteger GcdEuclid(BigInteger a, BigInteger b);

    /// <summary>НОД и решение соотношения Безу a·X + b·Y = НОД(a, b) расширенным алгоритмом Евклида.</summary>
    (BigInteger Gcd, BigInteger X, BigInteger Y) GcdExtended(BigInteger a, BigInteger b);

    /// <summary>НОД двух целых чисел бинарным алгоритмом Евклида (сдвиги и вычитания).</summary>
    BigInteger GcdBinary(BigInteger a, BigInteger b);

    /// <summary>Возведение value^exponent (mod modulus); при отрицательном показателе — через модульный обратный элемент.</summary>
    BigInteger ModPow(BigInteger value, BigInteger exponent, BigInteger modulus);

    /// <summary>Обратный элемент value^(-1) (mod modulus); существует, только если НОД(value, modulus) = 1.</summary>
    BigInteger ModularInverse(BigInteger value, BigInteger modulus);

    /// <summary>φ(n) по определению: количество k ∈ [1, n], взаимно простых с n (подходит для малых n).</summary>
    BigInteger EulerFunctionByDefinition(BigInteger n);

    /// <summary>φ(n) через каноническое разложение n = ∏ pᵢ^aᵢ: φ = ∏ pᵢ^(aᵢ−1)·(pᵢ − 1).</summary>
    BigInteger EulerFunctionByFactorization(BigInteger n);

    /// <summary>
    /// φ(n) через дискретное преобразование Фурье — тождество фон Штернека:
    /// φ(n) = Σ_{k=1..n} gcd(k, n)·cos(2πk/n). Точность достаточна для n примерно до 10^6.
    /// </summary>
    BigInteger EulerFunctionByFourierTransform(BigInteger n);

    /// <summary>λ(n) через каноническое разложение: λ(p^a) → см. свойства, λ(n) = НОК по простым степеням.</summary>
    BigInteger CarmichaelFunction(BigInteger n);

    /// <summary>Каноническое разложение на простые множители: n = ∏ pᵢ^aᵢ (перебор делителей, для малых n).</summary>
    IReadOnlyList<(BigInteger Prime, int Power)> PrimeFactorization(BigInteger n);

    /// <summary>Целая часть корня степени root: наибольшее x с x^root ≤ value (метод Ньютона).</summary>
    BigInteger IntegerRoot(BigInteger value, int root);
}
