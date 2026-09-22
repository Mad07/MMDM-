using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Mark1.Controllers
{
    [AllowAnonymous]
    public class CultureController : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            // Culture (number/date parsing+formatting) is pinned to "en" regardless of the chosen
            // UI language - es-CR uses a comma decimal separator, which would otherwise make typed
            // amounts like "126.42" fail to bind as soon as a user switched the UI to Spanish.
            // UICulture is what actually drives which language resource strings get shown.
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("en", culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });

            return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
        }
    }
}
