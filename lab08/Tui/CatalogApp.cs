using System.Globalization;
using Microsoft.Data.SqlClient;
using lab08.Data;

namespace lab08.Tui;


internal sealed class CatalogApp {
    private const int PageSize = 14;
    private const int FieldWidth = 53;

    private static readonly string[] Labels = ["Код", "Тема", "Назва", "Рік видання", "Вартість"];

    private readonly DiscRepository _repository;
    private readonly TerminalCanvas _canvas;
    private List<Disc> _discs = [];

    private enum Screen { Catalog, Editor, ConfirmDelete }

    private Screen _screen = Screen.Catalog;
    private int _selected;
    private int _first;
    private int _focus;
    private int? _originalCode;
    private int? _deleteCode;
    private string[] _fields = ["", "", "", "", ""];
    private bool _editing;
    private string _editBackup = "";
    private int _caret;
    private bool _quit;
    private bool _needsPaint = true;
    private string _status = "Готово до роботи.";
    private bool _statusIsError;

    public CatalogApp(DiscRepository repository, TerminalCanvas canvas) {
        _repository = repository;
        _canvas = canvas;
        Refresh();
    }

    public void Run() {
        var tooSmall = false;
        int lastWidth = -1, lastHeight = -1;

        while (!_quit) {
            int width = Console.WindowWidth, height = Console.WindowHeight;
            bool sizeChanged = width != lastWidth || height != lastHeight;
            lastWidth = width;
            lastHeight = height;

            if (!TerminalCanvas.Fits) {
                if (!tooSmall || sizeChanged) _canvas.ShowSizeWarning();
                tooSmall = true;
                if (Console.KeyAvailable && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape) _quit = true;
                Thread.Sleep(80);
                continue;
            }

            if (tooSmall || sizeChanged) {
                _canvas.ResetDisplay();
                _needsPaint = true;
                tooSmall = false;
            }

            if (_needsPaint) {
                try {
                    Draw();
                    _needsPaint = false;
                } catch (ArgumentOutOfRangeException) {
                    // Консоль могла змінити розмір під час побудови кадру.
                    _canvas.Invalidate();
                }
            }

            if (Console.KeyAvailable) {
                var key = Console.ReadKey(intercept: true);
                try {
                    HandleKey(key);
                } catch (SqlException ex) {
                    Error(ex.Number is 2601 or 2627
                        ? "Запис із таким кодом уже існує."
                        : $"SQL Server: {ex.Message}");
                } catch (Exception ex) when (ex is InvalidOperationException or ArgumentException) {
                    Error(ex.Message);
                }
                _needsPaint = true;
            } else {
                Thread.Sleep(50);
            }
        }
    }

    private void HandleKey(ConsoleKeyInfo key) {
        switch (_screen) {
            case Screen.Catalog: HandleCatalogKey(key); break;
            case Screen.Editor: HandleEditorKey(key); break;
            case Screen.ConfirmDelete: HandleDeleteKey(key); break;
        }
    }

    private void HandleCatalogKey(ConsoleKeyInfo key) {
        switch (key.Key) {
            case ConsoleKey.UpArrow: Move(-1); break;
            case ConsoleKey.DownArrow: Move(1); break;
            case ConsoleKey.PageUp: Move(-PageSize); break;
            case ConsoleKey.PageDown: Move(PageSize); break;
            case ConsoleKey.Home: Select(0); break;
            case ConsoleKey.End: Select(_discs.Count - 1); break;
            case ConsoleKey.A: OpenEditor(null); break;
            case ConsoleKey.E:
            case ConsoleKey.Enter:
                if (Selected is { } disc) OpenEditor(disc);
                break;
            case ConsoleKey.Delete:
            case ConsoleKey.D:
                if (Selected is { } current) {
                    _deleteCode = current.Code;
                    Switch(Screen.ConfirmDelete);
                }
                break;
            case ConsoleKey.R:
                Refresh();
                Message("Дані перечитано із SQL Server.");
                break;
            case ConsoleKey.Escape: _quit = true; break;
        }
    }

    private void HandleEditorKey(ConsoleKeyInfo key) {
        if ((key.Modifiers & ConsoleModifiers.Control) != 0 && key.Key == ConsoleKey.S) {
            if (_editing) _editing = false;
            Save();
            return;
        }

        if (_editing) {
            switch (key.Key) {
                case ConsoleKey.Escape:
                    _fields[_focus] = _editBackup;
                    _editing = false;
                    return;
                case ConsoleKey.Enter:
                    _editing = false;
                    _focus = (_focus + 1) % Labels.Length;
                    return;
                case ConsoleKey.Tab:
                    _editing = false;
                    FocusNext((key.Modifiers & ConsoleModifiers.Shift) != 0 ? -1 : 1);
                    return;
                case ConsoleKey.LeftArrow:
                    _caret = Math.Max(0, _caret - 1);
                    return;
                case ConsoleKey.RightArrow:
                    _caret = Math.Min(_fields[_focus].Length, _caret + 1);
                    return;
                case ConsoleKey.Home:
                    _caret = 0;
                    return;
                case ConsoleKey.End:
                    _caret = _fields[_focus].Length;
                    return;
                case ConsoleKey.Backspace:
                    if (_caret <= 0) return;
                    _fields[_focus] = _fields[_focus].Remove(_caret - 1, 1);
                    _caret--;
                    return;
                case ConsoleKey.Delete:
                    if (_caret < _fields[_focus].Length)
                        _fields[_focus] = _fields[_focus].Remove(_caret, 1);
                    return;
            }

            if ((key.Modifiers & ConsoleModifiers.Control) != 0 && key.Key == ConsoleKey.U) {
                _fields[_focus] = "";
                _caret = 0;
                return;
            }

            if (char.IsControl(key.KeyChar) || !Permitted(_focus, key.KeyChar)) return;
            var limit = _focus switch { 1 => 100, 2 => 150, _ => 32 };
            if (_fields[_focus].Length >= limit) return;

            _fields[_focus] = _fields[_focus].Insert(_caret, key.KeyChar.ToString());
            _caret++;
            return;
        }

        switch (key.Key) {
            case ConsoleKey.UpArrow: FocusNext(-1); break;
            case ConsoleKey.DownArrow:
            case ConsoleKey.Tab:
                FocusNext(key.Key == ConsoleKey.Tab && (key.Modifiers & ConsoleModifiers.Shift) != 0 ? -1 : 1);
                break;
            case ConsoleKey.Enter:
            case ConsoleKey.F2:
                _editing = true;
                _editBackup = _fields[_focus];
                _caret = _fields[_focus].Length;
                break;
            case ConsoleKey.Escape: Switch(Screen.Catalog); break;
        }
    }

    private void HandleDeleteKey(ConsoleKeyInfo key) {
        switch (key.Key) {
            case ConsoleKey.Y:
            case ConsoleKey.Enter:
                if (_deleteCode is { } code) {
                    int affected = _repository.Delete(code);
                    Refresh();
                    Message(affected == 0 ? "Запис уже відсутній у таблиці." : $"Диск #{code} видалено.");
                }
                _deleteCode = null;
                Switch(Screen.Catalog);
                break;
            case ConsoleKey.N:
            case ConsoleKey.Escape:
                _deleteCode = null;
                Switch(Screen.Catalog);
                break;
        }
    }

    private void OpenEditor(Disc? disc) {
        _originalCode = disc?.Code;
        _fields = disc is null
            ? ["", "", "", "", ""]
            : [disc.Code.ToString(), disc.Topic, disc.Title, disc.ReleaseYear.ToString(),
                disc.Price.ToString("0.00", CultureInfo.InvariantCulture)];
        _focus = 0;
        _caret = 0;
        _editing = false;
        Message(disc is null ? "Новий запис." : $"Редагування диска #{disc.Code}.");
        Switch(Screen.Editor);
    }

    private void Save() {
        if (!TryReadDisc(out Disc? disc, out string validationError)) {
            Error(validationError);
            return;
        }

        var affected = _originalCode is { } oldCode
            ? _repository.Update(oldCode, disc!)
            : _repository.Insert(disc!);

        Refresh(disc!.Code);
        Message(affected == 0 ? "Запис не знайдено: оновлення не виконано." : "Запис збережено у SQL Server.");
        Switch(Screen.Catalog);
    }

    private bool TryReadDisc(out Disc? disc, out string error) {
        disc = null;
        error = "";
        if (!int.TryParse(_fields[0].Trim(), out int code) || code <= 0) {
            error = "Код має бути додатним цілим числом.";
        } else if (string.IsNullOrWhiteSpace(_fields[1]) || _fields[1].Length > 100) {
            error = "Тема обов'язкова (до 100 символів).";
        } else if (string.IsNullOrWhiteSpace(_fields[2]) || _fields[2].Length > 150) {
            error = "Назва обов'язкова (до 150 символів).";
        } else if (!int.TryParse(_fields[3].Trim(), out int year) || year is < 1900 or > 2100) {
            error = "Рік видання має бути в межах 1900–2100.";
        } else if (!decimal.TryParse(_fields[4].Trim().Replace(',', '.'),
                       NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var price) ||
                   price < 0 || price > 99999999.99m || decimal.Round(price, 2) != price) {
            error = "Вартість: невід'ємне число з максимум 2 знаками після крапки.";
        } else {
            disc = new Disc(code, _fields[1].Trim(), _fields[2].Trim(), year, price);
            return true;
        }
        return false;
    }

    private static bool Permitted(int field, char c) => field switch {
        0 or 3 => char.IsAsciiDigit(c),
        4 => char.IsAsciiDigit(c) || c is '.' or ',',
        _ => !char.IsControl(c)
    };

    private void Move(int delta) => Select(_selected + delta);

    private void Select(int index) {
        _selected = _discs.Count == 0 ? 0 : Math.Clamp(index, 0, _discs.Count - 1);
        KeepSelectionVisible();
    }

    private void KeepSelectionVisible() {
        if (_selected < _first) _first = _selected;
        if (_selected >= _first + PageSize) _first = _selected - PageSize + 1;
        _first = Math.Clamp(_first, 0, Math.Max(0, _discs.Count - PageSize));
    }

    private Disc? Selected => _selected >= 0 && _selected < _discs.Count ? _discs[_selected] : null;

    private void Refresh(int? selectCode = null) {
        _discs = _repository.GetAll();
        if (selectCode is { } code) {
            var index = _discs.FindIndex(disc => disc.Code == code);
            if (index >= 0) _selected = index;
        }
        Select(_selected);
    }

    private void FocusNext(int direction) => _focus = (_focus + direction + Labels.Length) % Labels.Length;

    private void Switch(Screen next) {
        _screen = next;
        _canvas.Invalidate(); // Зміна форми: переписуємо всю робочу область.
        _needsPaint = true;
    }

    private void Message(string message) {
        _status = message.Replace('\r', ' ').Replace('\n', ' ');
        _statusIsError = false;
    }

    private void Error(string message) {
        _status = message.Replace('\r', ' ').Replace('\n', ' ');
        _statusIsError = true;
    }

    private void Draw() {
        _canvas.BeginFrame();

        var title = _screen switch {
            Screen.Catalog => "КАТАЛОГ ДИСКІВ",
            Screen.Editor => _originalCode is null ? "ДОДАТИ ДИСК" : "РЕДАГУВАТИ ДИСК",
            _ => "ПІДТВЕРДЖЕННЯ ВИДАЛЕННЯ"
        };
        _canvas.Put(0, 0, "┌" + new string('─', TerminalCanvas.Width - 2) + "┐", ConsoleColor.DarkGray);
        _canvas.Put(1, 2, title + "   |   Лабораторна №8 / ADO.NET", ConsoleColor.Cyan);
        _canvas.Put(2, 2, Short($"SQL: {_repository.Server} / {_repository.Database}", 91),
            ConsoleColor.DarkGray);
        _canvas.Line(3);

        switch (_screen) {
            case Screen.Catalog: DrawCatalog(); break;
            case Screen.Editor: DrawEditor(); break;
            case Screen.ConfirmDelete: DrawConfirmation(); break;
        }

        _canvas.Line(25);
        _canvas.Put(26, 0, "└" + new string('─', TerminalCanvas.Width - 2) + "┘", ConsoleColor.DarkGray);
        _canvas.Put(24, 2, Short(_status, 91),
            _statusIsError ? ConsoleColor.Yellow : ConsoleColor.Green);
        _canvas.Render();

        if (_screen != Screen.Editor || !_editing) return;
        var firstVisible = Math.Max(0, _caret - FieldWidth + 1);
        _canvas.ShowCaret(7 + _focus * 3, 24 + _caret - firstVisible);
    }

    private void DrawCatalog() {
        _canvas.Put(4, 2, "Код   Тема                Назва                    Рік  Вартість", ConsoleColor.White);
        _canvas.Put(4, 68, "ПОЛЯ ВИБРАНОГО ДИСКА", ConsoleColor.White);
        for (var y = 4; y <= 20; y++) _canvas.Put(y, 66, "│", ConsoleColor.DarkGray);
        _canvas.Line(20);

        for (var i = 0; i < PageSize; i++) {
            var index = _first + i;
            var y = 6 + i;
            if (index >= _discs.Count) break;

            Disc disc = _discs[index];
            var active = index == _selected;
            if (active) _canvas.Fill(y, 1, 64, ConsoleColor.Black, ConsoleColor.Gray);
            var value = $"{disc.Code,5}  {Short(disc.Topic,18),-18} " +
                              $"{Short(disc.Title,22),-22} {disc.ReleaseYear,4} {disc.Price,9:0.00}";
            _canvas.Put(y, 2, Short(value, 63),
                active ? ConsoleColor.Black : ConsoleColor.Gray,
                active ? ConsoleColor.Gray : ConsoleColor.Black);
        }

        if (_discs.Count == 0) _canvas.Put(8, 4, "Таблиця порожня. Натисніть A, щоб додати диск.", ConsoleColor.Yellow);
        if (Selected is { } selected) {
            _canvas.Put(6, 68, "Код:");
            _canvas.Put(7, 68, $"[{selected.Code}]", ConsoleColor.White);
            _canvas.Put(9, 68, "Тема:");
            _canvas.Put(10, 68, "[" + Short(selected.Topic, 25) + "]", ConsoleColor.White);
            _canvas.Put(12, 68, "Назва:");
            _canvas.Put(13, 68, "[" + Short(selected.Title, 25) + "]", ConsoleColor.White);
            _canvas.Put(15, 68, "Рік видання:");
            _canvas.Put(16, 68, $"[{selected.ReleaseYear}]", ConsoleColor.White);
            _canvas.Put(18, 68, "Вартість:");
            _canvas.Put(19, 68, $"[{selected.Price:0.00} грн]", ConsoleColor.White);
        }

        _canvas.Put(21, 2, $"Записів: {_discs.Count}. " +
            (_discs.Count == 0 ? "Немає вибраного запису." : $"Вибрано: {_selected + 1} із {_discs.Count}."));
        _canvas.Put(22, 2, "↑ ↓ / PgUp PgDn / Home End — вибір    Enter/E — редагувати    A — додати");
        _canvas.Put(23, 2, "Del/D — видалити    R — оновити з БД    Esc — вихід");
    }

    private void DrawEditor() {
        _canvas.Put(4, 2, "Переміщення: ↑ ↓ / Tab. Enter або F2 — вводити текст.");
        _canvas.Put(5, 2, "У режимі вводу: ← →, Home, End, Backspace, Delete; Enter — прийняти поле.");
        for (var i = 0; i < Labels.Length; i++) {
            var row = 7 + i * 3;
            bool active = i == _focus;
            _canvas.Put(row, 3, $"{Labels[i],-15}:", active ? ConsoleColor.Cyan : ConsoleColor.Gray);
            _canvas.Put(row, 22, "[", ConsoleColor.DarkGray);
            _canvas.Put(row, 79, "]", ConsoleColor.DarkGray);
            _canvas.Fill(row, 24, FieldWidth,
                active ? ConsoleColor.Black : ConsoleColor.White,
                active ? ConsoleColor.Gray : ConsoleColor.DarkGray);

            var start = active && _editing ? Math.Max(0, _caret - FieldWidth + 1) : 0;
            var visible = _fields[i].Length > start ? _fields[i][start..] : "";
            _canvas.Put(row, 24, Short(visible, FieldWidth),
                active ? ConsoleColor.Black : ConsoleColor.White,
                active ? ConsoleColor.Gray : ConsoleColor.DarkGray);
        }

        _canvas.Put(22, 2, "Ctrl+S — зберегти до БД    Esc — скасувати ввід / повернутися до каталогу");
        _canvas.Put(23, 2, "Ctrl+U — очистити активне поле під час вводу    Ціна: 123.45 або 123,45");
    }

    private void DrawConfirmation() {
        _canvas.Put(8, 5, $"Дійсно видалити диск із кодом {_deleteCode}?", ConsoleColor.Yellow);
        _canvas.Put(10, 5, "Операція видалить запис із таблиці SQL Server.");
        _canvas.Put(13, 5, "Y або Enter — так         N або Esc — ні", ConsoleColor.White);
    }

    private static string Short(string text, int max) => text.Length <= max ? text : text[..Math.Max(0, max - 1)] + "…";
}
