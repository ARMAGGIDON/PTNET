using System.Text;

namespace  lab01 {
// Абстрактний базовий клас
abstract class Triangle(double sideA, double sideB, double angle) {
    public abstract string TriangleType { get; }


    public double SideA { get; protected set; } = ValidateSide(sideA);
    public double SideB { get; protected set; } = ValidateSide(sideB);
    public double Angle { get; protected set; } = ValidateAngle(angle);

    // Конструктор без параметрів
    protected Triangle() : this(1, 1, 60) { }

    // Конструктор копіювання
    protected Triangle(Triangle other) : this(other.SideA, other.SideB, other.Angle) { }

    // Абстрактний метод обчислення площі
    public abstract double CalculateArea();

    // Абстрактний метод обчислення периметра
    public abstract double CalculatePerimeter();

    // Поліморфний метод
    public abstract void PrintAreaAndPerimeter();

    // Віртуальний метод
    public virtual string GetInfo() { return $"{TriangleType}: сторона A = {SideA:F2}, сторона B = {SideB:F2}, кут між ними = {Angle:F2}°"; }

    // Невіртуальний метод для демонстрації раннього зв'язування
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Даний метод спеціально екземплярний для демонстрації раннього зв'язування.")]
    public string GetBindingInfo() { return "Раннє зв'язування -> викликано метод базового класу Triangle"; }

    protected double CalculateThirdSide() {
        double angleRadians = Angle * Math.PI / 180.0;

        return Math.Sqrt(
            SideA * SideA +
            SideB * SideB -
            2 * SideA * SideB * Math.Cos(angleRadians));
    }

    private static double ValidateSide(double side) {
        if (side <= 0)
            throw new ArgumentOutOfRangeException(nameof(side), "Довжина сторони повинна бути більшою за 0.");
        return side;
    }

    private static double ValidateAngle(double angle) {
        if (angle <= 0 || angle >= 180)
            throw new ArgumentOutOfRangeException(nameof(angle), "Кут повинен бути в межах від 0 до 180 градусів.");
        return angle;
    }

    public override string ToString() => GetInfo();

    public override bool Equals(object? obj) {
        if (ReferenceEquals(this, obj))
            return true;

        if (obj is null || GetType() != obj.GetType())
            return false;

        Triangle other = (Triangle)obj;

        return SideA == other.SideA
                && SideB == other.SideB
                && Angle == other.Angle;
    }

    public override int GetHashCode() { return HashCode.Combine(GetType(), SideA, SideB, Angle); }
}


class EquilateralTriangle : Triangle {
    public override string TriangleType => "Рівносторонній трикутник";

    public double Side => SideA;

    public EquilateralTriangle() : base(1, 1, 60) { }

    public EquilateralTriangle(double side) : base(side, side, 60) { }

    public EquilateralTriangle(EquilateralTriangle other) : base(other) { }

    public override double CalculateArea() { return Math.Sqrt(3) / 4 * Side * Side; }

    public override double CalculatePerimeter() { return 3 * Side; }

    public override void PrintAreaAndPerimeter() { Console.WriteLine($"{TriangleType}: площа = {CalculateArea():F2}, периметр = {CalculatePerimeter():F2}"); }

    public override string GetInfo() { return $"{TriangleType}: сторона = {Side:F2}"; }

    // Приховування методу базового класу для демонстрації раннього зв'язування
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Даний метод спеціально екземплярний для демонстрації раннього зв'язування.")]
    public new string GetBindingInfo() { return "Раннє зв'язування -> викликано метод похідного класу EquilateralTriangle"; }

    public override bool Equals(object? obj) {
        if (!base.Equals(obj))
            return false;

        EquilateralTriangle other = (EquilateralTriangle)obj;
        return Side == other.Side;
    }

    public override int GetHashCode() { return HashCode.Combine(base.GetHashCode(), Side); }
}


class IsoscelesTriangle : Triangle {
    public override string TriangleType => "Рівнобедрений трикутник";

    public double EqualSide => SideA;
    public double VertexAngle => Angle;

    public IsoscelesTriangle() : base(1, 1, 60) { }

    public IsoscelesTriangle(double equalSide, double vertexAngle) : base(equalSide, equalSide, vertexAngle) { }

    public IsoscelesTriangle(IsoscelesTriangle other) : base(other) { }

    public override double CalculateArea() { return 0.5 * EqualSide * EqualSide * Math.Sin(VertexAngle * Math.PI / 180.0); }

    public override double CalculatePerimeter() { return 2 * EqualSide + CalculateThirdSide(); }

    public override void PrintAreaAndPerimeter() { Console.WriteLine($"{TriangleType}: площа = {CalculateArea():F2}, периметр = {CalculatePerimeter():F2}"); }

    public override string GetInfo() { return $"{TriangleType}: бічні сторони = {EqualSide:F2}, кут між ними = {VertexAngle:F2}°"; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Даний метод спеціально екземплярний для демонстрації раннього зв'язування.")]
    public new string GetBindingInfo() { return "Раннє зв'язування -> викликано метод похідного класу IsoscelesTriangle"; }

    public override bool Equals(object? obj) {
        if (!base.Equals(obj))
            return false;

        IsoscelesTriangle other = (IsoscelesTriangle)obj;
        return EqualSide == other.EqualSide
                && VertexAngle == other.VertexAngle;
    }

    public override int GetHashCode() { return HashCode.Combine(base.GetHashCode(), EqualSide, VertexAngle); }
}


class RightTriangle : Triangle {
    public override string TriangleType => "Прямокутний трикутник";

    public double LegA => SideA;
    public double LegB => SideB;

    public RightTriangle() : base(1, 1, 90) { }

    public RightTriangle(double legA, double legB) : base(legA, legB, 90) { }

    public RightTriangle(RightTriangle other) : base(other) { }

    public override double CalculateArea() { return LegA * LegB / 2; }

    public override double CalculatePerimeter() { return LegA + LegB + CalculateThirdSide(); }

    public override void PrintAreaAndPerimeter() {
        Console.WriteLine($"{TriangleType}: площа = {CalculateArea():F2}, периметр = {CalculatePerimeter():F2}"); }

    public override string GetInfo() { return $"{TriangleType}: катет A = {LegA:F2}, катет B = {LegB:F2}, кут між ними = {Angle:F2}°"; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Даний метод спеціально екземплярний для демонстрації раннього зв'язування.")]
    public new string GetBindingInfo() { return "Раннє зв'язування -> викликано метод похідного класу RightTriangle"; }

    public override bool Equals(object? obj) {
        if (!base.Equals(obj))
            return false;

        RightTriangle other = (RightTriangle)obj;
        return LegA == other.LegA && LegB == other.LegB;
    }

    public override int GetHashCode() { return HashCode.Combine(base.GetHashCode(), LegA, LegB); }
}


class Program {
    static void Main() {
        Console.OutputEncoding = Encoding.UTF8;
        PrintHeader("1. Створення об'єктів різними конструкторами");


        // Конструктори без параметрів
        EquilateralTriangle defaultEquilateral = new();
        IsoscelesTriangle defaultIsosceles = new();
        RightTriangle defaultRight = new();

        CLine(defaultEquilateral.GetInfo());
        CLine(defaultIsosceles.GetInfo());
        CLine(defaultRight.GetInfo());

        
        CLine();

        // Конструктори з параметрами
        EquilateralTriangle equilateral1 = new(6);
        IsoscelesTriangle isosceles1 = new(5, 40);
        RightTriangle right1 = new(3, 4);

        CLine(equilateral1.GetInfo());
        CLine(isosceles1.GetInfo());
        CLine(right1.GetInfo());


        // Конструктори копіювання
        EquilateralTriangle equilateral2 = new(equilateral1);
        IsoscelesTriangle isosceles2 = new(isosceles1);
        RightTriangle right2 = new(right1);

        PrintHeader("2. Перевірка Equals: порівняння значень, а не посилань");

        CLine($"equilateral1.Equals(equilateral2): {equilateral1.Equals(equilateral2)}");
        CLine($"ReferenceEquals(equilateral1, equilateral2): {ReferenceEquals(equilateral1, equilateral2)}");

        CLine();

        CLine($"isosceles1.Equals(isosceles2): {isosceles1.Equals(isosceles2)}");
        CLine($"ReferenceEquals(isosceles1, isosceles2): {ReferenceEquals(isosceles1, isosceles2)}");

        CLine();

        CLine($"right1.Equals(right2): {right1.Equals(right2)}");
        CLine($"ReferenceEquals(right1, right2): {ReferenceEquals(right1, right2)}");


        PrintHeader("3. Список базового класу з посиланнями на похідні класи");

        List<Triangle> triangles = [equilateral1, isosceles1, right1];

        foreach (Triangle triangle in triangles) {
            CLine(triangle.GetInfo());
        }

        PrintHeader("4. Поліморфізм");

        CLine("Метод PrintAreaAndPerimeter() викликається через посилання на базовий клас:");
        CLine();

        foreach (Triangle triangle in triangles) {
            triangle.PrintAreaAndPerimeter();
        }

        PrintHeader("5. Обчислення площі та периметра");

        foreach (Triangle triangle in triangles) {
            CLine(triangle.GetInfo());
            CLine($"Площа: {triangle.CalculateArea():F2}");
            CLine($"Периметр: {triangle.CalculatePerimeter():F2}");
            CLine();
        }

        PrintHeader("6. Різниця між раннім та пізнім зв'язуванням");

        EquilateralTriangle concreteTriangle = new(8);
        Triangle baseReference = concreteTriangle;

        CLine("Раннє зв'язування (невіртуальний метод, вибір на етапі компіляції):");
        CLine("Виклик через змінну типу EquilateralTriangle:");
        CLine(concreteTriangle.GetBindingInfo());

        CLine("Виклик через змінну типу Triangle:");
        CLine(baseReference.GetBindingInfo());

        CLine();

        CLine("Пізнє зв'язування (віртуальний/абстрактний метод, вибір під час виконання):");
        CLine("Виклик PrintAreaAndPerimeter() через змінну типу EquilateralTriangle:");
        concreteTriangle.PrintAreaAndPerimeter();

        CLine("Виклик PrintAreaAndPerimeter() через змінну типу Triangle:");
        baseReference.PrintAreaAndPerimeter();

        PrintHeader("7. Додаткова перевірка Equals");

        EquilateralTriangle equilateral3 = new(7);
        EquilateralTriangle equilateral4 = new(7);
        EquilateralTriangle equilateral5 = new(9);

        CLine($"equilateral3.Equals(equilateral4): {equilateral3.Equals(equilateral4)}");
        CLine($"equilateral3.Equals(equilateral5): {equilateral3.Equals(equilateral5)}");
        CLine($"ReferenceEquals(equilateral3, equilateral4): {ReferenceEquals(equilateral3, equilateral4)}");
    }

    static void CLine(string text = "") { Console.WriteLine(text); }

    static void PrintHeader(string title) {
        CLine();
        CLine(new string('=', 80));
        CLine(title);
        CLine(new string('=', 80));
    }
}
}