using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace lab07.Pages;

public class SessionResetModel : PageModel {
    public const string AttemptsKey = "lab07_failed_attempts";

    public IActionResult OnPost(string? returnUrl) {
        HttpContext.Session.Remove(AttemptsKey);
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) {
            return LocalRedirect(returnUrl);
        }
        return RedirectToPage("/Index");
    }
    public IActionResult OnGet() {
        var referer = Request.Headers.Referer.ToString();
        if (!string.IsNullOrEmpty(referer)) {
            return Redirect(referer);
        }

        OnPost(referer);
        return RedirectToPage("/Index");
    }
}