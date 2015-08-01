using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using StackExchange.Opserver.Controllers;
using StackExchange.Opserver.Models;

namespace StackExchange.Opserver.Helpers
{
    /// <summary>
    /// Constrain routes to certain <see cref="Roles"/>.  Can be placed at the class or method level.
    /// </summary>
    /// <remarks>
    /// When constrainting an entire controller/class, per-route additions can be made using the <see cref="AlsoAllowAttribute"/>.
    /// </remarks>
    public class OnlyAllowAttribute : AuthorizeAttribute
    {
        private const string ITEMS_KEY = "AlsoAllow.Roles";

        public new Roles Roles { get; set; }

        public OnlyAllowAttribute(Roles roles)
        {
            if (roles == Roles.None)
                throw new ArgumentOutOfRangeException(nameof(roles));

            Roles = roles;
        }

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            // method attribute allows additions to a policy set at the class level
            var alsoAllow = filterContext.ActionDescriptor.GetCustomAttributes(typeof(AlsoAllowAttribute), inherit: false).SingleOrDefault() as AlsoAllowAttribute;
            if (alsoAllow != null)
            {
                filterContext.HttpContext.Items[ITEMS_KEY] = alsoAllow.Roles;
            }

            // this will then call AuthorizeCore - one should view MS' source for OnAuthorization
            base.OnAuthorization(filterContext);
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var alsoAllow = httpContext.Items.Contains(ITEMS_KEY) ? (Roles)httpContext.Items[ITEMS_KEY] : Roles.None;
            var allAllow = Roles | alsoAllow;
            
            var u = Current.User;
            return u != null && u.IsInRole(allAllow); // when false, HandleUnauthorizedRequest executes
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.Result = ((StatusController)filterContext.Controller).AccessDenied();
        }
    }

    /// <summary>
    /// Specifies that an action method constrained by a class-level <see cref="OnlyAllowAttribute"/> can authorize additional <see cref="Roles"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AlsoAllowAttribute : Attribute
    {
        public Roles Roles { get; set; }

        public AlsoAllowAttribute(Roles roles)
        {
            if (roles == Roles.None)
                throw new ArgumentOutOfRangeException(nameof(roles));

            Roles = roles;
        }
    }

    /// <summary>
    /// Shortcut for [Allow(Roles.Developer)]
    /// </summary>
    public class AdminOnlyAttribute : OnlyAllowAttribute
    {
        public AdminOnlyAttribute() : base(Roles.GlobalAdmin) { }
    }
}