using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Opserver.Security;
using Opserver.Views.Login;

namespace Opserver.Controllers
{
    public partial class AuthController : StatusController
    {
        public AuthController(IOptions<OpserverSettings> settings) : base(settings) { }

        [AllowAnonymous]
        [Route("login"), HttpGet]
        public async Task<ActionResult> Login(string returnUrl)
        {
            if (!Current.Security.IsConfigured)
            {
                return View("NoConfiguration");
            }

            if (returnUrl == "/")
            {
                return RedirectToAction(nameof(Login));
            }

            switch (Current.Security.FlowType)
            {
                case SecurityProviderFlowType.None:
                case SecurityProviderFlowType.UsernamePassword:
                    return View(new LoginModel());
                case SecurityProviderFlowType.OAuth:
                    // TODO: redirect to OAuth URL
                    return new NotFoundResult();
            }
        }

        [AllowAnonymous]
        [Route("login"), HttpPost]
        public async Task<ActionResult> Login(string user, string pass, string url)
        {
            if (Current.Security.FlowType == SecurityProviderFlowType.OAuth)
            {
                return new NotFoundResult();
            }

            var vd = new LoginModel();
            if (Current.Security.ValidateToken(new UserNamePasswordToken(user, pass)))
            {
                await SignInAsync(user);
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

        private Task SignInAsync(string user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user)
            };
            var identity = new ClaimsIdentity(claims, "login");
            var principal = new ClaimsPrincipal(identity);
            return HttpContext.SignInAsync(principal);
        }
    }
}
