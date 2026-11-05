using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace lab07.Pages;

public class IndexModel : PageModel {
    [BindProperty] public string Surname { get; set; } = "";
    [BindProperty] public string FirstName { get; set; } = "";
    [BindProperty] public string Patronymic { get; set; } = "";
    [BindProperty] public string Email { get; set; } = "";

    public bool HasResult { get; private set; }
    public char[] NameLetters { get; private set; } = [];
    public string Initials { get; private set; } = "";
    public string EmailResult { get; private set; } = "";
    public string? ErrorMessage { get; private set; }

    public void OnPost() {
        Surname = Surname.Trim();
        FirstName = FirstName.Trim();
        Patronymic = Patronymic.Trim();
        Email = Email.Trim();

        if (Surname.Length == 0 || FirstName.Length == 0 || Patronymic.Length == 0) {
            ErrorMessage = "Заповніть прізвище, ім’я та по батькові.";
            return;
        }

        NameLetters = FirstName.ToCharArray();
        Initials = $"{char.ToUpperInvariant(Surname[0])}.{char.ToUpperInvariant(FirstName[0])}." +
                   $"{char.ToUpperInvariant(Patronymic[0])}.";
        EmailResult = new EmailAddressAttribute().IsValid(Email)
            ? "Коректна адреса електронної пошти."
            : "Некоректна адреса електронної пошти.";
        HasResult = true;
    }
}
