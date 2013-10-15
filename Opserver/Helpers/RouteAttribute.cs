using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Web.Routing;

namespace StackExchange.Opserver.Helpers
{
    /// <summary>
    /// Allows MVC routing urls to be declared on the action they map to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class RouteAttribute : ActionMethodSelectorAttribute, IComparable<RouteAttribute>
    {

        /// <summary>
        /// Within the StackOverflow.dll assembly, looks for any action methods that have the RouteAttribute defined, 
        /// adding the routes to the parameter 'routes' collection.
        /// </summary>
        public static void MapDecoratedRoutes(RouteCollection routes)
        {
            MapDecoratedRoutes(routes, Assembly.GetCallingAssembly());
        }

        /// <summary>
        /// Looks for any action methods in 'assemblyToSearch' that have the RouteAttribute defined, 
        /// adding the routes to the parameter 'routes' collection.
        /// </summary>
        /// <param name="routes">The routes collection to add the attribute mappings to</param>
        /// <param name="assemblyToSearch">An assembly containing Controllers with public methods decorated with the RouteAttribute</param>
        public static void MapDecoratedRoutes(RouteCollection routes, Assembly assemblyToSearch)
        {
            var decoratedMethods = from t in assemblyToSearch.GetTypes()
                                   where t.IsSubclassOf(typeof(Controller))
                                   from m in t.GetMethods()
                                   where m.IsDefined(typeof(RouteAttribute), false)
                                   select m;

            Debug.WriteLine("MapDecoratedRoutes - found {0} methods decorated with RouteAttribute", decoratedMethods.Count());

            var methodsToRegister = new SortedDictionary<RouteAttribute, MethodInfo>(); // sort urls alphabetically via RouteAttribute's IComparable implementation

            // first, collect all the methods decorated with our RouteAttribute
            foreach (var method in decoratedMethods)
            {
                foreach (var attr in method.GetCustomAttributes(typeof(RouteAttribute), false))
                {
                    var ra = (RouteAttribute)attr;
                    if (!methodsToRegister.Any(p => p.Key.Url.Equals(ra.Url)))
                        methodsToRegister.Add(ra, method);
                    else
                        Debug.WriteLine("MapDecoratedRoutes - found duplicate url -> " + ra.Url);
                }
            }

            // now register the unique urls to the Controller.Method that they were decorated upon
            foreach (var pair in methodsToRegister)
            {
                var attr = pair.Key;
                var method = pair.Value;
                var action = method.Name;

                var controllerType = method.ReflectedType;
                var controllerName = controllerType.Name.Replace("Controller", "");
                var controllerNamespace = controllerType.FullName.Replace("." + controllerType.Name, "");

                Debug.WriteLine("MapDecoratedRoutes - mapping url '{0}' to {1}.{2}.{3}", attr.Url, controllerNamespace, controllerName, action);

                var route = new Route(attr.Url, new MvcRouteHandler())
                    {
                        Defaults = new RouteValueDictionary(new {controller = controllerName, action})
                    };

                // optional parameters are specified like: "users/filter/{filter?}"
                if (attr.OptionalParameters != null)
                {
                    foreach (var optional in attr.OptionalParameters)
                        route.Defaults.Add(optional, "");                    
                }

                // constraints are specified like: @"users/{id:\d+}" or "users/{id:INT}"
                if (attr.Constraints != null)
                {
                    route.Constraints = new RouteValueDictionary();

                    foreach (var constraint in attr.Constraints)
                        route.Constraints.Add(constraint.Key, constraint.Value);
                }

                // fully-qualify route to its controller method by adding the namespace; allows multiple assemblies to share controller names/routes
                // e.g. StackOverflow.Controllers.HomeController, StackOverflow.Api.Controllers.HomeController
                route.DataTokens = new RouteValueDictionary(new { namespaces = new[] { controllerNamespace } });

                routes.Add(attr.Name, route);
            }
        }


        public RouteAttribute(string url)
            : this(url, "", null, RoutePriority.Default)
        {
        }

        public RouteAttribute(string url, HttpVerbs verbs)
            : this(url, "", verbs, RoutePriority.Default)
        {
        }

        public RouteAttribute(string url, RoutePriority priority)
            : this(url, "", null, priority)
        {
        }

        public RouteAttribute(string url, HttpVerbs verbs, RoutePriority priority)
            : this(url, "", verbs, priority)
        {
        }        

        private RouteAttribute(string url, string name, HttpVerbs? verbs, RoutePriority priority)
        {
            Url = url.ToLower();
            Name = name;
            AcceptVerbs = verbs;
            Priority = priority;
        }


        /// <summary>
        /// The explicit verbs that the route will allow.  If null, all verbs are valid.
        /// </summary>
        public HttpVerbs? AcceptVerbs { get; set; }

        /// <summary>
        /// Optional name to allow this route to be referred to later.
        /// </summary>
        public string Name { get; set; }

        private string _url;
        /// <summary>
        /// The request url that will map to the decorated action method.
        /// Specifying optional parameters: "/users/{id}/{name?}" where 'name' may be omitted.
        /// Specifying constraints on parameters: "/users/{id:(\d+)}" where 'id' matches a regex for at least one number
        /// Constraints can also be predefined: "/users/{id:INT}" where 'id' will be constrained to the predefined INT regex <see cref="PredefinedConstraints"/>.
        /// </summary>
        public string Url
        {
            get { return _url; }
            set { _url = ParseUrlForConstraints(value); /* side-effects include setting this.OptionalParameters and this.Constraints */ }
        }

        /// <summary>
        /// Determines when this route is registered in the <see cref="System.Web.Routing.RouteCollection"/>.  The higher the priority, the sooner
        /// this route is added to the collection, making it match before other registered routes for a given url.
        /// </summary>
        public RoutePriority Priority { get; set; }

        /// <summary>
        /// Gets any optional parameters contained by this Url. Optional parameters are specified with a ?, e.g. "users/{id}/{name?}".
        /// </summary>
        public string[] OptionalParameters { get; private set; }

        /// <summary>
        /// Based on /users/{id:(\d+)(;\d+)*}
        /// </summary>
        public Dictionary<string, string> Constraints { get; private set; }

        /// <summary>
        /// Contains keys that can be used in routes for well-known constraints, e.g. "users/{id:INT}" - this route would ensure the 'id' parameter
        /// would only accept at least one number to match.
        /// </summary>
        public static readonly Dictionary<string, string> PredefinedConstraints = new Dictionary<string, string> 
        { 
            { "INT",            @"-?\d{1,9}" }, // yes, int32 could have 10 digits, but do we really think we'll have ids that big anytime soon?
            { "INTS_DELIMITED", @"-?\d+(;-?\d+)*" },
            { "GUID",           @"\b[A-Fa-f0-9]{8}(?:-[A-Fa-f0-9]{4}){3}-[A-Za-z0-9]{12}\b" }
        };

        public override bool IsValidForRequest(ControllerContext cc, MethodInfo mi)
        {
            var result = true;

            if (AcceptVerbs.HasValue)
                result = new AcceptVerbsAttribute(AcceptVerbs.Value).IsValidForRequest(cc, mi);

            return result;
        }

        public override string ToString()
        {
            return (AcceptVerbs.HasValue ? AcceptVerbs.Value.ToString().ToUpper() + " " : "") + Url;
        }

        public int CompareTo(RouteAttribute other)
        {
            var result = other.Priority.CompareTo(Priority);

            if (result == 0) // sort like priorities in asc alphabetical order
                result = Url.CompareTo(other.Url);

            return result;
        }

        private string ParseUrlForConstraints(string url)
        {
            // example url with both optional specifier and a constraint: "posts/{id:INT}/edit-submit/{revisionguid?:GUID}"
            // note that a constraint regex cannot use { } for quantifiers
            var matches = Regex.Matches(url, @"{(?<param>\w+)(?<metadata>(?<optional>\?)?(?::(?<constraint>[^}]*))?)}", RegexOptions.IgnoreCase);

            if (matches.Count == 0) return url; // vanilla route without any parameters, e.g. "home", "users/login"   

            var result = url;
            var optionals = new List<string>();
            var constraints = new Dictionary<string, string>();

            foreach (Match m in matches)
            {
                var metadata = m.Groups["metadata"].Value; // all the extra info after the parameter name
                if (!metadata.HasValue()) continue;

                // we have optional specifier and/or constraints
                var param = m.Groups["param"].Value; // the name, e.g. 'id' in "/users/{id}"
                var isOptional = m.Groups["optional"].Success;
                    
                if (isOptional)
                    optionals.Add(param);

                var constraint = m.Groups["constraint"].Value;
                if (constraint.HasValue())
                {
                    string predefined;
                    if (PredefinedConstraints.TryGetValue(constraint.ToUpper(), out predefined))
                        constraint = predefined;

                    if (isOptional)
                        constraint = "(" + constraint + ")?";

                    constraints.Add(param, constraint);
                }

                result = result.Replace(metadata, "");
            }

            if (optionals.Count > 0) OptionalParameters = optionals.ToArray();
            if (constraints.Count > 0) Constraints = constraints;

            return result;
        }
    }

    /// <summary>
    /// Contains values that control when routes are added to the main <see cref="System.Web.Routing.RouteCollection"/>.
    /// </summary>
    /// <remarks>Routes with identical RoutePriority are registered in alphabetical order.  RoutePriority allows for different strata of routes.</remarks>
    public enum RoutePriority
    {
        /// <summary>
        /// A route with Low priority will be registered after routes with Default and High priorities.
        /// </summary>
        Low = 0,
        Default = 1,
        High = 2
    }
}
