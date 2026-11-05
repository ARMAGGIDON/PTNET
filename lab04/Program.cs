using System.Globalization;
using System.Text;

namespace lab04 {
internal sealed class ConsoleKeyPressedEventArgs(ConsoleKeyInfo keyInfo) : EventArgs { public ConsoleKeyInfo KeyInfo { get; } = keyInfo; }

internal sealed class IntegralCalculatedEventArgs(int functionNumber, double a, double b, int steps, double result) : EventArgs {
    public int FunctionNumber { get; } = functionNumber;
    public double A { get; } = a;
    public double B { get; } = b;
    public int Steps { get; } = steps;
    public double Result { get; } = result;
}

internal sealed class KeyboardController {
    private bool _isRunning = true;
    public event EventHandler<ConsoleKeyPressedEventArgs>? KeyPressed;

    public void Start() {
        _isRunning = true;
        while (_isRunning) {
            var keyInfo = Console.ReadKey(intercept: true);
            KeyPressed?.Invoke(this, new ConsoleKeyPressedEventArgs(keyInfo));
        }
    }
    public void Stop() { _isRunning = false; }
}

internal sealed class RightRectangleIntegrator  {
    public event EventHandler<IntegralCalculatedEventArgs>? IntegralCalculated;

    public double Calculate(Func<double, double> function, double a, double b, int steps, int functionNumber) {
        ArgumentNullException.ThrowIfNull(function);
        if (steps <= 0)
            throw new ArgumentOutOfRangeException(nameof(steps), "Кількість кроків має бути більшою за 0.");
        if (a > b)
            throw new ArgumentException("Права межа b повинна бути більшою за ліву межу a.");

        var h = (b - a) / steps;
        var sum = 0.0;
        for (var i = 1; i <= steps; i++) {
            var x = a + i * h;
            var fx = function(x);

            if (double.IsNaN(fx) || double.IsInfinity(fx))
                throw new ArithmeticException($"Функція повернула некоректне значення в точці x = {x}.");

            sum += fx;
        }

        var result = sum * h;
        IntegralCalculated?.Invoke(this, new IntegralCalculatedEventArgs(functionNumber, a, b, steps, result));
        return result;
    }
}

internal static class FunctionFactory {
    public static Func<double, double> GetFunction(int number) {
        return number switch {
            1 => x => 1.0 / Math.Pow(x, 3),
            2 => x => 1.0 / Math.Sqrt(x),
            3 => x => Math.Exp(-x) + 1.0,
            4 => x => x,
            _ => throw new ArgumentOutOfRangeException(nameof(number), "Невідомий номер функції.")
        };
    }

    public static string GetFunctionText(int number) {
        return number switch {
            1 => "f(x) = 1 / x^3",
            2 => "f(x) = 1 / sqrt(x)",
            3 => "f(x) = e^(-x) + 1",
            4 => "f(x) = x",
            _ => "Невідома функція"
        };
    }

    public static void ValidateRange(int number, double a, double b) {
        if (a > b)
            throw new ArgumentException("Ліва межа a не може бути більшою за праву межу b.");

        switch (number) {
            case 1:
                if (a <= 0 && b >= 0)
                    throw new ArgumentException("Для f(x) = 1 / x^3 відрізок [a; b] не повинен містити x = 0.");
                break;

            case 2:
                if (a <= 0 || b <= 0)
                    throw new ArgumentException("Для f(x) = 1 / sqrt(x) потрібно, щоб a > 0 і b > 0.");
                break;

            case 3:
            case 4:
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(number), "Невідомий номер функції.");
        }
    }
}


internal enum AppMode {
    Menu,
    InputA,
    InputB,
    InputN,
    WaitAfterHelp,
    WaitAfterError,
    WaitAfterResult,
    WaitBeforeExit
}


internal class Program {
    private readonly KeyboardController _keyboard = new();
    private readonly RightRectangleIntegrator _integrator = new();
    private readonly StringBuilder _inputBuffer = new();

    private static readonly AppMode[] InputModes = [AppMode.InputA, AppMode.InputB, AppMode.InputN];

    private const string ContinueText = "Натисніть будь-яку клавішу для продовження...";
    private const string ExitText = "Натисніть будь-яку клавішу для виходу...";

    private AppMode _mode = AppMode.Menu;
    private AppMode _modeBeforePause = AppMode.Menu;
    private AppMode _errorReturnMode = AppMode.Menu;

    private int? _selectedFunctionNumber;
    private double _aValue;
    private double _bValue;
    private int _stepsValue;

    private string _aText = string.Empty;
    private string _bText = string.Empty;
    private string _nText = string.Empty;

    private int _baseBottomTop;
    private int _formStartTop = -1;
    private int _formBottomTop = -1;
    private int _activeLineTop = -1;
    private int _tempStartTop = -1;
    private int _tempEndTop = -1;

    private int _savedCursorLeft;
    private int _savedCursorTop;

    private static void Main() {
        Program program = new();
        program.Run();
    }

    private void Run() {
        _keyboard.KeyPressed += OnKeyPressed;
        _integrator.IntegralCalculated += OnIntegralCalculated;

        Console.OutputEncoding = Encoding.UTF8;
        Console.CursorVisible = false;

        ResetFormState();
        RedrawScreen(clearConsole: true);
        _keyboard.Start();
    }

    private static void Cout(string text) { Console.Write(text); }
    private static void CLine(string text = "") { Console.WriteLine(text); }

    private static void Colored(string text, ConsoleColor? color = null, bool endLine = false) {
        if (color is not null)
            Console.ForegroundColor = color.Value;
        else
            Console.ResetColor();
        Cout(text);
        if (endLine)
            CLine();
        Console.ResetColor();
    }

    private void OnKeyPressed(object? sender, ConsoleKeyPressedEventArgs e) {
        if (_mode is AppMode.WaitAfterHelp or AppMode.WaitAfterError or AppMode.WaitAfterResult or AppMode.WaitBeforeExit) {
            switch (_mode) {
                case AppMode.WaitAfterHelp:
                    ClearPauseArea();
                    _mode = _modeBeforePause;

                    if (IsInputMode(_mode))
                        PlaceCursorToActiveInputEnd();
                    else
                        RestoreCursor();
                    break;

                case AppMode.WaitAfterError:
                    ClearPauseArea();

                    if (_selectedFunctionNumber is null) {
                        ResetFormState();
                        RedrawScreen(clearConsole: true);
                        return;
                    }

                    _mode = _errorReturnMode;

                    if (_inputBuffer.Length == 0)
                        _inputBuffer.Append(GetFieldView(_errorReturnMode).SavedValue);

                    RenderCurrentForm();
                    break;

                case AppMode.WaitAfterResult:
                    ResetFormState();
                    RedrawScreen(clearConsole: true);
                    break;

                case AppMode.WaitBeforeExit:
                    _keyboard.Stop();
                    break;
            }
            return;
        }

        if (IsInputMode(_mode)) {
            switch (e.KeyInfo.Key) {
                case ConsoleKey.V:
                case ConsoleKey.M:
                    ShowName();
                    return;

                case ConsoleKey.C:
                    RedrawScreen(clearConsole: true);
                    return;

                case ConsoleKey.Escape:
                    ShowPause(
                        AppMode.WaitBeforeExit,
                        GetSafeOutputTop(),
                        (ConsoleColor.Yellow, ExitText)
                    );
                    return;

                case ConsoleKey.Backspace:
                    if (_inputBuffer.Length > 0) {
                        _inputBuffer.Remove(_inputBuffer.Length - 1, 1);
                        RewriteActiveInputLine();
                    }
                    return;

                case ConsoleKey.Enter:
                    if (_selectedFunctionNumber is null || !IsInputMode(_mode))
                        return;
                    Console.CursorVisible = false;

                    try {
                        var currentText = _inputBuffer.ToString();

                        _errorReturnMode = _mode;
                        switch (_mode) {
                            case AppMode.InputA:
                                _aValue = ParseDouble(currentText, "a");
                                _aText = currentText;
                                break;

                            case AppMode.InputB:
                                _bValue = ParseDouble(currentText, "b");
                                _bText = currentText;

                                try {
                                    FunctionFactory.ValidateRange(_selectedFunctionNumber.Value, _aValue, _bValue);
                                } catch {
                                    _errorReturnMode = AppMode.InputA;
                                    _inputBuffer.Clear();
                                    throw;
                                }

                                break;

                            case AppMode.InputN:
                                if (string.IsNullOrWhiteSpace(currentText) || !int.TryParse(currentText, out var value))
                                    throw new ArgumentException($"Значення '{currentText}' не є коректним цілим числом для поля n.");
                                if (value <= 0)
                                    throw new ArgumentException($"Значення '{currentText}' не є цілим числом, більшим за 0, для поля n.");
                                _stepsValue = value;
                                _nText = currentText;
                                break;

                            default:
                                throw new InvalidOperationException("Непідтримуване поле вводу.");
                        }
                        _inputBuffer.Clear();

                        if (_mode != AppMode.InputN) {
                            _mode = _mode switch {
                                AppMode.InputA => AppMode.InputB,
                                AppMode.InputB => AppMode.InputN,
                                _ => throw new InvalidOperationException("Наступний режим для цього поля відсутній.")
                            };
                            RenderCurrentForm();
                            return;
                        }

                        _errorReturnMode = AppMode.InputA;

                        Console.SetCursorPosition(0, _formBottomTop + 1);

                        FunctionFactory.ValidateRange(_selectedFunctionNumber.Value, _aValue, _bValue);

                        var function = FunctionFactory.GetFunction(_selectedFunctionNumber.Value);
                        _integrator.Calculate(function, _aValue, _bValue, _stepsValue, _selectedFunctionNumber.Value);

                        CLine();

                        ShowPause(
                            AppMode.WaitAfterResult,
                            Console.CursorTop,
                            (ConsoleColor.Yellow, ContinueText)
                        );
                    } catch (Exception ex) {
                        ShowPause(
                            AppMode.WaitAfterError,
                            Math.Max(_formBottomTop + 1, Console.CursorTop),
                            (ConsoleColor.Red, $"Помилка: {ex.Message}"),
                            (ConsoleColor.Yellow, ContinueText)
                        );
                    }
                    return;
            }

            if (e.KeyInfo.KeyChar != '\0' && !char.IsControl(e.KeyInfo.KeyChar))
                _inputBuffer.Append(e.KeyInfo.KeyChar);
            RewriteActiveInputLine();
            return;
        }

        switch (e.KeyInfo.Key) {
            case ConsoleKey.D1:
            case ConsoleKey.NumPad1:
                StartFunctionInput(1);
                return;

            case ConsoleKey.D2:
            case ConsoleKey.NumPad2:
                StartFunctionInput(2);
                return;

            case ConsoleKey.D3:
            case ConsoleKey.NumPad3:
                StartFunctionInput(3);
                return;

            case ConsoleKey.D4:
            case ConsoleKey.NumPad4:
                StartFunctionInput(4);
                return;

            case ConsoleKey.V:
            case ConsoleKey.M:
                ShowName();
                return;

            case ConsoleKey.C:
                ResetFormState();
                RedrawScreen(clearConsole: true);
                return;

            case ConsoleKey.Escape:
                ShowPause(
                    AppMode.WaitBeforeExit,
                    GetSafeOutputTop(),
                    (ConsoleColor.Yellow, ExitText)
                );
                return;

            default:
                _modeBeforePause = _mode;
                SaveCursor();

                ShowPause(
                    AppMode.WaitAfterHelp,
                    GetSafeOutputTop(),
                    (ConsoleColor.Red, $"Натиснуто недопустиму клавішу: {FormatKey(e.KeyInfo)}"),
                    (null, "Доступні клавіші:"),
                    (null, "1, 2, 3 - функції варіанта 8"),
                    (null, "4 - f(x) = x, аналіз похибки"),
                    (null, "M - вивести ім'я"),
                    (null, "C - очистити консоль"),
                    (null, "Esc - вихід"),
                    (ConsoleColor.Yellow, ContinueText)
                );
                return;
        }
    }

    private static void OnIntegralCalculated(object? sender, IntegralCalculatedEventArgs e) {
        Console.ResetColor();
        CLine("=== Результат ===");
        CLine($"Функція: {FunctionFactory.GetFunctionText(e.FunctionNumber)}");
        CLine($"Проміжок: [{e.A}; {e.B}]");
        CLine($"Кількість кроків: {e.Steps}");
        CLine($"Інтеграл методом правих прямокутників ≈ {e.Result}");

        if (e.FunctionNumber != 4)
            return;

        var exact = (Math.Pow(e.B, 2) - Math.Pow(e.A, 2)) / 2.0;
        var error = Math.Abs(exact - e.Result);

        CLine();
        CLine("=== Аналіз похибки ===");
        CLine($"Аналітичне значення: {exact}");
        CLine($"Абсолютна похибка: {error}");
    }

    private void StartFunctionInput(int functionNumber) {
        _selectedFunctionNumber = functionNumber;

        _aValue = 0;
        _bValue = 0;
        _stepsValue = 0;

        _aText = string.Empty;
        _bText = string.Empty;
        _nText = string.Empty;

        _inputBuffer.Clear();
        _mode = AppMode.InputA;

        if (Console.CursorLeft != 0)
            CLine();

        _formStartTop = Console.CursorTop;
        _formBottomTop = _formStartTop;

        RenderCurrentForm();
    }

    private void RedrawScreen(bool clearConsole) {
        Console.CursorVisible = false;
        if (clearConsole)
            Console.Clear();

        Console.ResetColor();
        CLine("=== Обчислення інтеграла методом правих прямокутників ===");
        CLine("1 - f(x) = 1 / x^3");
        CLine("2 - f(x) = 1 / sqrt(x)");
        CLine("3 - f(x) = e^(-x) + 1");
        CLine("4 - f(x) = x (аналіз похибки)");
        CLine();
        CLine("Оберіть функцію.");
        CLine();
        _baseBottomTop = Console.CursorTop - 1;

        if (_selectedFunctionNumber is null) {
            _mode = AppMode.Menu;
            return;
        }

        _formStartTop = Console.CursorTop;
        _formBottomTop = _formStartTop;
        RenderCurrentForm();
    }

    private void RenderCurrentForm() {
        if (_selectedFunctionNumber is null)
            return;

        if (_formStartTop < 0)
            _formStartTop = Console.CursorTop;

        if (_formBottomTop >= _formStartTop)
            ClearLines(_formStartTop, _formBottomTop);

        Console.SetCursorPosition(0, _formStartTop);

        Console.ResetColor();
        CLine($"Функція: {FunctionFactory.GetFunctionText(_selectedFunctionNumber.Value)}");
        CLine("Введіть межі інтегрування та кількість кроків:");

        foreach (var fieldMode in InputModes) {
            var (prompt, savedValue) = GetFieldView(fieldMode);
            var isActive = _mode == fieldMode;
            var lineTop = Console.CursorTop;

            Console.ResetColor();
            Cout(prompt);
            Colored(isActive ? _inputBuffer.ToString() : savedValue, ConsoleColor.Green, true);

            if (isActive)
                _activeLineTop = lineTop;
        }
        CLine();

        _formBottomTop = Console.CursorTop - 1;
        PlaceCursorToActiveInputEnd();
    }

    private void RewriteActiveInputLine() {
        if (!IsInputMode(_mode) || _activeLineTop < 0)
            return;

        var (prompt, _) = GetFieldView(_mode);
        var value = _inputBuffer.ToString();

        ClearLines(_activeLineTop, _activeLineTop);
        Console.SetCursorPosition(0, _activeLineTop);

        Console.ResetColor();
        Cout(prompt);

        Colored(value, ConsoleColor.Green);

        PlaceCursorToActiveInputEnd();
    }

    private (string Prompt, string SavedValue) GetFieldView(AppMode fieldMode) {
        return fieldMode switch {
            AppMode.InputA => ("a = ", _aText),
            AppMode.InputB => ("b = ", _bText),
            AppMode.InputN => ("n = ", _nText),
            _ => throw new InvalidOperationException("Непідтримуваний режим поля.")
        };
    }

    private void ShowPause(AppMode pauseMode, int startTop, params (ConsoleColor? Color, string Text)[] lines) {
        Console.CursorVisible = false;

        _tempStartTop = startTop;
        Console.SetCursorPosition(0, _tempStartTop);

        foreach (var (color, text) in lines)
            Colored(text, color, true);

        _tempEndTop = Console.CursorTop - 1;
        _mode = pauseMode;
    }

    private void ClearPauseArea() {
        ClearLines(_tempStartTop, _tempEndTop);
        _tempStartTop = -1;
        _tempEndTop = -1;
    }

    private void ResetFormState() {
        _mode = AppMode.Menu;
        _modeBeforePause = AppMode.Menu;
        _errorReturnMode = AppMode.Menu;

        _selectedFunctionNumber = null;

        _aValue = 0;
        _bValue = 0;
        _stepsValue = 0;

        _aText = string.Empty;
        _bText = string.Empty;
        _nText = string.Empty;

        _inputBuffer.Clear();

        _formStartTop = -1;
        _formBottomTop = -1;
        _activeLineTop = -1;
        _tempStartTop = -1;
        _tempEndTop = -1;
    }

    private void ShowName() {
        SaveCursor();
        var writeTop = GetSafeOutputTop();

        ClearLines(writeTop, writeTop);
        Console.SetCursorPosition(0, writeTop);

        Console.ResetColor();
        Cout("Повне ім'я: ");
        Colored("Сідько Михайло Сергійович", ConsoleColor.Green);

        if (IsInputMode(_mode))
            PlaceCursorToActiveInputEnd();
        else
            RestoreCursor();
    }

    private void PlaceCursorToActiveInputEnd() {
        if (!IsInputMode(_mode) || _activeLineTop < 0)
            return;
        var (prompt, _) = GetFieldView(_mode);
        Console.CursorVisible = true;
        Console.SetCursorPosition(prompt.Length + _inputBuffer.Length, _activeLineTop);
    }

    private int GetSafeOutputTop() {
        if (IsInputMode(_mode) || _mode == AppMode.WaitAfterHelp || _mode == AppMode.WaitAfterError)
            return Math.Max(_formBottomTop + 1, Console.CursorTop);
        return Math.Max(_baseBottomTop + 1, Console.CursorTop);
    }

    private static bool IsInputMode(AppMode value) { return value is AppMode.InputA or AppMode.InputB or AppMode.InputN; }

    private void SaveCursor() {
        _savedCursorLeft = Console.CursorLeft;
        _savedCursorTop = Console.CursorTop;
    }

    private void RestoreCursor() { Console.SetCursorPosition(Math.Max(0, Math.Min(_savedCursorLeft, Console.BufferWidth - 1)), Math.Max(0, Math.Min(_savedCursorTop, Console.BufferHeight - 1))); }

    private static double ParseDouble(string text, string fieldName) {
        if (string.IsNullOrWhiteSpace(text) || !double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            throw new ArgumentException($"Значення '{text}' не є коректним числом для поля {fieldName}.");
        return value;
    }

    private static string FormatKey(ConsoleKeyInfo keyInfo) {
        if (!char.IsControl(keyInfo.KeyChar) && keyInfo.KeyChar != '\0')
            return $"'{keyInfo.KeyChar}'";
        return keyInfo.Key.ToString();
    }

    private static void ClearLines(int fromTop, int toTop) {
        if (fromTop < 0 || toTop < fromTop)
            return;

        string emptyLine = new(' ', Math.Max(1, Console.BufferWidth - 1));
        for (var row = fromTop; row <= toTop; row++) {
            Console.SetCursorPosition(0, row);
            Cout(emptyLine);
        }
        Console.SetCursorPosition(0, fromTop);
    }
}
}