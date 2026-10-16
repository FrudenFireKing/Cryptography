## Structure of Lab1

    CryptoLab1/
        CryptoLab1.csproj
        Program.cs                              — демонстрация (задания 5 и 7)
        Core/
            BitEnums.cs                         — enum'ы правил индексации битов
            BitOperations.cs                    — ЗАДАНИЕ 1 (функция перестановки битов)
            CipherEnums.cs                      — enum'ы режимов шифрования и набивки
            IKeyExpansion.cs                    — ЗАДАНИЕ 2.1 (интерфейс расширения ключа)
            IRoundFunction.cs                   — ЗАДАНИЕ 2.2 (интерфейс раундовой функции)
            ISymmetricCipher.cs                 — ЗАДАНИЕ 2.3 (интерфейс [де]шифрования)
            Padding.cs                          — режимы набивки (для 2.4)
            SimpleRandom.cs                     — собственный PRNG (для IV и набивки)
            CipherContext.cs                    — ЗАДАНИЕ 2.4 (контекст: режимы, async, файлы)
        Feistel/
            FeistelNetwork.cs                   — ЗАДАНИЕ 3 (сеть Фейстеля)
        Des/
            DesTables.cs                        — таблицы DES (IP, FP, E, P, PC-1, PC-2, S-блоки)
            DesKeyExpansion.cs                  — ЗАДАНИЕ 4 (реализация 2.1 для DES)
            DesRoundFunction.cs                 — ЗАДАНИЕ 4 (реализация 2.2 для DES)
            DesCipher.cs                        — ЗАДАНИЕ 4 (DES на базе сети Фейстеля)
        Deal/
            BlockCipherRoundFunctionAdapter.cs  — ЗАДАНИЕ 6 (адаптер «шифр → раундовая функция F»)
            DealKeyExpansion.cs                 — ЗАДАНИЕ 6 (реализация 2.1 для DEAL)
            DealCipher.cs                       — ЗАДАНИЕ 6 (DEAL: блок 128 бит)

---
### Запук программы и тестов:
```bash
dotnet run
