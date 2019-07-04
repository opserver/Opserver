using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Security;
using StackExchange.Opserver.Views.Login;
using Roles = StackExchange.Opserver.Models.Roles;

namespace StackExchange.Opserver.Controllers
{
    public class LoginController : StatusController
    {
        public LoginController(IOptions<OpserverSettings> _settings) : base(_settings) { }

        [Route("login"), HttpGet, AlsoAllow(Roles.Anonymous)]
        public ActionResult Login(string returnUrl)
        {
            if (returnUrl == "/")
                return RedirectToAction(nameof(Login));

            var vd = new LoginModel();
            return View(vd);
        }

        [Route("login"), HttpPost, AlsoAllow(Roles.Anonymous)]
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
                await HttpContext.SignInAsync(principal).ConfigureAwait(false);
                return Redirect(url.HasValue() ? url : "~/");
            }
            vd.ErrorMessage = "Login failed";

            return View("~/Views/Login/Login.cshtml", vd);
        }

        [Route("logout"), AlsoAllow(Roles.Anonymous)]
        public async Task<ActionResult> Logout()
        {
            await HttpContext.SignOutAsync().ConfigureAwait(false);
            return RedirectToAction(nameof(Login));
        }
    }
}
