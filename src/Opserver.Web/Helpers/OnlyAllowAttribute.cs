using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Opserver.Controllers;
using Opserver.Models;

namespace Opserver.Helpers
{
    /// <summary>
    /// Constrain routes to certain <see cref="Roles"/>.  Can be placed at the class or method level.
    /// </summary>
    /// <remarks>
    /// When constrainting an entire controller/class, per-route additions can be made using the <see cref="AlsoAllowAttribute"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public sealed class OnlyAllowAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        public string Role { get; set; }

        public OnlyAllowAttribute(string role)
        {
            Role = role ?? throw new ArgumentOutOfRangeException(nameof(role));
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (Current.User.Is(Role))
            {
                return; // Authorized via OnlyAllow
            }
            // TODO: Make this work for `n` [AlsoAllow], or take a params perhaps
            if (context.ActionDescriptor is ControllerActionDescriptor cad
                && cad?.MethodInfo.GetCustomAttributes(typeof(AlsoAllowAttribute), inherit: false).SingleOrDefault() is AlsoAllowAttribute alsoAllow
                && Current.User.Is(alsoAllow.Role))
            {
                return; // Authorized via AlsoAllow
            }

            if (!context.HttpContext.User.Identity.IsAuthenticated)
            {
                // it isn't needed to set unauthorized result 
                // as the base class already requires the user to be authenticated
                // this also makes redirect to a login page work properly
                // context.Result = new UnauthorizedResult();
                return;
            }

            // TODO: Sanity check
            context.Result = new RedirectToActionResult(nameof(MiscController.AccessDenied), "Misc", null);
        }
    }

    /// <summary>
    /// Specifies that an action method constrained by a class-level <see cref="OnlyAllowAttribute"/> can authorize additional <see cref="Roles"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class AlsoAllowAttribute : Attribute
    {
        public string Role { get; set; }

        public AlsoAllowAttribute(string role)
        {
            Role = role ?? throw new ArgumentOutOfRangeException(nameof(role));
        }
    }
}
