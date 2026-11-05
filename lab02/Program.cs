using System.Globalization;
using System.Reflection;
using System.Text;

namespace lab02 {
internal static class Program {
    private static void Main() {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        Console.CursorVisible = false;

        var assembly = Assembly.Load("lab02DLL");
        var functionsType = assembly.GetType("lab02DLL.Functions")
            ?? throw new TypeLoadException("Клас lab02DLL.Functions не знайдено.");

        WriteColored("1. Підрахунок входжень символу\n", ConsoleColor.Cyan);

        var sentence = GetIfString("Введіть речення: ", IsSentence);
        var symbol = GetIfChar("Введіть символ: ", IsChar);

        var count = (int)(InvokeStatic(functionsType, "CountCharacter", sentence, symbol)
            ?? throw new InvalidOperationException("CountCharacter не повернув результат."));

        CLine("\nРезультат:");
        WriteColored("Речення: ", ConsoleColor.DarkGray);
        PrintSentence(sentence, symbol);
        WriteColored("Символ: ", ConsoleColor.DarkGray);
        WriteColored($"'{symbol}'\n", ConsoleColor.Yellow);
        WriteColored("Кількість входжень: ", ConsoleColor.DarkGray);
        Success($"{count}\n");

        CLine();
        WriteColored("2. Заповнення масиву випадковими числами\n", ConsoleColor.Cyan);

        var size = GetIfInt("Введіть розмір масиву: ", IsArraySize);

        // Передається int, тому InvokeMember автоматично вибирає
        // перевантаження FillRandomArray(int size), яке повертає масив.
        // Якщо передати int[], буде автоматично вибрано FillRandomArray(int[] array).
        var array = (int[])(InvokeStatic(functionsType, "FillRandomArray", size)
                            ?? throw new InvalidOperationException("FillRandomArray не повернув масив."));

        var columns = GetOptimalColumns(array.Length);
        var rows = (int)Math.Ceiling((double)array.Length / columns);

        CLine("\nРезультат:");
        WriteColored($"Масив [{array.Length}]", ConsoleColor.DarkGray);
        WriteColored($" ({columns} × {rows}):\n", ConsoleColor.DarkGray);
        PrintArray(array, columns);

        CLine();
        ClearPreviousLines(0, "Натисніть будь-яку клавішу для виходу...");
        return;

        int IsArraySize(string input) {
            var value = GetInt(input);
            return value < 1 ? throw new InvalidFormatException("Помилка: розмір масиву повинен бути не менше 1.") : value;
        }

        char IsChar(string input) {
            return input.Length != 1 ? throw new InvalidFormatException("Помилка: введіть рівно один символ.") : input[0];
        }

        string IsSentence(string input) {
            return string.IsNullOrWhiteSpace(input) ? throw new InvalidFormatException("Помилка: речення не може бути порожнім.") : input;
        }
    }

    private static object? InvokeStatic(Type type, string methodName, params object?[] args) {
        return type.InvokeMember(methodName,
            BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static,
            binder: null, target: null, args: args
        );
    }

    private static void PrintSentence(string sentence, char symbol) {
        foreach (var value in sentence) {
            if (value == symbol) {
                WriteColored(value.ToString(), ConsoleColor.Yellow);
            } else {
                Cout(value.ToString());
            }
        }
        CLine();
    }

    private static int GetOptimalColumns(int count) {
        int[] variants = [5, 10, 15];

        return variants.Select(columns => new {
                Columns = columns,
                Rows = (int)Math.Ceiling((double)count / columns)
            })
            .OrderBy(layout => Math.Abs(layout.Columns - layout.Rows))
            .ThenBy(layout => layout.Rows)
            .First()
            .Columns;
    }
    private static void PrintArray(int[] array, int columns) {
        const int valueWidth = 4;

        for (var i = 0; i < array.Length; i++) {
            PrintArrayValue(array[i], valueWidth);

            if ((i + 1) % columns == 0 || i == array.Length - 1) {
                Console.WriteLine();
            } else {
                Console.Write(" ");
            }
        }
    }

    private static void PrintArrayValue(int value, int width) {
        var color = value switch {
            < 0 => ConsoleColor.Red,
            0 => ConsoleColor.Blue,
            > 0 => ConsoleColor.Green
        };

        WriteColored(value.ToString().PadLeft(width), color);
    }

    private static void Cout(string message = "") { Console.Write(message); }

    private static void CLine(string message = "") { Console.WriteLine(message); }

    private static string Cin(string message) {
        Cout(message);
        Console.CursorVisible = true;
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        string input = Console.ReadLine() ?? string.Empty;
        Console.CursorVisible = false;
        Console.ResetColor();
        return input;
    }

    private static void WriteColored(string message, ConsoleColor color) {
        Console.ForegroundColor = color;
        Cout(message);
        Console.ResetColor();
    }

    private static void Error(string message) => WriteColored(message, ConsoleColor.Red);

    private static void Warning(string message) => WriteColored(message, ConsoleColor.Yellow);

    private static void Success(string message) => WriteColored(message, ConsoleColor.Green);

    private static void ClearPreviousLines(int linesCount = 0, string text = "Натисніть будь-яку клавішу для продовження...") {
        Warning(text);
        linesCount++;
        Console.ReadKey();
        for (int i = 0; i < linesCount; i++) {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));

            // Повертаємо курсор на початок попереднього рядка, при умові, що це не останній рядок
            // Якщо це останній рядок, переміщаємо курсор на початок поточного рядка, щоб не залишати порожні рядки після очищення
            if (i != linesCount - 1) {
                Console.SetCursorPosition(0, Console.CursorTop - 1);
            } else {
                Console.SetCursorPosition(0, Console.CursorTop);
            }
        }
    }

    private static int GetInt(string input) {
        // перевіряємо чи дане значення цілим числом, якщо ні, то викидаємо помилку, якщо так, то повертаємо це число
        return !(int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)) ? throw new InvalidFormatException("Помилка: введіть ціле число.") : value;
    }

    private static int GetIfInt(string reMsg, Func<string, int> func) {
        int value;
        var (_, startTop) = Console.GetCursorPosition();
        while (true) {
            try {
                value = func(Cin(reMsg));
                break;
            } catch (InvalidFormatException err) {
                Error($"{err.Message}\r\n");
                var (_, top) = Console.GetCursorPosition();
                ClearPreviousLines(top - startTop);
            }
        }
        return value;
    }

    private static string GetIfString(string reMsg, Func<string, string> func) {
        string value;
        var (_, startTop) = Console.GetCursorPosition();
        while (true) {
            try {
                value = func(Cin(reMsg));
                break;
            } catch (InvalidFormatException err) {
                Error($"{err.Message}\r\n");
                var (_, top) = Console.GetCursorPosition();
                ClearPreviousLines(top - startTop);
            }
        }
        return value;
    }

    private static char GetIfChar(string reMsg, Func<string, char> func) {
        char value;
        var (_, startTop) = Console.GetCursorPosition();
        while (true) {
            try {
                value = func(Cin(reMsg));
                break;
            } catch (InvalidFormatException err) {
                Error($"{err.Message}\r\n");
                var (_, top) = Console.GetCursorPosition();
                ClearPreviousLines(top - startTop);
            }
        }
        return value;
    }
}

internal class InvalidFormatException(string message) : FormatException(message) { }
}