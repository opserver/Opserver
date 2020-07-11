using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Opserver.Views.Login;

namespace Opserver.Controllers
{
    public class AuthController : StatusController
    {
        public AuthController(IOptions<OpserverSettings> _settings) : base(_settings) { }

        [AllowAnonymous]
        [Route("login"), HttpGet]
        public ActionResult Login(string returnUrl)
        {
            if (!Current.Security.IsConfigured)
            {
                return View("NoConfiguration");
            }

            if (returnUrl == "/")
            {
                return RedirectToAction(nameof(Login));
            }

            var vd = new LoginModel();
            return View(vd);
        }

        [AllowAnonymous]
        [Route("login"), HttpPost]
        public async Task<ActionResult> Login(string user, string pass, string url)
        {
            var vd = new LoginModel();
            if (Current.Security.ValidateUser(user, pass))
            {
                var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user)
                    };
                var userIdentity = new ClaimsIdentity(claims, "login");
                var principal = new ClaimsPrincipal(userIdentity);
                await HttpContext.SignInAsync(principal);
                return Redirect(url.HasValue() ? url : "~/");
            }
            vd.ErrorMessage = "Login failed";

            return View("Login", vd);
        }

        [Route("logout")]
        public async Task<ActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }
    }
}
