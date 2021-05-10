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
        public IActionResult Login(string returnUrl)
        {
            if (!Current.Security.IsConfigured || Current.Security.FlowType == SecurityProviderFlowType.None)
            {
                return View("NoConfiguration");
            }

            if (returnUrl == "/")
            {
                return RedirectToAction(nameof(Login));
            }

            return View(new LoginModel());
        }

        [AllowAnonymous]
        [Route("login"), HttpPost]
        public async Task<IActionResult> Login(string user, string pass, string url)
        {
            var returnUrl = url.HasValue() ? url : "~/";
            if (Current.Security.FlowType == SecurityProviderFlowType.OIDC)
            {
                // OpenID Connect needs to go through an authorization flow
                // before we can login successfully...
                return RedirectToProvider(returnUrl);
            }

            if (!Current.Security.TryValidateToken(new UserNamePasswordToken(user, pass), out var claimsPrincipal))
            {
                return View(
                    "Login",
                    new LoginModel
                    {
                        ErrorMessage = "Login failed"
                    }
                );
            }

            await HttpContext.SignInAsync(claimsPrincipal);
            return Redirect(returnUrl);
        }

        [Route("logout")]
        public async Task<ActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }
    }
}
