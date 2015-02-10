using System.Web.Mvc;
using System.Web.Security;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Views.Login;
using Roles = StackExchange.Opserver.Models.Roles;

namespace StackExchange.Opserver.Controllers
{
    public class LoginController : StatusController
    {
        [Route("login"), HttpGet, AlsoAllow(Roles.Anonymous)]
        public ActionResult Login(string returnUrl)
        {
            if (returnUrl == "/")
                return Redirect("/login");

            var vd = new LoginModel();
            return View(vd);
        }

        [ValidateInput(false)]
        [Route("login"), HttpPost, AlsoAllow(Roles.Anonymous)]
        public ActionResult Login(string user, string pass, string url)
        {
            var vd = new LoginModel();
            if (Current.Security.ValidateUser(user, pass))
            {
                var cookie = FormsAuthentication.GetAuthCookie(user, true);
                if (Current.IsSecureConnection) cookie.Secure = true;
                Response.Cookies.Add(cookie);

                return Redirect(url.HasValue() ? url : "/");
            }
            vd.ErrorMessage = "Login failed";

            return View("~/Views/Login/Login.cshtml", vd);
        }

        [Route("logout"), AlsoAllow(Roles.Anonymous)]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }
    }
}