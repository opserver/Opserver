using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace StackExchange.Opserver.Data.HAProxy
{
    public class StatProperty
    {
        /// <summary>
        /// The HAProxy Stat name
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// The position HAProxy places this attribute in the CSV (parsing order)
        /// </summary>
        public int Position { get; internal set; }
        /// <summary>
        /// The property info for this stat's property, for populating in parsing
        /// </summary>
        public PropertyInfo PropertyInfo { get; internal set; }

        /// <summary>
        /// Creates a StatProperty from a property's PropertyInfo
        /// </summary>
        /// <param name="p">The propertyInfo decorated with a StatAttribute</param>
        public StatProperty(PropertyInfo p)
        {
            var sa = p.GetCustomAttributes(typeof(StatAttribute), false)[0] as StatAttribute;
            Name = sa.Name;
            Position = sa.Position;
            PropertyInfo = p;
        }

        #region Static Collection

        //Load properties to parse initially on load
        public static List<StatProperty> AllOrdered = GetAll();

        private static List<StatProperty> GetAll()
        {
            return typeof(Item).GetProperties()
                   .Where(p => p.IsDefined(typeof(StatAttribute), false))
                   .Select(p => new StatProperty(p))
                   .OrderBy(s => s.Position)
                   .ToList();
        }

        #endregion
    }
}