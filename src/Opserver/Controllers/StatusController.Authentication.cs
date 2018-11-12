using System.Web.Mvc;
using StackExchange.Opserver.Models;

namespace StackExchange.Opserver.Controllers
{
    public partial class StatusController
    {
        protected override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.User != null)
            {
                filterContext.HttpContext.User = new User(filterContext.HttpContext.User.Identity);
            }
        }
    }
}