using System.Numerics;
using CryptoLab2.NumberTheory;

namespace CryptoLab2.Primality;

/// <summary>
/// Вероятностный тест простоты Соловея–Штрассена. Итерация проверяет критерий Эйлера:
/// для простого value и НОД(a, value) = 1 выполнено a^((value−1)/2) ≡ (a|value) (mod value).
/// </summary>
/// <remarks>
/// Итерация: вычисляется символ Якоби J = (a|value). Если J = 0, найден нетривиальный
/// делитель — значение составное. Иначе сравнивается J с a^((value−1)/2) mod value
/// (с учётом представления −1 как value − 1). Для простого value совпадение гарантировано
/// критерием Эйлера; для составного множество лжецов Эйлера — собственная подгруппа,
/// поэтому лжецов ≤ φ(value)/2 и граница ошибки одной итерации равна 1/2.
/// </remarks>
public sealed class SolovayStrassenPrimalityTest : ProbabilisticPrimalityTest
{
    public SolovayStrassenPrimalityTest(INumberTheoryService numberTheory) : base(numberTheory) { }

    protected override double OneRoundErrorProbability => 0.5;

    protected override bool SingleRound(BigInteger value, BigInteger baseValue)
    {
        int jacobi = NumberTheory.JacobiSymbol(baseValue, value);
        if (jacobi == 0)
            return false; // gcd(baseValue, value) > 1 — найден нетривиальный делитель

        BigInteger eulerResidue = NumberTheory.ModPow(baseValue, (value - 1) / 2, value);

        // (a|value) = 1 ожидает остаток 1, (a|value) = −1 ожидает остаток value − 1
        return jacobi == 1 ? eulerResidue == 1 : eulerResidue == value - 1;
    }
}
