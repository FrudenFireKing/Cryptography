namespace CryptoLab1.Core;

/// <summary>
/// Собственный генератор псевдослучайных чисел (xorshift64*). Используется только
/// для векторов инициализации, набивки ISO 10126 и демонстрационных данных —
/// к самому шифру отношения не имеет (требование «без готовых криптобиблиотек»).
/// </summary>
public static class SimpleRandom
{
    private static readonly object Gate = new();
    private static ulong _state = (ulong)Environment.TickCount64 * 2685821657736338717UL | 1UL;

    public static void NextBytes(byte[] buffer)
    {
        lock (Gate)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                _state ^= _state >> 12;
                _state ^= _state << 25;
                _state ^= _state >> 27;
                _state *= 0x2545F4914F6CDD1DUL;
                buffer[i] = (byte)(_state >> 56);
            }
        }
    }
}