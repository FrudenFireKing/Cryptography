using CryptoLab1.Core;
using CryptoLab1.Deal;
using CryptoLab1.Des;

namespace CryptoLab1;

public static class Program
{
    public static async Task Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("=== Лабораторная работа №1 по основам криптографии ===\n");

        DemoTask1();
        DemoTask4();
        DemoTask5Modes();
        DemoTask5Files();
        DemoVisualFiles();
        DemoTask6();
        DemoTask7();
        await DemoAsyncOperations();

        Console.WriteLine("\nВсе демонстрации завершены.");
    }

    // ---------------- Задание 1 ----------------
    private static void DemoTask1()
    {
        Console.WriteLine("--- Задание 1. Перестановка битов (P-блок) ---");

        byte[] value = { 0xB4 }; // 1011 0100
        Console.WriteLine($"Исходное значение:         {ToBinary(value)} (0x{ToHex(value)})");

        // Реверс битов, нумерация как в DES: с 1, от старшего к младшему
        byte[] reversed = BitOperations.PermuteBits(
            value, new[] { 8, 7, 6, 5, 4, 3, 2, 1 }, BitIndexing.MsbToLsb, BitStartNumber.OneBased);
        Console.WriteLine($"Реверс (с 1, от старшего): {ToBinary(reversed)} (0x{ToHex(reversed)}, ожидалось 2D)");

        // Тот же реверс в другой системе индексации: с 0, от младшего к старшему
        byte[] reversed2 = BitOperations.PermuteBits(
            value, new[] { 7, 6, 5, 4, 3, 2, 1, 0 }, BitIndexing.LsbToMsb, BitStartNumber.ZeroBased);
        Console.WriteLine($"Реверс (с 0, от младшего): {ToBinary(reversed2)} (0x{ToHex(reversed2)}, ожидалось 2D)");

        // Произвольный P-блок: pBlock[i] — какой бит встанет на позицию i
        byte[] shuffled = BitOperations.PermuteBits(
            value, new[] { 0, 2, 4, 6, 1, 3, 5, 7 }, BitIndexing.LsbToMsb, BitStartNumber.ZeroBased);
        Console.WriteLine($"P-блок {{0,2,4,6,1,3,5,7}}:  {ToBinary(shuffled)} (0x{ToHex(shuffled)})\n");
    }

    // ---------------- Задание 4 ----------------
    private static void DemoTask4()
    {
        Console.WriteLine("--- Задание 4. DES: проверка на эталонных векторах ---");
        CheckDesVector("133457799BBCDFF1", "0123456789ABCDEF", "85E813540F0AB405");
        CheckDesVector("0E329232EA6D0D73", "8787878787878787", "0000000000000000");
        Console.WriteLine();
    }

    private static void CheckDesVector(string keyHex, string plaintextHex, string expectedCipherHex)
    {
        DesCipher des = new();
        des.SetKey(Hex(keyHex));
        byte[] plaintext = Hex(plaintextHex);
        byte[] ciphertext = des.EncryptBlock(plaintext);
        byte[] restored = des.DecryptBlock(ciphertext);

        bool cipherOk = ToHex(ciphertext) == expectedCipherHex;
        bool decryptOk = restored.SequenceEqual(plaintext);
        Console.WriteLine($"Ключ {keyHex}, текст {plaintextHex}:");
        Console.WriteLine($"  шифртекст {ToHex(ciphertext)} — {(cipherOk ? "совпал с эталоном" : "ОШИБКА")}, " +
                          $"дешифрование — {(decryptOk ? "OK" : "ОШИБКА")}");
    }

    // ---------------- Задание 5: режимы ----------------
    private static void DemoTask5Modes()
    {
        Console.WriteLine("--- Задание 5. DES: все режимы шифрования (набивка PKCS7) ---");
        Console.WriteLine("(режим RandomDelta добавляет к результату заголовок IV + delta)");

        byte[] key = Hex("0123456789ABCDEF");
        byte[] iv = Hex("FEDCBA9876543210");
        byte[] data = new byte[1001]; // длина, не кратная блоку
        SimpleRandom.NextBytes(data);

        foreach (BlockCipherMode mode in Enum.GetValues<BlockCipherMode>())
        {
            CipherContext context = new(
                new DesCipher(), key, mode, BlockPaddingMode.Pkcs7,
                iv: mode == BlockCipherMode.ECB ? null : iv);
            byte[] encrypted = context.Encrypt(data);
            byte[] decrypted = context.Decrypt(encrypted);
            Console.WriteLine($"DES, режим {mode,-12}: {data.Length,4} байт → {encrypted.Length,4} байт, " +
                              $"раунд-трип: {(decrypted.SequenceEqual(data) ? "OK" : "ОШИБКА")}");
        }

        foreach (BlockPaddingMode padding in Enum.GetValues<BlockPaddingMode>())
        {
            CipherContext context = new(new DesCipher(), key, BlockCipherMode.CBC, padding, iv);
            byte[] decrypted = context.Decrypt(context.Encrypt(data));
            Console.WriteLine($"DES, CBC + набивка {padding,-10}: раунд-трип: {(decrypted.SequenceEqual(data) ? "OK" : "ОШИБКА")}");
        }
        Console.WriteLine();
    }

    // ---------------- Задание 5: файлы ----------------
    private static void DemoTask5Files()
    {
        Console.WriteLine("--- Задание 5. Шифрование файлов (DES, режим CBC) ---");
        string dir = Path.Combine(Path.GetTempPath(), "CryptoLabDemo");
        Directory.CreateDirectory(dir);

        string source = Path.Combine(dir, "original.txt");
        string encryptedPath = Path.Combine(dir, "original.txt.enc");
        string decryptedPath = Path.Combine(dir, "original.decrypted.txt");
        File.WriteAllText(source, "Лабораторная работа №1 по основам криптографии: шифрование файла DESом в режиме CBC.");

        CipherContext context = new(
            new DesCipher(), Hex("AABBCCDDEEFF0011"), BlockCipherMode.CBC, BlockPaddingMode.Pkcs7, Hex("1122334455667788"));
        context.Encrypt(source, encryptedPath);
        context.Decrypt(encryptedPath, decryptedPath);

        byte[] original = File.ReadAllBytes(source);
        byte[] encrypted = File.ReadAllBytes(encryptedPath);
        byte[] restored = File.ReadAllBytes(decryptedPath);

        Console.WriteLine($"Исходный файл:  {source} ({original.Length} байт)");
        Console.WriteLine($"Зашифрованный:  {encryptedPath} ({encrypted.Length} байт), первые 16 байт: {ToHex(encrypted[..16])}…");
        Console.WriteLine($"Расшифрованный совпадает с исходным: {(original.SequenceEqual(restored) ? "OK" : "ОШИБКА")}");
        Console.WriteLine();
    }
    
    // ---------------- Визуальная демонстрация (картинка + mp3) ----------------
    private static void DemoVisualFiles()
    {
        Console.WriteLine("--- Визуальная демонстрация: картинка и mp3 ---");
        string demoDir = "demo";
        string photo = Path.Combine(demoDir, "grantalpha.jpg");
        string music = Path.Combine(demoDir, "Star_Wars_Main_Title.mp3");

        if (!File.Exists(photo) || !File.Exists(music))
        {
            Console.WriteLine($"Положите файлы в папку: {Path.GetFullPath(demoDir)} и запустите снова.");
            return;
        }

        CipherContext ctx = new(new DesCipher(), Hex("AABBCCDDEEFF0011"),
            BlockCipherMode.CBC, BlockPaddingMode.Pkcs7, Hex("1122334455667788"));

        // картинка: CBC + PKCS7
        ctx.Encrypt(photo, photo + ".enc");
        ctx.Decrypt(photo + ".enc", Path.ChangeExtension(photo, ".restored.jpg"));
        // музыка: OFB + Zeros
        CipherContext audioCtx = new(new DesCipher(), Hex("1122334455667788"),
            BlockCipherMode.OFB, BlockPaddingMode.Pkcs7, Hex("AABBCCDDEEFF0011"));
        audioCtx.Encrypt(music, music + ".enc");
        audioCtx.Decrypt(music + ".enc", Path.ChangeExtension(music, ".restored.mp3"));

        Console.WriteLine($"Картинка восстановлена бит-в-бит: {File.ReadAllBytes(photo).SequenceEqual(File.ReadAllBytes(Path.ChangeExtension(photo, ".restored.jpg")))}");
        Console.WriteLine($"Музыка восстановлена бит-в-бит:  {File.ReadAllBytes(music).SequenceEqual(File.ReadAllBytes(Path.ChangeExtension(music, ".restored.mp3")))}");
        Console.WriteLine($"Файлы лежат здесь (откройте в Проводнике): {Path.GetFullPath(demoDir)}");
    }

    // ---------------- Задание 6 ----------------
    private static void DemoTask6()
    {
        Console.WriteLine("--- Задание 6. DEAL (блок 128 бит, раундовая функция — наш DES) ---");
        DealCipher deal = new(); // 6 раундов, ключ 128 бит
        deal.SetKey(Hex("0123456789ABCDEF0123456789ABCDEF"));

        byte[] block = Hex("00112233445566778899AABBCCDDEEFF");
        byte[] encrypted = deal.EncryptBlock(block);
        byte[] decrypted = deal.DecryptBlock(encrypted);

        Console.WriteLine($"Блок открытого текста: {ToHex(block)}");
        Console.WriteLine($"Зашифрованный блок:    {ToHex(encrypted)}");
        Console.WriteLine($"Дешифрование совпало:  {(decrypted.SequenceEqual(block) ? "OK" : "ОШИБКА")}");
        Console.WriteLine($"Размер блока: {deal.BlockSize} байт (у DES — 8), раундов: {deal.Rounds}");
        Console.WriteLine();
    }

    // ---------------- Задание 7 ----------------
    private static void DemoTask7()
    {
        Console.WriteLine("--- Задание 7. DEAL: разные режимы шифрования и файлы ---");
        byte[] key = Hex("0123456789ABCDEF0123456789ABCDEF"); // 128 бит → 6 раундов
        byte[] iv = Hex("00112233445566778899AABBCCDDEEFF");
        byte[] data = new byte[1057]; // произвольная длина («музыкальный файл»)
        SimpleRandom.NextBytes(data);

        foreach (BlockCipherMode mode in Enum.GetValues<BlockCipherMode>())
        {
            CipherContext context = new(
                new DealCipher(), key, mode, BlockPaddingMode.Pkcs7,
                iv: mode == BlockCipherMode.ECB ? null : iv);
            byte[] decrypted = context.Decrypt(context.Encrypt(data));
            Console.WriteLine($"DEAL, режим {mode,-12}: раунд-трип: {(decrypted.SequenceEqual(data) ? "OK" : "ОШИБКА")}");
        }

        string dir = Path.Combine(Path.GetTempPath(), "CryptoLabDemo");
        Directory.CreateDirectory(dir);
        string source = Path.Combine(dir, "track.mp3");
        string encryptedPath = Path.Combine(dir, "track.mp3.enc");
        string decryptedPath = Path.Combine(dir, "track.decrypted.mp3");
        File.WriteAllBytes(source, data);

        CipherContext fileContext = new(new DealCipher(), key, BlockCipherMode.OFB, BlockPaddingMode.Zeros, iv);
        fileContext.Encrypt(source, encryptedPath);
        fileContext.Decrypt(encryptedPath, decryptedPath);
        Console.WriteLine($"Файл track.mp3: зашифрован → расшифрован: " +
                          $"{(File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(decryptedPath)) ? "OK" : "ОШИБКА")}");
        Console.WriteLine();
    }

    // ---------------- Асинхронность (задание 2.4) ----------------
    private static async Task DemoAsyncOperations()
    {
        Console.WriteLine("--- Асинхронные операции и параллельная обработка (задание 2.4) ---");

        CipherContext context = new(
            new DesCipher(), Hex("133457799BBCDFF1"),
            BlockCipherMode.CTR, BlockPaddingMode.Pkcs7, Hex("0000000000000010"));

        byte[] data = new byte[4096];
        SimpleRandom.NextBytes(data);
        byte[] encrypted = await context.EncryptAsync(data);
        byte[] decrypted = await context.DecryptAsync(encrypted);
        Console.WriteLine($"EncryptAsync/DecryptAsync (CTR): {(decrypted.SequenceEqual(data) ? "OK" : "ОШИБКА")}");

        // параллельное шифрование нескольких файлов
        string dir = Path.Combine(Path.GetTempPath(), "CryptoLabDemo");
        Directory.CreateDirectory(dir);
        string[] sources =
        {
            Path.Combine(dir, "a.txt"), // текстовый файл
            Path.Combine(dir, "b.png"), // «изображение»
            Path.Combine(dir, "c.cs")   // файл исходного кода
        };
        File.WriteAllBytes(sources[0], "Текстовый файл"u8.ToArray());
        File.WriteAllBytes(sources[1], PseudoFile(4096));
        File.WriteAllBytes(sources[2], "class RedBlackTreeAllocator { }"u8.ToArray());

        byte[][] encryptedFiles = await Task.WhenAll(sources.Select(File.ReadAllBytes).Select(context.EncryptAsync));
        byte[][] decryptedFiles = await Task.WhenAll(encryptedFiles.Select(context.DecryptAsync));
        bool allOk = sources.Select(File.ReadAllBytes)
            .Zip(decryptedFiles, (original, restored) => original.SequenceEqual(restored))
            .All(x => x);
        Console.WriteLine($"Параллельное шифрование {sources.Length} файлов (Task.WhenAll): {(allOk ? "OK" : "ОШИБКА")}");
    }

    private static byte[] PseudoFile(int length)
    {
        byte[] bytes = new byte[length];
        SimpleRandom.NextBytes(bytes);
        return bytes;
    }

    // ---------------- Вспомогательные методы ----------------
    private static byte[] Hex(string hex)
    {
        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }

    private static string ToHex(byte[] bytes) => Convert.ToHexString(bytes);

    private static string ToBinary(byte[] bytes) =>
        string.Join(' ', bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
}