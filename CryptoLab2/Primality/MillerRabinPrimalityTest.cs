using System.Numerics;
using CryptoLab2.NumberTheory;

namespace CryptoLab2.Primality;

/// <summary>
/// Вероятностный тест простоты Миллера–Рабина — сильнейший из трёх реализованных:
/// граница ошибки одной итерации равна 1/4 для ЛЮБОГО нечётного составного числа.
/// Итерация опирается на критерий: для простого value последовательность
/// a^d, a^(2d), …, a^(2^(s−1)·d) (где value − 1 = 2^s·d, d нечётно) обязана
/// содержать −1 либо начинаться с 1.
/// </summary>
/// <remarks>
/// Числа, проходящие итерацию с данным a, называются сильными псевдопростыми по
/// основанию a (например, 2047 = 23·89 — сильное псевдопростое по базе 2).
/// Теорема Рабина: для нечётного составного value доля таких «лжецов» среди
/// оснований из [2, value − 2] не превышает 1/4.
/// </remarks>
public sealed class MillerRabinPrimalityTest : ProbabilisticPrimalityTest
{
    public MillerRabinPrimalityTest(INumberTheoryService numberTheory) : base(numberTheory) { }

    protected override double OneRoundErrorProbability => 0.25;

    protected override bool SingleRound(BigInteger value, BigInteger baseValue)
    {
        // разложение value − 1 = 2^s · d, где d нечётно
        BigInteger d = value - 1;
        int s = 0;
        while (d.IsEven)
        {
            d >>= 1;
            s++;
        }

        BigInteger x = NumberTheory.ModPow(baseValue, d, value);
        if (x == 1 || x == value - 1)
            return true; // сильное условие выполнено сразу

        for (int r = 1; r < s; r++)
        {
            x = x * x % value;
            if (x == value - 1)
                return true; // встретили −1 в нужной позиции
        }

        return false; // последовательность «плохая» → составное доказано
    }
}
