using System.Numerics;

namespace CryptoLab2.Attacks;

/// <summary>Подходящая дробь num/den цепной дроби e/n, вычисленная во время атаки Винера.</summary>
public readonly record struct WienerConvergent(BigInteger Numerator, BigInteger Denominator)
{
    public override string ToString() => $"{Numerator}/{Denominator}";
}

/// <summary>Результат атаки Винера на открытый ключ RSA.</summary>
public sealed class WienerAttackResult
{
    /// <summary>true — закрытая экспонента восстановлена (ключ уязвим).</summary>
    public bool Success { get; init; }

    /// <summary>Найденное значение дешифрующей экспоненты d (при Success = false равно 0).</summary>
    public BigInteger DecryptionExponent { get; init; }

    /// <summary>Найденное значение функции Эйлера φ(n) (при Success = false равно 0).</summary>
    public BigInteger EulerPhi { get; init; }

    /// <summary>Коллекция всех подходящих дробей, вычисленных во время атаки.</summary>
    public IReadOnlyList<WienerConvergent> Convergents { get; init; } = Array.Empty<WienerConvergent>();

    /// <summary>Сколько кандидатов d было проверено до получения результата.</summary>
    public int VerifiedCandidates { get; init; }
}
