using System.Numerics;
using CryptoLab2.Common;
using CryptoLab2.NumberTheory;

namespace CryptoLab2.Primality;

/// <summary>
/// Задание 2. Базовый абстрактный класс вероятностного теста простоты,
/// построенный на поведенческом паттерне «Шаблонный метод» (Template Method).
/// Публичный метод <see cref="IsProbablyPrime"/> фиксирует неизменный каркас:
/// валидация вероятности → отсев тривиальных случаев → вычисление числа итераций
/// по требуемой вероятности → цикл итераций со случайным основанием.
/// Поведение одной итерации кастомизируется наследниками через абстрактный
/// метод <see cref="SingleRound"/> — это «шаг-крючок» шаблонного метода.
/// Все вычисления выполняются сервисом теории чисел из задания 1.
/// </summary>
public abstract class ProbabilisticPrimalityTest : IProbabilisticPrimalityTest
{
    protected INumberTheoryService NumberTheory { get; }

    protected ProbabilisticPrimalityTest(INumberTheoryService numberTheory)
    {
        NumberTheory = numberTheory ?? throw new ArgumentNullException(nameof(numberTheory));
    }

    /// <summary>
    /// Верхняя граница вероятности ошибки ОДНОЙ итерации на составном значении:
    /// 1/2 для Ферма и Соловея–Штрассена, 1/4 для Миллера–Рабина.
    /// </summary>
    protected abstract double OneRoundErrorProbability { get; }

    /// <summary>
    /// Одна итерация теста со случайным основанием baseValue:
    /// true — основание не выявило составность, false — доказана составность.
    /// </summary>
    protected abstract bool SingleRound(BigInteger value, BigInteger baseValue);

    /// <inheritdoc/>
    public bool IsProbablyPrime(BigInteger value, double minProbability)
    {
        if (minProbability < 0.5 || minProbability >= 1.0)
            throw new ArgumentOutOfRangeException(
                nameof(minProbability), minProbability, "Минимальная вероятность простоты должна лежать в [0.5, 1).");

        if (value < 2) return false;          // 0 и 1 — не простые
        if (value == 2 || value == 3) return true;
        if (value.IsEven) return false;       // чётные > 2 — составные

        int rounds = ComputeRoundCount(minProbability);
        for (int round = 0; round < rounds; round++)
        {
            // случайное основание из [2, value − 2]: 1 и value − 1 информативности не несут
            BigInteger baseValue = PseudoRandom.NextInRange(2, value - 1);
            if (!SingleRound(value, baseValue))
                return false; // найдено основание-свидетель составности
        }

        return true;
    }

    /// <summary>
    /// Число итераций из требуемой вероятности. Если для составного value вероятность
    /// «обмана» одной итерацией не превышает ε, то для t независимых итераций (разные
    /// случайные основания) она не превышает ε^t. Требование ε^t ≤ 1 − minProbability
    /// даёт t ≥ ln(1 − minProbability) / ln(ε).
    /// </summary>
    protected virtual int ComputeRoundCount(double minProbability)
    {
        double epsilon = OneRoundErrorProbability;
        double raw = Math.Log(1.0 - minProbability) / Math.Log(epsilon);
        int rounds = (int)Math.Ceiling(raw - 1e-9); // защита от погрешности floating-point
        return Math.Max(rounds, 1);
    }
}
