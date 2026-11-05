using System.Reflection;
using System.Text;

namespace lab05;

internal sealed class MainForm : Form {
    private readonly NumericUpDown _nudTopBase;
    private readonly NumericUpDown _nudBottomBase;
    private readonly NumericUpDown _nudHeight;

    private readonly Panel _drawPanel;
    private readonly TextBox _txtReflection;
    private readonly Label _lblStatus;

    private readonly SplitContainer _splitLeftRight;
    private readonly SplitContainer _splitTopBottom;

    private Trapezoid _trapezoid = new();

    private readonly string _xmlPath = Path.Combine(AppContext.BaseDirectory, "trapezoid.xml");
    private readonly string _binPath = Path.Combine(AppContext.BaseDirectory, "appstate.bin");
    private bool _isUpdatingControls;

    public MainForm() {
        Text = "Лабораторна 17 - Серіалізація трапеції";
        Font = new Font("Times New Roman", 14);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 700);
        Width = 1200;
        Height = 750;

        _nudTopBase = CreateNumeric();
        _nudTopBase.ValueChanged += OnParametersChanged;

        _nudBottomBase = CreateNumeric();
        _nudBottomBase.ValueChanged += OnParametersChanged;

        _nudHeight = CreateNumeric();
        _nudHeight.ValueChanged += OnParametersChanged;

        var btnSaveXml = CreateButton("Зберегти XML", OnSaveXmlClick);
        var btnLoadXml = CreateButton("Завантажити XML", OnLoadXmlClick);
        var btnSaveBin = CreateButton("Зберегти BIN", OnSaveBinClick);
        var btnLoadBin = CreateButton("Завантажити BIN", OnLoadBinClick);
        var btnReflect = CreateButton("Рефлексія", OnReflectClick);

        _lblStatus = new Label {
            Dock = DockStyle.Top, Padding = new Padding(6),
            Height = 48, AutoSize = false, AutoEllipsis = true,
            BorderStyle = BorderStyle.FixedSingle, TextAlign = ContentAlignment.MiddleLeft,
            Text = "Стан: готово"
        };

        _drawPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        _drawPanel.Paint += OnDrawPanelPaint;
        _drawPanel.Resize += (_, _) => _drawPanel.Invalidate();

        _txtReflection = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Both, ReadOnly = true, WordWrap = false };


        _splitLeftRight = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, BorderStyle = BorderStyle.FixedSingle, };
        _splitLeftRight.SplitterMoved += (_, _) => _drawPanel.Invalidate();
        
        _splitTopBottom = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, BorderStyle = BorderStyle.FixedSingle };
        _splitTopBottom.SplitterMoved += (_, _) => _drawPanel.Invalidate();

        Panel leftPanel = new() { Dock = DockStyle.Fill, Padding = new Padding(10) };
        TableLayoutPanel leftGrid = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        leftGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        GroupBox gbInput = new() { Text = "Параметри трапеції", Dock = DockStyle.Top, Padding = new Padding(10), AutoSize = true };

        TableLayoutPanel inputGrid = new() { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, RowCount = 3, };
        inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddInputRow(inputGrid, 0, "Верхня основа:", _nudTopBase);
        AddInputRow(inputGrid, 1, "Нижня основа:", _nudBottomBase);
        AddInputRow(inputGrid, 2, "Висота:", _nudHeight);

        TableLayoutPanel btnGrid = new() { Dock = DockStyle.Top, AutoSize = true, Margin = new Padding(0, 10, 0, 10), ColumnCount = 2, RowCount = 3, };
        btnGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        btnGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        btnGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        btnGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        btnGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        btnGrid.Controls.Add(btnSaveXml, 0, 0);
        btnGrid.Controls.Add(btnLoadXml, 0, 1);
        btnGrid.Controls.Add(btnSaveBin, 1, 0);
        btnGrid.Controls.Add(btnLoadBin, 1, 1);
        btnGrid.SetColumnSpan(btnReflect, 2);

        gbInput.Controls.Add(inputGrid);
        leftGrid.Controls.Add(gbInput, 0, 0);
        leftGrid.Controls.Add(btnGrid, 0, 1);
        leftGrid.Controls.Add(_lblStatus, 0, 2);
        leftPanel.Controls.Add(leftGrid);

        _splitTopBottom.Panel1.Controls.Add(_drawPanel);
        _splitTopBottom.Panel2.Controls.Add(_txtReflection);

        _splitLeftRight.Panel1.Controls.Add(leftPanel);
        _splitLeftRight.Panel2.Controls.Add(_splitTopBottom);
        Controls.Add(_splitLeftRight);

        RestoreOnStart();
        ShowReflection();
        Shown += MainForm_Shown;
    }

    private static void SetSafeSplitter(SplitContainer split, int desiredDistance) {
        var total = split.Orientation == Orientation.Vertical
            ? split.ClientSize.Width
            : split.ClientSize.Height;

        var min = split.Panel1MinSize;
        var max = total - split.Panel2MinSize - split.SplitterWidth;

        if (max < min) {
            max = min;
        }

        split.SplitterDistance = Math.Clamp(desiredDistance, min, max);
    }
    
    private void InitSplitBounds() {
        var total = _splitLeftRight.ClientSize.Width;
        int safePanel2Min;
        if (total > 0) {
            safePanel2Min = Math.Min(600, Math.Max(0, total - 440 - _splitLeftRight.SplitterWidth));
            _splitLeftRight.Panel1MinSize = Math.Min(440, Math.Max(0, total - safePanel2Min - _splitLeftRight.SplitterWidth));
            _splitLeftRight.Panel2MinSize = safePanel2Min;
        }

        total = _splitTopBottom.ClientSize.Height;
        if (total <= 0) return;
        
        safePanel2Min = Math.Min(150, Math.Max(0, total - 220 - _splitTopBottom.SplitterWidth));
        _splitTopBottom.Panel1MinSize = Math.Min(220, Math.Max(0, total - safePanel2Min - _splitTopBottom.SplitterWidth));
        _splitTopBottom.Panel2MinSize = safePanel2Min;
    }
    
    private void MainForm_Shown(object? sender, EventArgs e) {
        Shown -= MainForm_Shown;
        InitSplitBounds();

        if (File.Exists(_binPath)) {
            try {
                AppState appState = AppStateBinaryStorage.Load(_binPath);
                ApplyAppState(appState);
            } catch {
                SetSafeSplitter(_splitLeftRight, 320);
                SetSafeSplitter(_splitTopBottom, 420);
            }
        } else {
            SetSafeSplitter(_splitLeftRight, 320);
            SetSafeSplitter(_splitTopBottom, 420);
        }

        _drawPanel.Invalidate();
    }

    private static NumericUpDown CreateNumeric() { return new NumericUpDown { DecimalPlaces = 1, Minimum = 0, Maximum = 100000, Increment = 1, Dock = DockStyle.Fill }; }

    private static Button CreateButton(string text, EventHandler clickHandler) {
        Button button = new() {
            Text = text, AutoSize = false, Height = 50,
            Padding = new Padding(8), Anchor = AnchorStyles.Left | AnchorStyles.Right,
        };
        button.Click += clickHandler;
        return button;
    }

    private static void AddInputRow(TableLayoutPanel grid, int rowIndex, string labelText, Control control) {
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Label label = new() {
            Text = labelText, AutoSize = true,
            Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 10, 6)
        };
        grid.Controls.Add(label, 0, rowIndex);
        grid.Controls.Add(control, 1, rowIndex);
    }

    private void OnParametersChanged(object? sender, EventArgs e) { if (!_isUpdatingControls) UpdateModelFromControls(); }

    private void UpdateModelFromControls() {
        _isUpdatingControls = true;
        try {
            _trapezoid.TopBase = (double)_nudTopBase.Value;
            _trapezoid.BottomBase = (double)_nudBottomBase.Value;
            _trapezoid.Height = (double)_nudHeight.Value;
        } finally {
            _isUpdatingControls = false;
        }

        _lblStatus.Text = $"Стан: об'єкт відновлено | Площа = {_trapezoid.GetArea():0.##}";
        _drawPanel.Invalidate();
    }

    private void UpdateControlsFromModel() {
        _isUpdatingControls = true;
        try {
            _nudTopBase.Value = ToDecimalSafe(_trapezoid.TopBase);
            _nudBottomBase.Value = ToDecimalSafe(_trapezoid.BottomBase);
            _nudHeight.Value = ToDecimalSafe(_trapezoid.Height);
        } finally {
            _isUpdatingControls = false;
        }

        _lblStatus.Text = $"Стан: об'єкт відновлено | Площа = {_trapezoid.GetArea():0.##}";
        _drawPanel.Invalidate();
    }

    private static decimal ToDecimalSafe(double value) { return Convert.ToDecimal(Math.Clamp(value, 0, 100000)); }

    private void OnDrawPanelPaint(object? sender, PaintEventArgs e) { _trapezoid.Draw(e.Graphics, _drawPanel.ClientRectangle); }

    private void OnSaveXmlClick(object? sender, EventArgs e) {
        try {
            UpdateModelFromControls();
            TrapezoidXmlStorage.Save(_xmlPath, _trapezoid);
            _lblStatus.Text = $"Стан: XML фігури збережено -> a={_trapezoid.TopBase:0.##}; b={_trapezoid.BottomBase:0.##}; h={_trapezoid.Height:0.##}";
        } catch (Exception ex) {
            ShowError("Помилка збереження XML", ex);
        }
    }

    private void OnLoadXmlClick(object? sender, EventArgs e) {
        try {
            if (!File.Exists(_xmlPath)) {
                MessageBox.Show("XML-файл фігури ще не створено.", "Інформація", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _trapezoid = TrapezoidXmlStorage.Load(_xmlPath);
            UpdateControlsFromModel();

            _lblStatus.Text = $"Стан: XML фігури завантажено -> a={_trapezoid.TopBase:0.##}; b={_trapezoid.BottomBase:0.##}; h={_trapezoid.Height:0.##}";
        } catch (Exception ex) {
            ShowError("Помилка завантаження XML", ex);
        }
    }

    private AppState BuildAppState() {
        var bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        var mainRatio = 0.5;
        var mainTotal = _splitLeftRight.ClientSize.Width - _splitLeftRight.SplitterWidth;
        if (mainTotal > 0) {
            mainRatio = (double)_splitLeftRight.SplitterDistance / mainTotal;
        }

        var rightRatio = 0.5;
        var rightTotal = _splitTopBottom.ClientSize.Height - _splitTopBottom.SplitterWidth;
        if (rightTotal > 0) {
            rightRatio = (double)_splitTopBottom.SplitterDistance / rightTotal;
        }

        return new AppState {
            FormLeft = bounds.Left,
            FormTop = bounds.Top,
            FormWidth = bounds.Width,
            FormHeight = bounds.Height,
            WindowState = WindowState,
            MainSplitRatio = mainRatio,
            RightSplitRatio = rightRatio
        };
    }
    private void ApplyAppState(AppState appState) {
        StartPosition = FormStartPosition.Manual;

        Bounds = new Rectangle(
            appState.FormLeft, appState.FormTop,
            Math.Max(appState.FormWidth, MinimumSize.Width),
            Math.Max(appState.FormHeight, MinimumSize.Height)
        );

        WindowState = appState.WindowState;

        BeginInvoke(() => {
            InitSplitBounds();

            var mainTotal = _splitLeftRight.ClientSize.Width - _splitLeftRight.SplitterWidth;
            var rightTotal = _splitTopBottom.ClientSize.Height - _splitTopBottom.SplitterWidth;

            var mainDistance = mainTotal > 0
                ? (int)Math.Round(appState.MainSplitRatio * mainTotal)
                : 320;

            var rightDistance = rightTotal > 0
                ? (int)Math.Round(appState.RightSplitRatio * rightTotal)
                : 420;

            SetSafeSplitter(_splitLeftRight, mainDistance);
            SetSafeSplitter(_splitTopBottom, rightDistance);

            _drawPanel.Invalidate();
        });
    }

    private void OnSaveBinClick(object? sender, EventArgs e) {
        try {
            var appState = BuildAppState();
            AppStateBinaryStorage.Save(_binPath, appState);
            _lblStatus.Text = $"Стан: BIN програми збережено -> splitV={appState.MainSplitRatio:0.##}; splitH={appState.RightSplitRatio:0.##}";
        } catch (Exception ex) {
            ShowError("Помилка збереження BIN", ex);
        }
    }

    private void OnLoadBinClick(object? sender, EventArgs e) {
        try {
            if (!File.Exists(_binPath)) {
                MessageBox.Show("BIN-файл стану програми ще не створено.", "Інформація", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var appState = AppStateBinaryStorage.Load(_binPath);
            ApplyAppState(appState);
            _lblStatus.Text = $"Стан: BIN програми завантажено -> splitV={appState.MainSplitRatio:0.##}; splitH={appState.RightSplitRatio:0.##}";
        } catch (Exception ex) {
            ShowError("Помилка завантаження BIN", ex);
        }
    }

    private void OnReflectClick(object? sender, EventArgs e) { ShowReflection(); }
    private void ShowReflection() {
        var type = typeof(Trapezoid);
        StringBuilder sb = new();

        sb.AppendLine("=== РЕФЛЕКСІЯ КЛАСУ ===");
        sb.AppendLine($"Повне ім'я класу: {type.FullName}");

        sb.AppendLine();
        sb.AppendLine("Конструктори:");
        foreach (var constructor in type.GetConstructors()) {
            sb.AppendLine($"- {type.Name}({string.Join(", ", constructor.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})");
        }

        sb.AppendLine();
        sb.AppendLine("Властивості:");
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)) {
            sb.AppendLine($"- {property.PropertyType.Name} {property.Name}");
        }

        sb.AppendLine();
        sb.AppendLine("Методи:");
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)) {
            sb.AppendLine($"- {method.ReturnType.Name} {method.Name}({string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})");
        }

        _txtReflection.Text = sb.ToString();
    }

    private void RestoreOnStart() {
        try {
            if (File.Exists(_xmlPath)) {
                _trapezoid = TrapezoidXmlStorage.Load(_xmlPath);
            } else {
                _trapezoid = new Trapezoid();
            }

            UpdateControlsFromModel();
            _lblStatus.Text = "Стан: при запуску відновлено фігуру з XML";
        } catch (Exception ex) {
            _trapezoid = new Trapezoid();
            UpdateControlsFromModel();
            _lblStatus.Text = $"Стан: помилка автовідновлення -> {ex.Message}";
        }
    }

    private static void ShowError(string title, Exception ex) { MessageBox.Show($"{ex.Message}", title, MessageBoxButtons.OK, MessageBoxIcon.Error); }

    protected override void OnFormClosing(FormClosingEventArgs e) {
        OnSaveXmlClick(this, EventArgs.Empty);
        try {
            AppStateBinaryStorage.Save(_binPath, BuildAppState());
        }
        catch { // ignored
        }

        base.OnFormClosing(e);
    }
}
