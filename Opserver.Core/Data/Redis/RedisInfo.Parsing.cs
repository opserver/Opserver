using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace StackExchange.Opserver.Data.Redis
{
    public partial class RedisInfo
    {
        private static readonly ConcurrentDictionary<string, PropertyInfo> _sectionMappings;
        private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> _propertyMappings;

        static RedisInfo()
        {
            _propertyMappings = new ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>>();
            _sectionMappings = new ConcurrentDictionary<string, PropertyInfo>();

            var sections = typeof (RedisInfo).GetProperties().Where(s => typeof(RedisInfoSection).IsAssignableFrom(s.PropertyType));
            foreach (var section in sections)
            {
                _sectionMappings[section.Name] = section;
                var propMaps = _propertyMappings[section.PropertyType] = new Dictionary<string, PropertyInfo>();

                var type = section.PropertyType;
                var props = type.GetProperties().Where(p => p.IsDefined(typeof (RedisInfoPropertyAttribute), false));
                foreach (var prop in props)
                {
                    var propAttribute = prop.GetCustomAttribute<RedisInfoPropertyAttribute>();
                    propMaps[propAttribute.PropertyName] = prop;
                }
            }
        }

        public static RedisInfo FromInfoString(string infoStr)
        {
            var info = new RedisInfo {FullInfoString = infoStr};

            RedisInfoSection currentSection = null;

            var lines = infoStr.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.StartsWith("#"))
                {
                    var sectionName = line.Replace("# ", "");
                    PropertyInfo currentSectionProp;
                    if (_sectionMappings.TryGetValue(sectionName, out currentSectionProp))
                    {
                        currentSection = (RedisInfoSection) currentSectionProp.GetValue(info);
                    }
                    else
                    {
                        currentSection = new RedisInfoSection {Name = sectionName, IsUnrecognized = true};
                        if (info.UnrecognizedSections == null)
                            info.UnrecognizedSections = new List<RedisInfoSection>();
                        info.UnrecognizedSections.Add(currentSection);
                    }
                    continue;
                }
                if (currentSection == null)
                {
                    //TODO: Take care of global pre-2.6 case here
                    continue;
                }

                var splits = line.Split(StringSplits.Colon, 2);
                if (splits.Length != 2)
                {
                    currentSection.MapUnrecognizedLine(line);
                    continue;
                }

                string key = splits[0], value = splits[1];
                currentSection.AddLine(key, value);

                if (currentSection.IsUnrecognized) 
                    continue;
                
                PropertyInfo propertyInfo;
                var prop = _propertyMappings[currentSection.GetType()].TryGetValue(key, out propertyInfo) ? propertyInfo : null;
                if (prop == null)
                {
                    currentSection.MapUnrecognizedLine(key, value);
                    continue;
                }

                try
                {
                    if (prop.PropertyType == typeof (bool))
                    {
                        prop.SetValue(currentSection, value.IsNullOrEmptyReturn("0") != "0");
                    }
                    else
                    {
                        prop.SetValue(currentSection, Convert.ChangeType(value, prop.PropertyType));
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"Error parsing '{value}' from {key} as {prop.PropertyType.Name} for {currentSection.GetType()}.{prop.Name}", e);
                }
            }

            return info;
        }

        public class RedisInfoSection
        {
            public bool IsGlobal { get; internal set; }
            public bool IsUnrecognized { get; internal set; }
            protected string _name { get; set; }
            public virtual string Name { get { return _name ?? Regex.Replace(GetType().Name, "Info$", ""); } internal set { _name = value; } }

            public virtual void MapUnrecognizedLine(string infoLine)
            {
                var splits = infoLine.Split(StringSplits.Colon, 2);
                if (splits.Length == 2) MapUnrecognizedLine(splits[0], splits[1]);
            }
            public virtual void MapUnrecognizedLine(string key, string value) { }

            public RedisInfoSection()
            {
                Lines = new List<RedisInfoLine>();
            }

            public List<RedisInfoLine> Lines { get; internal set; }

            internal virtual void AddLine(string key, string value)
            {
                Lines.Add(new RedisInfoLine(key, value));
            }
        }

        public class RedisInfoLine
        {
            public bool Important { get; internal set; }
            public string Key { get; internal set; }
            public string ParsedValue { get; internal set; }
            public string OriginalValue { get; internal set; }

            public RedisInfoLine(string key, string value)
            {
                Key = key;
                OriginalValue = value;
                Important = IsImportantInfoKey(key);
                ParsedValue = GetInfoValue(key, value);
            }

            private static readonly List<string> _dontFormatList = new List<string>
                {
                    "redis_git",
                    "process_id",
                    "tcp_port",
                    "master_port"
                };
            
            private static string GetInfoValue(string label, string value)
            {
                long l;              

                switch (label)
                {
                    case "uptime_in_seconds":
                        if (long.TryParse(value, out l))
                        {
                            var ts = TimeSpan.FromSeconds(l);
                            return $"{value} ({(int) ts.TotalDays}d {ts.Hours}h {ts.Minutes}m {ts.Seconds}s)";
                        }
                        break;
                    case "last_save_time":
                        if (long.TryParse(value, out l))
                        {
                            var time = l.ToDateTime();
                            return $"{value} ({time.ToRelativeTime()})";
                        }
                        break;
                    case "master_sync_left_bytes":
                        if (long.TryParse(value, out l))
                        {
                            return $"{l:n0} bytes";
                        }
                        break;
                    case "used_memory_rss":
                        if (long.TryParse(value, out l))
                        {
                            return $"{l} ({l.ToSize(precision: 1)})";
                        }
                        break;
                    default:
                        if (!_dontFormatList.Any(label.Contains) && long.TryParse(value, out l))
                        {
                            return $"{l:n0}";
                        }
                        break;
                }
                return value;
            }

            private static bool IsImportantInfoKey(string variableName)
            {
                if (variableName.IsNullOrEmpty())
                    return false;
                if (variableName.StartsWith("slave") || variableName.StartsWith("master_") || variableName.StartsWith("aof_") || variableName.StartsWith("loading_"))
                    return true;

                switch (variableName)
                {
                    case "redis_version":
                    case "uptime_in_seconds":
                    case "connected_clients":
                    case "connected_slaves":
                    case "instantaneous_ops_per_sec":
                    case "pubsub_channels":
                    case "pubsub_patterns":
                    case "total_connections_received":
                    case "total_commands_processed":
                    case "used_memory_human":
                    case "used_memory_rss":
                    case "used_memory_peak_human":
                    case "role":
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}
