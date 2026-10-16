namespace CryptoLab1.Core;

/// <summary>Задание 2.2. Функционал шифрующего преобразования (раундовая функция).</summary>
public interface IRoundFunction
{
    /// <summary>Входной блок + раундовый ключ → выходной блок (все — массивы байтов).</summary>
    byte[] Transform(byte[] block, byte[] roundKey);
}