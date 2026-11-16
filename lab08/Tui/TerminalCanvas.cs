namespace lab08.Tui;

// Фіксована область 96x27. Мінімум вікна 100x30, щоб не торкатися правої/нижньої межі.
internal sealed class TerminalCanvas : IDisposable {
    public const int Width = 96;
    public const int Height = 27;
    public const int MinimumWidth = 100;
    public const int MinimumHeight = 30;

    private const int Left = 2;
    private const int Top = 1;

    private readonly Cell[,] _current = new Cell[Height, Width];
    private readonly Cell[,] _previous = new Cell[Height, Width];
    private bool _invalidated = true;

    private readonly record struct Cell(char Character, ConsoleColor Foreground, ConsoleColor Background);

    public bool Fits => Console.WindowWidth >= MinimumWidth && Console.WindowHeight >= MinimumHeight;

    public void BeginFrame() {
        for (int y = 0; y < Height; y++) {
            for (int x = 0; x < Width; x++) _current[y, x] = new Cell(' ', ConsoleColor.Gray, ConsoleColor.Black);
        }
    }

    public void Put(int row, int column, string text, ConsoleColor fg = ConsoleColor.Gray,
        ConsoleColor bg = ConsoleColor.Black) {
        if (row < 0 || row >= Height || string.IsNullOrEmpty(text)) return;
        for (int i = 0; i < text.Length; i++) {
            int x = column + i;
            if (x >= 0 && x < Width) _current[row, x] = new Cell(text[i], fg, bg);
        }
    }

    public void Fill(int row, int column, int count, ConsoleColor fg, ConsoleColor bg) {
        if (row < 0 || row >= Height) return;
        for (int x = Math.Max(0, column); x < Math.Min(Width, column + count); x++) {
            _current[row, x] = new Cell(_current[row, x].Character, fg, bg);
        }
    }

    public void Line(int row) => Put(row, 0, new string('─', Width), ConsoleColor.DarkGray);

    public void Render() {
        Console.CursorVisible = false;

        for (int row = 0; row < Height; row++) {
            bool changed = _invalidated;
            for (int x = 0; !changed && x < Width; x++) changed = !_current[row, x].Equals(_previous[row, x]);
            if (!changed) continue;

            Console.SetCursorPosition(Left, Top + row);
            int offset = 0;
            while (offset < Width) {
                Cell style = _current[row, offset];
                int end = offset + 1;
                while (end < Width && _current[row, end].Foreground == style.Foreground &&
                       _current[row, end].Background == style.Background) end++;

                Console.ForegroundColor = style.Foreground;
                Console.BackgroundColor = style.Background;
                char[] chars = new char[end - offset];
                for (int i = offset; i < end; i++) chars[i - offset] = _current[row, i].Character;
                Console.Write(chars);
                offset = end;
            }

            for (int x = 0; x < Width; x++) _previous[row, x] = _current[row, x];
        }

        Console.ResetColor();
        _invalidated = false;
    }

    public void ShowCaret(int row, int column) {
        if (!Fits) return;
        Console.SetCursorPosition(Left + Math.Clamp(column, 0, Width - 1), Top + Math.Clamp(row, 0, Height - 1));
        Console.CursorVisible = true;
    }

    public void ShowSizeWarning() {
        Console.CursorVisible = false;
        Console.ResetColor();
        int width = Console.WindowWidth;
        int height = Console.WindowHeight;
        if (width <= 0 || height <= 0) return;

        string message = $"ПОМИЛКА: мінімальний розмір консолі {MinimumWidth}x{MinimumHeight}. " +
                         $"Зараз {width}x{height}. Збільште вікно.";
        Console.SetCursorPosition(0, 0);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(message[..Math.Min(message.Length, Math.Max(0, width - 1))]
            .PadRight(Math.Max(0, width - 1)));
        Console.ResetColor();
        Invalidate();
    }

    public void Invalidate() => _invalidated = true;

    public void Dispose() {
        Console.ResetColor();
        Console.CursorVisible = true;
        if (Console.WindowHeight > 0) {
            Console.SetCursorPosition(0, Math.Min(Console.WindowHeight - 1, Top + Height));
            Console.WriteLine();
        }
    }
}
