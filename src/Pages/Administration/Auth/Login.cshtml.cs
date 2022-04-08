
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MikeCodesDotNET.Pages
{
    public class LoginModel : PageModel
    {
        public async Task OnGet(string redirectUri)
        {
            if (string.IsNullOrEmpty(redirectUri))
                redirectUri = "/admin";

            await HttpContext.ChallengeAsync("Auth0", new AuthenticationProperties
            {
                RedirectUri = redirectUri
            });
        }
    }
}
