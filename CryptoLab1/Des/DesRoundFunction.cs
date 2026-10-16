using CryptoLab1.Core;

namespace CryptoLab1.Des;

/// <summary>
/// Задание 4. Раундовая функция F шифра DES.
/// Вход: правая половина блока (4 байта) и раундовый ключ (6 байт); выход — 4 байта.
/// </summary>
public class DesRoundFunction : IRoundFunction
{
    public const int HalfBlockBytes = 4;
    public const int RoundKeyBytes = 6;

    public byte[] Transform(byte[] block, byte[] roundKey)
    {
        if (block == null || block.Length != HalfBlockBytes)
            throw new ArgumentException($"Вход функции F должен быть {HalfBlockBytes} байт.");
        if (roundKey == null || roundKey.Length != RoundKeyBytes)
            throw new ArgumentException($"Раундовый ключ должен быть {RoundKeyBytes} байт.");

        // 1. Расширение E: 32 бита -> 48 (функция из задания 1)
        byte[] expanded = BitOperations.PermuteBits(block, DesTables.E, BitIndexing.MsbToLsb, BitStartNumber.OneBased);

        // 2. Наложение раундового ключа
        byte[] mixed = BitOperations.Xor(expanded, roundKey);

        // 3. Восемь S-блоков: 48 бит -> 32
        bool[] sOutput = new bool[32];
        for (int box = 0; box < 8; box++)
        {
            // 6 бит S-блока: b1..b6 (слева направо)
            int b1 = Bit(mixed, box * 6 + 1) ? 1 : 0;
            int b2 = Bit(mixed, box * 6 + 2) ? 1 : 0;
            int b3 = Bit(mixed, box * 6 + 3) ? 1 : 0;
            int b4 = Bit(mixed, box * 6 + 4) ? 1 : 0;
            int b5 = Bit(mixed, box * 6 + 5) ? 1 : 0;
            int b6 = Bit(mixed, box * 6 + 6) ? 1 : 0;

            int row = b1 * 2 + b6;                       // крайние биты
            int column = b2 * 8 + b3 * 4 + b4 * 2 + b5;  // четыре средних
            int value = DesTables.SBoxes[box][row * 16 + column];

            sOutput[box * 4 + 0] = (value & 0b1000) != 0;
            sOutput[box * 4 + 1] = (value & 0b0100) != 0;
            sOutput[box * 4 + 2] = (value & 0b0010) != 0;
            sOutput[box * 4 + 3] = (value & 0b0001) != 0;
        }

        // 4. Перестановка P (функция из задания 1)
        byte[] sBytes = BitOperations.FromBits(sOutput, BitIndexing.MsbToLsb);
        return BitOperations.PermuteBits(sBytes, DesTables.P, BitIndexing.MsbToLsb, BitStartNumber.OneBased);
    }

    private static bool Bit(byte[] data, int bitNumber) =>
        BitOperations.GetBit(data, bitNumber, BitIndexing.MsbToLsb, BitStartNumber.OneBased);
}