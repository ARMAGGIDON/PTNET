namespace lab08.Tui;


internal sealed class TerminalCanvas : IDisposable {
    public const int Width = 96;
    private const int Height = 27;
    private const int MinimumWidth = 100;
    private const int MinimumHeight = 30;

    private const int Left = 2;
    private int? _top;

    public bool IsReserved => _top.HasValue;

    public void Reserve() {
        if (_top.HasValue || !Fits) return;
        Console.ResetColor();
        Console.CursorVisible = false;

        if (Console.CursorLeft != 0) Console.WriteLine();
        for (var i = 0; i <= Height; i++) Console.WriteLine();

        _top = Math.Max(0, Console.CursorTop - Height - 1);
        Invalidate();
    }

    private readonly Cell[,] _current = new Cell[Height, Width];
    private readonly Cell[,] _previous = new Cell[Height, Width];
    private bool _invalidated = true;

    private readonly record struct Cell(char Character, ConsoleColor Foreground, ConsoleColor Background);

    public static bool Fits => Console.WindowWidth >= MinimumWidth && Console.WindowHeight >= MinimumHeight;

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

            Console.SetCursorPosition(Left, _top!.Value + row);
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
        if (_top is null) return;
        Console.SetCursorPosition(Left + Math.Clamp(column, 0, Width - 1), _top.Value + Math.Clamp(row, 0, Height - 1));
        Console.CursorVisible = true;
    }

    public void ShowSizeWarning() {
        Console.CursorVisible = false;
        Console.ResetColor();
        var width = Console.WindowWidth;
        var height = Console.WindowHeight;
        if (width <= 0 || height <= 0) return;

        var message = $"ПОМИЛКА: мінімальний розмір {MinimumWidth}x{MinimumHeight}; " +
                      $"зараз {width}x{height}. Збільште вікно або натисніть Esc.";
        Console.ForegroundColor = ConsoleColor.Yellow;

        if (_top is { } top) {
            var visibleRow = Math.Clamp(top + 1, Console.WindowTop, Console.WindowTop + Console.WindowHeight - 1);
            Console.SetCursorPosition(0, visibleRow);
            Console.Write(message[..Math.Min(message.Length, Math.Max(0, width - 1))].PadRight(Math.Max(0, width - 1)));
        } else {
            Console.WriteLine(message);
        }

        Console.ResetColor();
        Invalidate();
    }

    public void Invalidate() => _invalidated = true;

    public void Dispose() {
        Console.ResetColor();
        Console.CursorVisible = true;
        if (_top is not { } top) return;

        try {
            Console.SetCursorPosition(0, Math.Min(Console.BufferHeight - 1, top + Height));
            Console.WriteLine();
        } catch (ArgumentOutOfRangeException) {
            Console.WriteLine();
        }
    }
}
