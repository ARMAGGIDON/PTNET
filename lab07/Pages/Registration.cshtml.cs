using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace lab07.Pages;

public class RegistrationModel(IWebHostEnvironment environment) : PageModel {
    public int MaxAttempts => 3;
    public int FailedAttempts => HttpContext.Session.GetInt32(SessionResetModel.AttemptsKey) ?? 0;
    public bool IsBlocked => FailedAttempts >= MaxAttempts;

    [BindProperty] public string Surname { get; set; } = "";
    [BindProperty] public string FirstName { get; set; } = "";
    [BindProperty] public string Patronymic { get; set; } = "";
    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Login { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";

    public string? Message { get; private set; }
    public bool IsSuccess { get; private set; }

    public async Task<IActionResult> OnPostAsync() {
        if (IsBlocked) {
            Message = "Кількість спроб вичерпано. Реєстрацію заблоковано до завершення сеансу.";
            return Page();
        }

        Surname = Surname.Trim();
        FirstName = FirstName.Trim();
        Patronymic = Patronymic.Trim();
        Email = Email.Trim();
        Login = Login.Trim();

        if (new[] { Surname, FirstName, Patronymic, Email, Login, Password }.Any(string.IsNullOrWhiteSpace) ||
            !new EmailAddressAttribute().IsValid(Email)) {
            Message = "Заповніть усі поля та введіть коректний email.";
            return Page();
        }

        var directory = Path.Combine(environment.ContentRootPath, "AppData");
        var loginFile = Path.Combine(directory, "login.txt");
        var passwordFile = Path.Combine(directory, "pass.txt");

        if (!System.IO.File.Exists(loginFile) || !System.IO.File.Exists(passwordFile)) {
            Message = "Не знайдено файли login.txt або pass.txt.";
            return Page();
        }

        var rawLogin = await System.IO.File.ReadAllTextAsync(loginFile);
        var rawPassword = await System.IO.File.ReadAllTextAsync(passwordFile);
        if (Login != rawLogin.Trim() || Password != rawPassword.Trim()) {
            HttpContext.Session.SetInt32(SessionResetModel.AttemptsKey, FailedAttempts + 1);
            Message = IsBlocked
                ? "Три невдалі спроби. Подальшу реєстрацію заблоковано до завершення сеансу."
                : $"Неправильний логін або пароль. Залишилося спроб: {MaxAttempts - FailedAttempts}.";
            return Page();
        }

        Directory.CreateDirectory(directory);
        await System.IO.File.AppendAllTextAsync(Path.Combine(directory, "registrations.txt"), 
            $"Дата: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" +
                    $"Прізвище: {Clean(Surname)}{Environment.NewLine}" +
                    $"Ім’я: {Clean(FirstName)}{Environment.NewLine}" +
                    $"По батькові: {Clean(Patronymic)}{Environment.NewLine}" +
                    $"Email: {Clean(Email)}{Environment.NewLine}" +
                    $"Логін: {Clean(Login)}{Environment.NewLine}---{Environment.NewLine}", Encoding.UTF8);

        HttpContext.Session.Remove(SessionResetModel.AttemptsKey);
        Password = "";
        IsSuccess = true;
        Message = "Реєстрацію успішно завершено! Анкету збережено у AppData/registrations.txt.";
        return Page();
    }

    private static string Clean(string value) {
        return value.Replace("\r", " ").Replace("\n", " ");
    }
}
