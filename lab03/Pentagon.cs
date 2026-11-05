namespace Lab03;

public class Pentagon {
    public static int Count { get; set; }

    private int Number { get; }
    private Point Center { get; }
    private int Radius { get; }
    private Color Color { get; }

    public Pentagon(Point center, int radius, Color color) {
        Center = center;
        Radius = radius;
        Color = color;

        Number = ++Count;
    }

    public void Draw(Graphics graphics) {
        var points = new Point[5];

        for (var i = 0; i < points.Length; i++) {
            var angle = -Math.PI / 2 + i * 2 * Math.PI / 5;

            points[i] = new Point(
                Center.X + (int)(Radius * Math.Cos(angle)),
                Center.Y + (int)(Radius * Math.Sin(angle))
            );
        }

        using var pen = new Pen(Color, 2);

        graphics.DrawPolygon(pen, points);
        graphics.DrawString(
            Number.ToString(),
            SystemFonts.DefaultFont,
            Brushes.Black,
            Center.X - 5,
            Center.Y - 7
        );
    }
}