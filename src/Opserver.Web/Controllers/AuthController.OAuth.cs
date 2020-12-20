using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Opserver.Security;
using Opserver.Views.Login;

namespace Opserver.Controllers
{
    partial class AuthController
    {
        [AllowAnonymous]
        [Route("login/oauth/callback"), HttpPost]
        public ActionResult OAuthCallback(string code, string error = null)
        {
            if (!Current.Security.IsConfigured)
            {
                return View("NoConfiguration");
            }

            if (Current.Security.FlowType != SecurityProviderFlowType.OAuth)
            {
                return new NotFoundResult();
            }

            //TODO:
            // exchange the authorization code for a token
            // extract the claims from it in order to login the user
        }
    }
}
