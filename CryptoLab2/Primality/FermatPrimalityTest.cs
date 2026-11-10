using System.Numerics;
using CryptoLab2.NumberTheory;

namespace CryptoLab2.Primality;

/// <summary>
/// Вероятностный тест простоты Ферма. Итерация проверяет условие
/// a^(value − 1) ≡ 1 (mod value) — необходимое условие простоты по малой теореме Ферма.
/// </summary>
/// <remarks>
/// <para>Граница ошибки одной итерации ≤ 1/2 обоснована так: для составного value,
/// НЕ являющегося числом Кармайкла, множество лжецов Ферма (оснований, для которых
/// a^(value−1) ≡ 1) образует собственную подгруппу группы Z*value, поэтому среди
/// взаимно простых с value оснований лжецов не больше половины.</para>
/// <para>Известное ограничение: для чисел Кармайкла (561, 1105, 1729, …) все взаимно
/// простые основания — лжецы, поэтому тест Ферма их принципиально не распознаёт.
/// Соловей–Штрассен и Миллер–Рабин лишены этого дефекта.</para>
/// </remarks>
public sealed class FermatPrimalityTest : ProbabilisticPrimalityTest
{
    public FermatPrimalityTest(INumberTheoryService numberTheory) : base(numberTheory) { }

    protected override double OneRoundErrorProbability => 0.5;

    protected override bool SingleRound(BigInteger value, BigInteger baseValue) =>
        NumberTheory.ModPow(baseValue, value - 1, value) == 1;
}
