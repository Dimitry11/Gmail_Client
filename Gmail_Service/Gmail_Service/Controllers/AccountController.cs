namespace Gmail_Service.Controllers
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;

    public class AccountController : Controller
    {
        public IActionResult Login(string returnUri = "/")
        {
            return Challenge(new AuthenticationProperties() { RedirectUri = returnUri }, "Google");
        }

        [HttpGet]
        [Authorize]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}