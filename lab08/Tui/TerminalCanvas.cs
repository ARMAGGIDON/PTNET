namespace lab08.Tui;

// Windows Terminal: окремий екран для TUI, історія PowerShell залишається недоторканою.
internal sealed class TerminalCanvas : IDisposable {
    public const int Width = 96;
    private const int Height = 27;
    private const int MinimumWidth = 100;
    private const int MinimumHeight = 30;
    private const int Left = 2;
    private const int Top = 1;

    private readonly Cell[,] _current = new Cell[Height, Width];
    private readonly Cell[,] _previous = new Cell[Height, Width];
    private bool _invalidated = true;
    private bool _disposed;

    private readonly record struct Cell(char Character, ConsoleColor Foreground, ConsoleColor Background);

    public static bool Fits => Console.WindowWidth >= MinimumWidth && Console.WindowHeight >= MinimumHeight;

    public TerminalCanvas() {
        // DECSET 1049 перемикає на альтернативний буфер, не створюючи записів у scrollback.
        Console.Write("\u001b[?1049h\u001b[2J\u001b[H");
        Console.CursorVisible = false;
    }

    public void ResetDisplay() {
        // При зміні розміру вікна старе зображення може бути пошкоджене reflow.
        Console.CursorVisible = false;
        Console.ResetColor();
        Console.Write("\u001b[2J\u001b[H");
        Invalidate();
    }

    public void BeginFrame() {
        for (var y = 0; y < Height; y++) {
            for (var x = 0; x < Width; x++) _current[y, x] = new Cell(' ', ConsoleColor.Gray, ConsoleColor.Black);
        }
    }

    public void Put(int row, int column, string text, ConsoleColor fg = ConsoleColor.Gray,
        ConsoleColor bg = ConsoleColor.Black) {
        if (row < 0 || row >= Height || string.IsNullOrEmpty(text)) return;
        for (var i = 0; i < text.Length; i++) {
            var x = column + i;
            if (x >= 0 && x < Width) _current[row, x] = new Cell(text[i], fg, bg);
        }
    }

    public void Fill(int row, int column, int count, ConsoleColor fg, ConsoleColor bg) {
        if (row < 0 || row >= Height) return;
        for (var x = Math.Max(0, column); x < Math.Min(Width, column + count); x++) {
            _current[row, x] = new Cell(_current[row, x].Character, fg, bg);
        }
    }

    public void Line(int row) => Put(row, 0, new string('─', Width), ConsoleColor.DarkGray);

    public void Render() {
        Console.CursorVisible = false;
        for (var row = 0; row < Height; row++) {
            var changed = _invalidated;
            for (var x = 0; !changed && x < Width; x++) changed = !_current[row, x].Equals(_previous[row, x]);
            if (!changed) continue;

            Console.SetCursorPosition(Left, Top + row);
            var offset = 0;
            while (offset < Width) {
                var style = _current[row, offset];
                var end = offset + 1;
                while (end < Width && _current[row, end].Foreground == style.Foreground &&
                       _current[row, end].Background == style.Background) end++;

                Console.ForegroundColor = style.Foreground;
                Console.BackgroundColor = style.Background;
                var chars = new char[end - offset];
                for (var i = offset; i < end; i++) chars[i - offset] = _current[row, i].Character;
                Console.Write(chars);
                offset = end;
            }

            for (var x = 0; x < Width; x++) _previous[row, x] = _current[row, x];
        }

        Console.ResetColor();
        _invalidated = false;
    }

    public void ShowCaret(int row, int column) {
        if (!Fits) return;
        Console.SetCursorPosition(Left + Math.Clamp(column, 0, Width - 1),
            Top + Math.Clamp(row, 0, Height - 1));
        Console.CursorVisible = true;
    }

    public void ShowSizeWarning() {
        ResetDisplay();
        var width = Console.WindowWidth;
        var height = Console.WindowHeight;
        if (width <= 0 || height <= 0) return;

        Console.ForegroundColor = ConsoleColor.Yellow;
        WriteClipped(0, $"Недостатній розмір: {width}x{height}. Мінімум: {MinimumWidth}x{MinimumHeight}.");
        if (height > 1) WriteClipped(1, "Збільште термінал або натисніть Esc для виходу.");
        Console.ResetColor();
    }

    private static void WriteClipped(int row, string message) {
        var limit = Math.Max(0, Console.WindowWidth - 1);
        if (limit == 0) return;
        Console.SetCursorPosition(0, row);
        Console.Write(message[..Math.Min(message.Length, limit)]);
    }

    public void Invalidate() => _invalidated = true;

    public void Dispose() {
        if (_disposed) return;
        _disposed = true;
        Console.ResetColor();
        Console.Write("\u001b[?1049l"); // Повертаємо PowerShell з його попередньою історією.
        Console.CursorVisible = true;
    }
}
