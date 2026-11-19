using System.Text;
using Microsoft.Data.SqlClient;
using lab08.Data;
using lab08.Tui;

namespace lab08;


internal static class Program {
    private static int Main() {
        Console.OutputEncoding = new UTF8Encoding(false);
        Console.InputEncoding = Encoding.UTF8;

        if (Console.IsInputRedirected || Console.IsOutputRedirected) {
            Console.Error.WriteLine("Програма потребує інтерактивного термінала. " +
                                    "Запустіть її через Terminal у Rider: dotnet run --project lab08");
            return 1;
        }

        try {
            var repository = new DiscRepository();
            repository.Initialize();

            using var canvas = new TerminalCanvas();
            var app = new CatalogApp(repository, canvas);
            app.Run();
            return 0;
        } catch (SqlException ex) {
            Console.ResetColor();
            Console.CursorVisible = true;
            Console.Error.WriteLine($"Помилка SQL Server ({ex.Number}): {ex.Message}");
            Console.Error.WriteLine("Перевірте службу SQL Server та PTNET_LAB08_SERVER / PTNET_LAB08_CONNECTION.");
            return 2;
        } catch (Exception ex) {
            Console.ResetColor();
            Console.CursorVisible = true;
            Console.Error.WriteLine($"Помилка: {ex.Message}");
            return 3;
        }
    }
}
