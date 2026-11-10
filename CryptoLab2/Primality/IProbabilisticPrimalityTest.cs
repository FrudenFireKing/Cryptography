using System.Numerics;

namespace CryptoLab2.Primality;

/// <summary>
/// Задание 2. Контракт вероятностного теста простоты.
/// </summary>
public interface IProbabilisticPrimalityTest
{
    /// <summary>
    /// Вероятностная проверка простоты.
    /// </summary>
    /// <param name="value">Тестируемое значение.</param>
    /// <param name="minProbability">Минимальная вероятность простоты в диапазоне [0.5, 1);
    /// тест выполняет столько независимых итераций, чтобы вероятность ошибочно назвать
    /// составное число простым не превышала 1 − minProbability.</param>
    /// <returns>true — вероятно простое; false — точно составное.</returns>
    bool IsProbablyPrime(BigInteger value, double minProbability);
}
