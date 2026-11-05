using System.Drawing.Drawing2D;
using System.Xml.Serialization;


namespace lab05;


internal static class Program {
    [STAThread]
    private static void Main() {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}


[Serializable]
public sealed class Trapezoid {
    private double _topBase;
    private double _bottomBase;
    private double _height;

    public double TopBase {
        get => _topBase;
        set => _topBase = value < 0 ? 0 : value;
    }

    public double BottomBase {
        get => _bottomBase;
        set => _bottomBase = value < 0 ? 0 : value;
    }

    public double Height {
        get => _height;
        set => _height = value < 0 ? 0 : value;
    }

    public Trapezoid() {
        TopBase = 0;
        BottomBase = 0;
        Height = 0;
    }

    public Trapezoid(double topBase, double bottomBase, double height) {
        TopBase = topBase;
        BottomBase = bottomBase;
        Height = height;
    }

    public double GetArea() { return (TopBase + BottomBase) * Height / 2.0; }

    public void Draw(Graphics graphics, Rectangle bounds) {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.White);

        using Font textFont = new("Times New Roman", 14);
        using Brush textBrush = new SolidBrush(Color.Black);

        if (!(TopBase > 0 && BottomBase > 0 && Height > 0)) {
            const string msg = "Введіть додатні значення основ і висоти";
            var textSize = graphics.MeasureString(msg, textFont);
            graphics.DrawString(msg, textFont, textBrush, (bounds.Width - textSize.Width) / 2f, (bounds.Height - textSize.Height) / 2f);
            return;
        }

        var drawArea = Rectangle.Inflate(bounds, -20, -20);
        var scale = Math.Min(drawArea.Width / Math.Max(TopBase, BottomBase), drawArea.Height / Height);

        var topWidth = (float)(TopBase * scale);
        var bottomWidth = (float)(BottomBase * scale);
        var trapezoidHeight = (float)(Height * scale);

        var centerX = drawArea.Left + drawArea.Width / 2f;
        var topY = drawArea.Top + (drawArea.Height - trapezoidHeight) / 2f;
        var bottomY = topY + trapezoidHeight;

        PointF[] points = [
            new(centerX - topWidth / 2f, topY),
            new(centerX + topWidth / 2f, topY),
            new(centerX + bottomWidth / 2f, bottomY),
            new(centerX - bottomWidth / 2f, bottomY)
        ];

        using Brush fillBrush = new SolidBrush(Color.FromArgb(180, 135, 206, 250));
        using Pen borderPen = new(Color.SteelBlue, 2);

        graphics.FillPolygon(fillBrush, points);
        graphics.DrawPolygon(borderPen, points);

        var info =
            $"Верхня основа: {TopBase:0.##}\n" +
            $"Нижня основа: {BottomBase:0.##}\n" +
            $"Висота: {Height:0.##}\n" +
            $"Площа: {GetArea():0.##}";

        graphics.DrawString(info, textFont, textBrush, drawArea.Left, drawArea.Top);
    }

    public override string ToString() { return $"Trapezoid(TopBase={TopBase}, BottomBase={BottomBase}, Height={Height})"; }
}


internal static class TrapezoidXmlStorage {
    public static void Save(string path, Trapezoid trapezoid) {
        XmlSerializer serializer = new(typeof(Trapezoid));
        using FileStream fileStream = new(path, FileMode.Create, FileAccess.Write);
        serializer.Serialize(fileStream, trapezoid);
    }

    public static Trapezoid Load(string path) {
        XmlSerializer serializer = new(typeof(Trapezoid));
        using FileStream fileStream = new(path, FileMode.Open, FileAccess.Read);
        var obj = serializer.Deserialize(fileStream);

        return obj as Trapezoid
            ?? throw new InvalidDataException("Не вдалося десеріалізувати XML-файл.");
    }
}

internal static class AppStateBinaryStorage {
    private const string Signature = "APP_STATE_V1";

    public static void Save(string path, AppState appState) {
        using FileStream fileStream = new(path, FileMode.Create, FileAccess.Write);
        using BinaryWriter writer = new(fileStream);

        writer.Write(Signature);
        writer.Write(appState.FormLeft);
        writer.Write(appState.FormTop);
        writer.Write(appState.FormWidth);
        writer.Write(appState.FormHeight);
        writer.Write((int)appState.WindowState);
        writer.Write(appState.MainSplitRatio);
        writer.Write(appState.RightSplitRatio);
    }

    public static AppState Load(string path) {
        using FileStream fileStream = new(path, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new(fileStream);
        if (reader.ReadString() != Signature) {
            throw new InvalidDataException("Невірний формат файлу стану програми.");
        }

        return new AppState {
            FormLeft = reader.ReadInt32(),
            FormTop = reader.ReadInt32(),
            FormWidth = reader.ReadInt32(),
            FormHeight = reader.ReadInt32(),
            WindowState = (FormWindowState)reader.ReadInt32(),
            MainSplitRatio = reader.ReadDouble(),
            RightSplitRatio = reader.ReadDouble()
        };
    }
}


internal class AppState {
    public int FormLeft { get; init; }
    public int FormTop { get; init; }
    public int FormWidth { get; init; }
    public int FormHeight { get; init; }
    public FormWindowState WindowState { get; init; }

    public double MainSplitRatio { get; init; }
    public double RightSplitRatio { get; init; }
}
