using System.Globalization;
using System.Windows;

namespace lab06;

public partial class MainWindow {
    public MainWindow() {
        InitializeComponent();
    }
    
    private void CalculateButton_Click(object sender, RoutedEventArgs e) {
        var input = SideTextBox.Text.Replace(',', '.');
        if (!double.TryParse(input, CultureInfo.InvariantCulture, out var side) || side <= 0) {
            MessageBox.Show("Введіть додатне числове значення сторони.", "Помилка",
                MessageBoxButton.OK, MessageBoxImage.Warning
            );
            return;
        }

        var area = side * side;
        var perimeter = 4 * side;
        AreaTextBlock.Text = $"Площа: {area:g} од²";
        PerimeterTextBlock.Text = $"Периметр: {perimeter:g} од.";
    }
}