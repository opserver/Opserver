using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
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
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class OnlyAllowAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        public new Roles Roles { get; set; }

        public OnlyAllowAttribute(Roles roles)
        {
            if (roles == Roles.None)
                throw new ArgumentOutOfRangeException(nameof(roles));

            Roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (Current.IsInRole(Roles))
            {
                return; // Authorized via OnlyAllow
            }
            if (context.ActionDescriptor is ControllerActionDescriptor cad
                && cad?.MethodInfo.GetCustomAttributes(typeof(AlsoAllowAttribute), inherit: false).SingleOrDefault() is AlsoAllowAttribute alsoAllow
                && Current.IsInRole(alsoAllow.Roles))
            {
                return; // Authorized via AlsoAllow
            }

            // TODO: Sanity check
            context.Result = new RedirectToActionResult(nameof(MiscController.AccessDenied), "Misc", null);
        }
    }

    /// <summary>
    /// Specifies that an action method constrained by a class-level <see cref="OnlyAllowAttribute"/> can authorize additional <see cref="Roles"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
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
}
