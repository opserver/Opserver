using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Opserver.Controllers
{
    [AttributeUsage(AttributeTargets.Method)]
    public class DefaultRoute : RouteAttribute
    {
        private static Dictionary<Type, DefaultRoute> AllRoutes => new Dictionary<Type, DefaultRoute>();

        public DefaultRoute(string template) : base(template) { }

        public static DefaultRoute GetFor(Type t) => AllRoutes.TryGetValue(t, out var route) ? route : null;
    }
}
