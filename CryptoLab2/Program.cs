using System.Diagnostics;
using System.Numerics;
using System.Text;
using CryptoLab2.Attacks;
using CryptoLab2.Common;
using CryptoLab2.NumberTheory;
using CryptoLab2.Primality;
using CryptoLab2.Rsa;

namespace CryptoLab2;

/// <summary>
/// Демонстрация выполнения заданий 1–4 лабораторной работы №2.
/// Фиксированное зерно генератора делает вывод воспроизводимым от запуска к запуску.
/// </summary>
public static class Program
{
    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        PseudoRandom.SetSeed(20251113UL);

        Console.WriteLine("=== Лабораторная работа №2 по основам криптографии ===");
        Console.WriteLine("Теория чисел, вероятностные тесты простоты, RSA, атака Винера\n");

        DemoTask1();
        DemoTask2();
        DemoTask3();
        DemoTask4();

        Console.WriteLine("\nВсе демонстрации завершены.");
    }

    // ------------------------- Задание 1 -------------------------

    private static void DemoTask1()
    {
        Console.WriteLine("--- Задание 1. Stateless-сервис теории чисел ---");
        INumberTheoryService nt = new NumberTheoryService();

        // Три алгоритма Евклида + соотношение Безу
        BigInteger a = 252, b = 198;
        (BigInteger gcd, BigInteger x, BigInteger y) = nt.GcdExtended(a, b);
        Console.WriteLine($"НОД(252, 198): Евклид = {nt.GcdEuclid(a, b)}, бинарный = {nt.GcdBinary(a, b)}, " +
                          $"расширенный = {gcd} — {(nt.GcdEuclid(a, b) == nt.GcdBinary(a, b) && nt.GcdBinary(a, b) == gcd ? "совпали" : "ОШИБКА")}");
        Console.WriteLine($"Соотношение Безу: 252·({x}) + 198·({y}) = {a * x + b * y} " +
                          $"{(a * x + b * y == gcd ? "(проверка OK)" : "(ОШИБКА)")}");

        // Возведение в степень по модулю и обратный элемент
        Console.WriteLine($"Быстрое возведение в степень: 3^200 mod 1000 = {nt.ModPow(3, 200, 1000)}");
        BigInteger inv = nt.ModularInverse(7, 40);
        Console.WriteLine($"Обратный элемент: 7^(-1) mod 40 = {inv} (7·{inv} mod 40 = {7 * inv % 40})");

        // Символ Лежандра: сверка с определением через множество квадратов
        BigInteger p = 13;
        var quadraticResidues = new HashSet<BigInteger>();
        for (BigInteger v = 1; v < p; v++) quadraticResidues.Add(v * v % p);

        bool legendreOk = true;
        for (BigInteger v = 1; v < p; v++)
        {
            int expected = quadraticResidues.Contains(v) ? 1 : -1;
            if (nt.LegendreSymbol(v, p) != expected) legendreOk = false;
        }
        Console.WriteLine($"Символ Лежандра (p = 13) сверён с определением через квадраты: {(legendreOk ? "OK" : "ОШИБКА")}");
        Console.WriteLine($"Примеры: (2|7) = {nt.LegendreSymbol(2, 7)}, (3|7) = {nt.LegendreSymbol(3, 7)}, " +
                          $"(5|13) = {nt.LegendreSymbol(5, 13)}, (10|13) = {nt.LegendreSymbol(10, 13)}");

        // Символ Якоби на составных модулях
        Console.WriteLine($"Символ Якоби: (10|21) = {nt.JacobiSymbol(10, 21)}, (7|15) = {nt.JacobiSymbol(7, 15)}, " +
                          $"(2|15) = {nt.JacobiSymbol(2, 15)}, (6|35) = {nt.JacobiSymbol(6, 35)}");

        // Функция Эйлера тремя способами
        foreach (BigInteger n in new BigInteger[] { 15, 100, 97 })
        {
            BigInteger byDefinition = nt.EulerFunctionByDefinition(n);
            BigInteger byFactorization = nt.EulerFunctionByFactorization(n);
            BigInteger byFourier = nt.EulerFunctionByFourierTransform(n);
            Console.WriteLine($"φ({n}): по определению = {byDefinition}, по разложению = {byFactorization}, " +
                              $"через ДПФ = {byFourier} — {(byDefinition == byFactorization && byFactorization == byFourier ? "все три совпали" : "ОШИБКА")}");
        }

        // Функция Кармайкла + проверка a^λ ≡ 1 (mod n) для взаимно простых a
        foreach (BigInteger n in new BigInteger[] { 15, 100, 561 })
        {
            BigInteger lambda = nt.CarmichaelFunction(n);
            var coprimeBases = new List<BigInteger>();
            for (BigInteger baseValue = 2; coprimeBases.Count < 2 && baseValue < n; baseValue++)
                if (nt.GcdBinary(baseValue, n) == 1)
                    coprimeBases.Add(baseValue);

            bool holds = coprimeBases.All(baseValue => nt.ModPow(baseValue, lambda, n) == 1);
            Console.WriteLine($"λ({n}) = {lambda}; взаимно простые основания: {string.Join(", ", coprimeBases)} — " +
                              $"a^λ ≡ 1 (mod n): {(holds ? "подтверждено" : "ОШИБКА")}");
        }

        Console.WriteLine();
    }

    // ------------------------- Задание 2 -------------------------

    private static void DemoTask2()
    {
        Console.WriteLine("--- Задание 2. Вероятностные тесты простоты (шаблонный метод) ---");
        INumberTheoryService nt = new NumberTheoryService();
        var tests = new (string Name, IProbabilisticPrimalityTest Test)[]
        {
            ("Ферма",            new FermatPrimalityTest(nt)),
            ("Соловей–Штрассен", new SolovayStrassenPrimalityTest(nt)),
            ("Миллер–Рабин",     new MillerRabinPrimalityTest(nt))
        };

        Console.WriteLine("Число итераций t = ⌈ln(1 − p_min) / ln(ε)⌉ при ε = 1/2 (Ферма, СШ) и ε = 1/4 (МР):");
        foreach (double probability in new[] { 0.5, 0.9, 0.99, 0.999999 })
        {
            int fermatRounds = ComputeRoundCount(0.5, probability);
            int millerRounds = ComputeRoundCount(0.25, probability);
            Console.WriteLine($"  p_min = {probability,-9}: Ферма/Соловей–Штрассен t = {fermatRounds,3}   Миллер–Рабин t = {millerRounds,3}");
        }
        Console.WriteLine();

        var candidates = new (string Name, BigInteger Value)[]
        {
            ("7919 (простое)",                                7919),
            ("2147483647 = 2^31 − 1 (простое)",               2147483647),
            ("341 = 11·31 (псевдопростое по базе 2)",         341),
            ("2047 = 23·89 (сильное псевдопростое, база 2)",  2047),
            ("561 = 3·11·17 (число Кармайкла)",               561),
            ("1105 = 5·13·17 (число Кармайкла)",              1105),
            ("2^67 − 1 (псевдопростое по базе 2)",            (BigInteger.One << 67) - 1)
        };

        const double minProbability = 0.99;
        Console.WriteLine($"Проверка кандидатов при p_min = {minProbability} (true = «вероятно простое»):");
        foreach ((string name, BigInteger value) in candidates)
        {
            Console.Write($"  {name,-44}: ");
            foreach ((string testName, IProbabilisticPrimalityTest test) in tests)
            {
                bool isPrime = test.IsProbablyPrime(value, minProbability);
                Console.Write($"{testName}: {(isPrime ? "простое" : "составное")}   ");
            }
            Console.WriteLine();
        }
        Console.WriteLine("Каждый тест выполняет 7 итераций (Ферма/СШ) и 4 итерации (МР) со случайными основаниями; " +
                          "для чисел Кармайкла лжецами являются ВСЕ взаимно простые основания, поэтому Ферма уязвим по построению.");
        Console.WriteLine();
    }

    // ------------------------- Задание 3 -------------------------

    private static void DemoTask3()
    {
        Console.WriteLine("--- Задание 3. RSA: генерация ключей, шифрование, дешифрование ---");
        var service = new RsaCipherService(RsaCipherService.PrimalityTestType.MillerRabin, 0.99, 256);

        Stopwatch stopwatch = Stopwatch.StartNew();
        RsaKeyPair keys = service.GenerateKeyPair();
        stopwatch.Stop();

        int nBits = RsaCipherService.GetBitLength(keys.Public.Modulus);
        Console.WriteLine($"Ключи сгенерированы за {stopwatch.ElapsedMilliseconds} мс " +
                          $"(Миллер–Рабин, p_min = 0.99, простые по 256 бит):");
        Console.WriteLine($"  p = {keys.Private.PrimeP} ({RsaCipherService.GetBitLength(keys.Private.PrimeP)} бит)");
        Console.WriteLine($"  q = {keys.Private.PrimeQ} ({RsaCipherService.GetBitLength(keys.Private.PrimeQ)} бит)");
        Console.WriteLine($"  n = p·q  ({nBits} бит),  e = {keys.Public.Exponent},  d = {keys.Private.DecryptionExponent} ({RsaCipherService.GetBitLength(keys.Private.DecryptionExponent)} бит), d = e^(-1) (mod φ(n))");
        Console.WriteLine($"  φ(n) = (p−1)(q−1) = {keys.Private.EulerPhi},  λ(n) = НОК(p−1, q−1) = {keys.Private.CarmichaelLambda}");

        int halfBits = RsaCipherService.GetBitLength(keys.Private.PrimeP);
        BigInteger fermatThreshold = BigInteger.One << (3 * halfBits / 4);
        BigInteger fourthRoot = new NumberTheoryService().IntegerRoot(keys.Public.Modulus, 4);
        Console.WriteLine($"  Защита от атаки Ферма: |p − q| > 2^{3 * halfBits / 4} = {fermatThreshold} — " +
                          $"{(BigInteger.Abs(keys.Private.PrimeP - keys.Private.PrimeQ) > fermatThreshold ? "OK" : "ОШИБКА")}");
        Console.WriteLine($"  Защита от атаки Винера: 3·d > n^(1/4) = {fourthRoot} — " +
                          $"{(3 * keys.Private.DecryptionExponent > fourthRoot ? "OK" : "ОШИБКА")}");

        string plaintext = "Криптография — это наука о математических методах обеспечения конфиденциальности и целостности данных.";
        byte[] data = Encoding.UTF8.GetBytes(plaintext);
        byte[] cipher = service.Encrypt(data, keys.Public);
        byte[] restored = service.Decrypt(cipher, keys.Private);
        Console.WriteLine($"\nШифрование строки ({data.Length} байт): {cipher.Length} байт шифртекста; первые 32 байта:");
        Console.WriteLine($"  {ToHex(cipher, 32)}…");
        Console.WriteLine($"Дешифрование: {(restored.SequenceEqual(data) ? "OK — текст восстановлен бит-в-бит" : "ОШИБКА")}");

        byte[] randomData = new byte[1000];
        PseudoRandom.NextBytes(randomData);
        byte[] randomCipher = service.Encrypt(randomData, keys.Public);
        byte[] randomRestored = service.Decrypt(randomCipher, keys.Private);
        Console.WriteLine($"Шифрование 1000 случайных байт: {randomCipher.Length} байт шифртекста, " +
                          $"раунд-трип: {(randomRestored.SequenceEqual(randomData) ? "OK" : "ОШИБКА")}");

        // та же схема с другими тестами простоты
        foreach (RsaCipherService.PrimalityTestType testType in new[]
                 {
                     RsaCipherService.PrimalityTestType.Fermat,
                     RsaCipherService.PrimalityTestType.SolovayStrassen
                 })
        {
            var altService = new RsaCipherService(testType, 0.99, 256);
            RsaKeyPair altKeys = altService.GenerateKeyPair();
            byte[] altCipher = altService.Encrypt(data, altKeys.Public);
            byte[] altRestored = altService.Decrypt(altCipher, altKeys.Private);
            Console.WriteLine($"Генерация ключей тестом {testType}: d ({RsaCipherService.GetBitLength(altKeys.Private.DecryptionExponent)} бит), " +
                              $"шифрование/дешифрование: {(altRestored.SequenceEqual(data) ? "OK" : "ОШИБКА")}");
        }

        Console.WriteLine();
    }

    // ------------------------- Задание 4 -------------------------

    private static void DemoTask4()
    {
        Console.WriteLine("--- Задание 4. Атака Винера ---");
        var vulnerableGenerator = new VulnerableRsaKeyPairGenerator(0.99);
        RsaKeyPair weak = vulnerableGenerator.GenerateVulnerableKeyPair(256);

        Console.WriteLine($"Уязвимая ключевая пара (простые по 256 бит):");
        Console.WriteLine($"  e = {weak.Public.Exponent} ({RsaCipherService.GetBitLength(weak.Public.Exponent)} бит)");
        Console.WriteLine($"  d = {weak.Private.DecryptionExponent} ({RsaCipherService.GetBitLength(weak.Private.DecryptionExponent)} бит) — " +
                          $"d < n^(1/4)/3 = {(new NumberTheoryService().IntegerRoot(weak.Public.Modulus, 4) / 3)}");

        var attackService = new WienerAttackService(new NumberTheoryService());
        WienerAttackResult result = attackService.Attack(weak.Public);

        Console.WriteLine($"Атака Винера:");
        Console.WriteLine($"  Подходящих дробей e/n вычислено: {result.Convergents.Count}");
        Console.WriteLine($"  Кандидатов d проверено: {result.VerifiedCandidates}");
        Console.WriteLine($"  Первые подходящие дроби: {string.Join(", ", result.Convergents.Take(6))} …");
        Console.WriteLine($"  Атака успешна: {result.Success}");
        Console.WriteLine($"  d  восстановлено верно: {(result.Success && result.DecryptionExponent == weak.Private.DecryptionExponent ? "OK" : "ОШИБКА")}");
        Console.WriteLine($"  φ(n) восстановлено верно: {(result.Success && result.EulerPhi == weak.Private.EulerPhi ? "OK" : "ОШИБКА")}");

        // расшифрование конкретного сообщения взломанным ключом
        BigInteger message = 123456789;
        BigInteger cipher = new NumberTheoryService().ModPow(message, weak.Public.Exponent, weak.Public.Modulus);
        BigInteger recovered = new NumberTheoryService().ModPow(cipher, result.DecryptionExponent, weak.Public.Modulus);
        Console.WriteLine($"  Расшифрование сообщения {message} взломанным d: {(recovered == message ? "OK — атака полностью функциональна" : "ОШИБКА")}");

        // атака на защищённый ключ должна провалиться
        var secureService = new RsaCipherService(RsaCipherService.PrimalityTestType.MillerRabin, 0.99, 256);
        RsaKeyPair secure = secureService.GenerateKeyPair();
        WienerAttackResult failed = attackService.Attack(secure.Public);
        Console.WriteLine($"  Атака на защищённый ключ (d ≈ λ(n)): Success = {failed.Success} " +
                          $"{(failed.Success ? "ОШИБКА — не должно быть!" : "(ожидаемый результат: d слишком велик)")}");
        Console.WriteLine();
    }

    // ------------------------- Помощники -------------------------

    private static int ComputeRoundCount(double oneRoundError, double minProbability)
    {
        double raw = Math.Log(1.0 - minProbability) / Math.Log(oneRoundError);
        return Math.Max((int)Math.Ceiling(raw - 1e-9), 1);
    }

    private static string ToHex(byte[] bytes, int limit)
    {
        int count = Math.Min(limit, bytes.Length);
        var builder = new StringBuilder(count * 2);
        for (int i = 0; i < count; i++)
            builder.Append(bytes[i].ToString("X2"));
        return builder.ToString();
    }
}
