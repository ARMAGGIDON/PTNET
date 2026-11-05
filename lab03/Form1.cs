using System.Drawing.Drawing2D;

namespace Lab03;

public sealed class Form1 : Form {
    private const int MaxPentagons = 20;

    private readonly List<Pentagon> _pentagons = [];

    private readonly Button _createButton;
    private readonly Label _countLabel;

    private static readonly Color[] Colors = [
        Color.Red,
        Color.Blue,
        Color.Green,
        Color.DarkOrange,
        Color.Purple,
        Color.DeepPink
    ];

    public Form1() {
        Text = "Лабораторна робота 3 — Правильний п'ятикутник";
        ClientSize = new Size(900, 650);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(700, 500);

        DoubleBuffered = true;

        _createButton = new Button {
            Text = "Створити п'ятикутник",
            Location = new Point(20, 20),
            Size = new Size(180, 35)
        };

        _countLabel = new Label {
            Text = "Кількість об'єктів: 0",
            Location = new Point(220, 28),
            AutoSize = true
        };

        _createButton.Click += CreateButton_Click;

        Controls.Add(_createButton);
        Controls.Add(_countLabel);
    }

    private void CreateButton_Click(object? sender, EventArgs e) {
        if (Pentagon.Count >= MaxPentagons) {
            _pentagons.Clear();
            Pentagon.Count = 0;
        }

        var number = Pentagon.Count + 1;

        var column = (number - 1) % 5;
        var row = (number - 1) / 5;

        var center = new Point(
            90 + column * 170,
            140 + row * 140
        );

        var radius = 30 + number * 2;
        var color = Colors[(number - 1) % Colors.Length];

        _pentagons.Add(new Pentagon(center, radius, color));

        _countLabel.Text = $"Кількість об'єктів: {Pentagon.Count}";

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        foreach (var pentagon in _pentagons)
            pentagon.Draw(e.Graphics);
    }
}