namespace CryptoLab1.Core;

/// <summary>Задание 2.1. Функционал процедуры расширения ключа (генерации раундовых ключей).</summary>
public interface IKeyExpansion
{
    /// <summary>Входной ключ — массив байтов; результат — массив раундовых ключей (каждый — массив байтов).</summary>
    byte[][] Expand(byte[] key);
}