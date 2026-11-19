using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jil
{
    /// <summary>
    /// Configuration options for Jil serialization, passed to the JSON.Serialize method.
    /// </summary>
    public sealed class Options
    {
#pragma warning disable 1591
        // Start OptionsGeneration.linq generated content
        public static readonly Options Default = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch);
        public static readonly Options PrettyPrint = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true);
        public static readonly Options ExcludeNulls = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, excludeNulls: true);
        public static readonly Options JSONP = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, jsonp: true);
        public static readonly Options IncludeInherited = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, includeInherited: true);
        public static readonly Options Utc = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options CamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options PrettyPrintExcludeNulls = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true);
        public static readonly Options PrettyPrintJSONP = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, jsonp: true);
        public static readonly Options PrettyPrintIncludeInherited = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, includeInherited: true);
        public static readonly Options PrettyPrintUtc = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options PrettyPrintCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ExcludeNullsJSONP = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, excludeNulls: true, jsonp: true);
        public static readonly Options ExcludeNullsIncludeInherited = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, excludeNulls: true, includeInherited: true);
        public static readonly Options ExcludeNullsUtc = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, excludeNulls: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options ExcludeNullsCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, excludeNulls: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options JSONPIncludeInherited = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, jsonp: true, includeInherited: true);
        public static readonly Options JSONPUtc = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options JSONPCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, jsonp: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options IncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options IncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options UtcCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options PrettyPrintExcludeNullsJSONP = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, jsonp: true);
        public static readonly Options PrettyPrintExcludeNullsIncludeInherited = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, includeInherited: true);
        public static readonly Options PrettyPrintExcludeNullsUtc = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options PrettyPrintExcludeNullsCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options PrettyPrintJSONPIncludeInherited = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, jsonp: true, includeInherited: true);
        public static readonly Options PrettyPrintJSONPUtc = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options PrettyPrintJSONPCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, jsonp: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options PrettyPrintIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options PrettyPrintIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options PrettyPrintUtcCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ExcludeNullsJSONPIncludeInherited = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, excludeNulls: true, jsonp: true, includeInherited: true);
        public static readonly Options ExcludeNullsJSONPUtc = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, excludeNulls: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options ExcludeNullsJSONPCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, excludeNulls: true, jsonp: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ExcludeNullsIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, excludeNulls: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options ExcludeNullsIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, excludeNulls: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ExcludeNullsUtcCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, excludeNulls: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options JSONPIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options JSONPIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, jsonp: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options JSONPUtcCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options IncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options PrettyPrintExcludeNullsJSONPIncludeInherited = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, jsonp: true, includeInherited: true);
        public static readonly Options PrettyPrintExcludeNullsJSONPUtc = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options PrettyPrintExcludeNullsJSONPCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, jsonp: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options PrettyPrintExcludeNullsIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options PrettyPrintExcludeNullsIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options PrettyPrintExcludeNullsUtcCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options PrettyPrintJSONPIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options PrettyPrintJSONPIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, jsonp: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options PrettyPrintJSONPUtcCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options PrettyPrintIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ExcludeNullsJSONPIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, excludeNulls: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options ExcludeNullsJSONPIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, excludeNulls: true, jsonp: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ExcludeNullsJSONPUtcCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, excludeNulls: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ExcludeNullsIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, excludeNulls: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options JSONPIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options PrettyPrintExcludeNullsJSONPIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options PrettyPrintExcludeNullsJSONPIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, jsonp: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options PrettyPrintExcludeNullsJSONPUtcCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options PrettyPrintExcludeNullsIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options PrettyPrintJSONPIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ExcludeNullsJSONPIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, excludeNulls: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options PrettyPrintExcludeNullsJSONPIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601 = new Options(dateFormat: DateTimeFormat.ISO8601);
        public static readonly Options ISO8601PrettyPrint = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true);
        public static readonly Options ISO8601ExcludeNulls = new Options(dateFormat: DateTimeFormat.ISO8601, excludeNulls: true);
        public static readonly Options ISO8601JSONP = new Options(dateFormat: DateTimeFormat.ISO8601, jsonp: true);
        public static readonly Options ISO8601IncludeInherited = new Options(dateFormat: DateTimeFormat.ISO8601, includeInherited: true);
        public static readonly Options ISO8601Utc = new Options(dateFormat: DateTimeFormat.ISO8601, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options ISO8601CamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601PrettyPrintExcludeNulls = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, excludeNulls: true);
        public static readonly Options ISO8601PrettyPrintJSONP = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, jsonp: true);
        public static readonly Options ISO8601PrettyPrintIncludeInherited = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, includeInherited: true);
        public static readonly Options ISO8601PrettyPrintUtc = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options ISO8601PrettyPrintCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601ExcludeNullsJSONP = new Options(dateFormat: DateTimeFormat.ISO8601, excludeNulls: true, jsonp: true);
        public static readonly Options ISO8601ExcludeNullsIncludeInherited = new Options(dateFormat: DateTimeFormat.ISO8601, excludeNulls: true, includeInherited: true);
        public static readonly Options ISO8601ExcludeNullsUtc = new Options(dateFormat: DateTimeFormat.ISO8601, excludeNulls: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options ISO8601ExcludeNullsCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, excludeNulls: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601JSONPIncludeInherited = new Options(dateFormat: DateTimeFormat.ISO8601, jsonp: true, includeInherited: true);
        public static readonly Options ISO8601JSONPUtc = new Options(dateFormat: DateTimeFormat.ISO8601, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options ISO8601JSONPCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, jsonp: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601IncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.ISO8601, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options ISO8601IncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601UtcCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601PrettyPrintExcludeNullsJSONP = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, excludeNulls: true, jsonp: true);
        public static readonly Options ISO8601PrettyPrintExcludeNullsIncludeInherited = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, excludeNulls: true, includeInherited: true);
        public static readonly Options ISO8601PrettyPrintExcludeNullsUtc = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, excludeNulls: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options ISO8601PrettyPrintExcludeNullsCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, excludeNulls: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601PrettyPrintJSONPIncludeInherited = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, jsonp: true, includeInherited: true);
        public static readonly Options ISO8601PrettyPrintJSONPUtc = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options ISO8601PrettyPrintJSONPCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, jsonp: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601PrettyPrintIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options ISO8601PrettyPrintIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601PrettyPrintUtcCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601ExcludeNullsJSONPIncludeInherited = new Options(dateFormat: DateTimeFormat.ISO8601, excludeNulls: true, jsonp: true, includeInherited: true);
        public static readonly Options ISO8601ExcludeNullsJSONPUtc = new Options(dateFormat: DateTimeFormat.ISO8601, excludeNulls: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options ISO8601ExcludeNullsJSONPCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, excludeNulls: true, jsonp: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601ExcludeNullsIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.ISO8601, excludeNulls: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options ISO8601ExcludeNullsIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, excludeNulls: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601ExcludeNullsUtcCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, excludeNulls: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601JSONPIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.ISO8601, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options ISO8601JSONPIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, jsonp: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601JSONPUtcCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601IncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601PrettyPrintExcludeNullsJSONPIncludeInherited = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, excludeNulls: true, jsonp: true, includeInherited: true);
        public static readonly Options ISO8601PrettyPrintExcludeNullsJSONPUtc = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, excludeNulls: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options ISO8601PrettyPrintExcludeNullsJSONPCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, excludeNulls: true, jsonp: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601PrettyPrintExcludeNullsIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, excludeNulls: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options ISO8601PrettyPrintExcludeNullsIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, excludeNulls: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601PrettyPrintExcludeNullsUtcCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, excludeNulls: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601PrettyPrintJSONPIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options ISO8601PrettyPrintJSONPIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, jsonp: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601PrettyPrintJSONPUtcCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601PrettyPrintIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601ExcludeNullsJSONPIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.ISO8601, excludeNulls: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options ISO8601ExcludeNullsJSONPIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, excludeNulls: true, jsonp: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601ExcludeNullsJSONPUtcCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, excludeNulls: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601ExcludeNullsIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, excludeNulls: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601JSONPIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601PrettyPrintExcludeNullsJSONPIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, excludeNulls: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options ISO8601PrettyPrintExcludeNullsJSONPIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, excludeNulls: true, jsonp: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601PrettyPrintExcludeNullsJSONPUtcCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, excludeNulls: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601PrettyPrintExcludeNullsIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, excludeNulls: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601PrettyPrintJSONPIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601ExcludeNullsJSONPIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, excludeNulls: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options ISO8601PrettyPrintExcludeNullsJSONPIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.ISO8601, prettyPrint: true, excludeNulls: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpoch = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrint = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true);
        public static readonly Options MillisecondsSinceUnixEpochExcludeNulls = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, excludeNulls: true);
        public static readonly Options MillisecondsSinceUnixEpochJSONP = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, jsonp: true);
        public static readonly Options MillisecondsSinceUnixEpochIncludeInherited = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, includeInherited: true);
        public static readonly Options MillisecondsSinceUnixEpochUtc = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options MillisecondsSinceUnixEpochCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintExcludeNulls = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintJSONP = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, jsonp: true);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintIncludeInherited = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, includeInherited: true);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintUtc = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochExcludeNullsJSONP = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, excludeNulls: true, jsonp: true);
        public static readonly Options MillisecondsSinceUnixEpochExcludeNullsIncludeInherited = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, excludeNulls: true, includeInherited: true);
        public static readonly Options MillisecondsSinceUnixEpochExcludeNullsUtc = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, excludeNulls: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options MillisecondsSinceUnixEpochExcludeNullsCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, excludeNulls: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochJSONPIncludeInherited = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, jsonp: true, includeInherited: true);
        public static readonly Options MillisecondsSinceUnixEpochJSONPUtc = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options MillisecondsSinceUnixEpochJSONPCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, jsonp: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options MillisecondsSinceUnixEpochIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochUtcCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintExcludeNullsJSONP = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, jsonp: true);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintExcludeNullsIncludeInherited = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, includeInherited: true);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintExcludeNullsUtc = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintExcludeNullsCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintJSONPIncludeInherited = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, jsonp: true, includeInherited: true);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintJSONPUtc = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintJSONPCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, jsonp: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintUtcCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochExcludeNullsJSONPIncludeInherited = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, excludeNulls: true, jsonp: true, includeInherited: true);
        public static readonly Options MillisecondsSinceUnixEpochExcludeNullsJSONPUtc = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, excludeNulls: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options MillisecondsSinceUnixEpochExcludeNullsJSONPCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, excludeNulls: true, jsonp: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochExcludeNullsIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, excludeNulls: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options MillisecondsSinceUnixEpochExcludeNullsIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, excludeNulls: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochExcludeNullsUtcCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, excludeNulls: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochJSONPIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options MillisecondsSinceUnixEpochJSONPIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, jsonp: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochJSONPUtcCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintExcludeNullsJSONPIncludeInherited = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, jsonp: true, includeInherited: true);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintExcludeNullsJSONPUtc = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintExcludeNullsJSONPCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, jsonp: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintExcludeNullsIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintExcludeNullsIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintExcludeNullsUtcCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintJSONPIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintJSONPIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, jsonp: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintJSONPUtcCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochExcludeNullsJSONPIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, excludeNulls: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options MillisecondsSinceUnixEpochExcludeNullsJSONPIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, excludeNulls: true, jsonp: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochExcludeNullsJSONPUtcCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, excludeNulls: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochExcludeNullsIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, excludeNulls: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochJSONPIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintExcludeNullsJSONPIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintExcludeNullsJSONPIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, jsonp: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintExcludeNullsJSONPUtcCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintExcludeNullsIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintJSONPIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochExcludeNullsJSONPIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, excludeNulls: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options MillisecondsSinceUnixEpochPrettyPrintExcludeNullsJSONPIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.MillisecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123 = new Options(dateFormat: DateTimeFormat.RFC1123);
        public static readonly Options RFC1123PrettyPrint = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true);
        public static readonly Options RFC1123ExcludeNulls = new Options(dateFormat: DateTimeFormat.RFC1123, excludeNulls: true);
        public static readonly Options RFC1123JSONP = new Options(dateFormat: DateTimeFormat.RFC1123, jsonp: true);
        public static readonly Options RFC1123IncludeInherited = new Options(dateFormat: DateTimeFormat.RFC1123, includeInherited: true);
        public static readonly Options RFC1123Utc = new Options(dateFormat: DateTimeFormat.RFC1123, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options RFC1123CamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123PrettyPrintExcludeNulls = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, excludeNulls: true);
        public static readonly Options RFC1123PrettyPrintJSONP = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, jsonp: true);
        public static readonly Options RFC1123PrettyPrintIncludeInherited = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, includeInherited: true);
        public static readonly Options RFC1123PrettyPrintUtc = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options RFC1123PrettyPrintCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123ExcludeNullsJSONP = new Options(dateFormat: DateTimeFormat.RFC1123, excludeNulls: true, jsonp: true);
        public static readonly Options RFC1123ExcludeNullsIncludeInherited = new Options(dateFormat: DateTimeFormat.RFC1123, excludeNulls: true, includeInherited: true);
        public static readonly Options RFC1123ExcludeNullsUtc = new Options(dateFormat: DateTimeFormat.RFC1123, excludeNulls: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options RFC1123ExcludeNullsCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, excludeNulls: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123JSONPIncludeInherited = new Options(dateFormat: DateTimeFormat.RFC1123, jsonp: true, includeInherited: true);
        public static readonly Options RFC1123JSONPUtc = new Options(dateFormat: DateTimeFormat.RFC1123, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options RFC1123JSONPCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, jsonp: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123IncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.RFC1123, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options RFC1123IncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123UtcCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123PrettyPrintExcludeNullsJSONP = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, excludeNulls: true, jsonp: true);
        public static readonly Options RFC1123PrettyPrintExcludeNullsIncludeInherited = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, excludeNulls: true, includeInherited: true);
        public static readonly Options RFC1123PrettyPrintExcludeNullsUtc = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, excludeNulls: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options RFC1123PrettyPrintExcludeNullsCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, excludeNulls: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123PrettyPrintJSONPIncludeInherited = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, jsonp: true, includeInherited: true);
        public static readonly Options RFC1123PrettyPrintJSONPUtc = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options RFC1123PrettyPrintJSONPCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, jsonp: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123PrettyPrintIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options RFC1123PrettyPrintIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123PrettyPrintUtcCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123ExcludeNullsJSONPIncludeInherited = new Options(dateFormat: DateTimeFormat.RFC1123, excludeNulls: true, jsonp: true, includeInherited: true);
        public static readonly Options RFC1123ExcludeNullsJSONPUtc = new Options(dateFormat: DateTimeFormat.RFC1123, excludeNulls: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options RFC1123ExcludeNullsJSONPCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, excludeNulls: true, jsonp: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123ExcludeNullsIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.RFC1123, excludeNulls: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options RFC1123ExcludeNullsIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, excludeNulls: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123ExcludeNullsUtcCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, excludeNulls: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123JSONPIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.RFC1123, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options RFC1123JSONPIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, jsonp: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123JSONPUtcCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123IncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123PrettyPrintExcludeNullsJSONPIncludeInherited = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, excludeNulls: true, jsonp: true, includeInherited: true);
        public static readonly Options RFC1123PrettyPrintExcludeNullsJSONPUtc = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, excludeNulls: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options RFC1123PrettyPrintExcludeNullsJSONPCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, excludeNulls: true, jsonp: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123PrettyPrintExcludeNullsIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, excludeNulls: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options RFC1123PrettyPrintExcludeNullsIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, excludeNulls: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123PrettyPrintExcludeNullsUtcCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, excludeNulls: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123PrettyPrintJSONPIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options RFC1123PrettyPrintJSONPIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, jsonp: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123PrettyPrintJSONPUtcCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123PrettyPrintIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123ExcludeNullsJSONPIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.RFC1123, excludeNulls: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options RFC1123ExcludeNullsJSONPIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, excludeNulls: true, jsonp: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123ExcludeNullsJSONPUtcCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, excludeNulls: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123ExcludeNullsIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, excludeNulls: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123JSONPIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123PrettyPrintExcludeNullsJSONPIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, excludeNulls: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options RFC1123PrettyPrintExcludeNullsJSONPIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, excludeNulls: true, jsonp: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123PrettyPrintExcludeNullsJSONPUtcCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, excludeNulls: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123PrettyPrintExcludeNullsIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, excludeNulls: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123PrettyPrintJSONPIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123ExcludeNullsJSONPIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, excludeNulls: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options RFC1123PrettyPrintExcludeNullsJSONPIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.RFC1123, prettyPrint: true, excludeNulls: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpoch = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch);
        public static readonly Options SecondsSinceUnixEpochPrettyPrint = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true);
        public static readonly Options SecondsSinceUnixEpochExcludeNulls = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, excludeNulls: true);
        public static readonly Options SecondsSinceUnixEpochJSONP = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, jsonp: true);
        public static readonly Options SecondsSinceUnixEpochIncludeInherited = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, includeInherited: true);
        public static readonly Options SecondsSinceUnixEpochUtc = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options SecondsSinceUnixEpochCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintExcludeNulls = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintJSONP = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, jsonp: true);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintIncludeInherited = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, includeInherited: true);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintUtc = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochExcludeNullsJSONP = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, excludeNulls: true, jsonp: true);
        public static readonly Options SecondsSinceUnixEpochExcludeNullsIncludeInherited = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, excludeNulls: true, includeInherited: true);
        public static readonly Options SecondsSinceUnixEpochExcludeNullsUtc = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, excludeNulls: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options SecondsSinceUnixEpochExcludeNullsCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, excludeNulls: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochJSONPIncludeInherited = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, jsonp: true, includeInherited: true);
        public static readonly Options SecondsSinceUnixEpochJSONPUtc = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options SecondsSinceUnixEpochJSONPCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, jsonp: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options SecondsSinceUnixEpochIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochUtcCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintExcludeNullsJSONP = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, jsonp: true);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintExcludeNullsIncludeInherited = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, includeInherited: true);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintExcludeNullsUtc = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintExcludeNullsCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintJSONPIncludeInherited = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, jsonp: true, includeInherited: true);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintJSONPUtc = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintJSONPCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, jsonp: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintUtcCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochExcludeNullsJSONPIncludeInherited = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, excludeNulls: true, jsonp: true, includeInherited: true);
        public static readonly Options SecondsSinceUnixEpochExcludeNullsJSONPUtc = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, excludeNulls: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options SecondsSinceUnixEpochExcludeNullsJSONPCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, excludeNulls: true, jsonp: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochExcludeNullsIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, excludeNulls: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options SecondsSinceUnixEpochExcludeNullsIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, excludeNulls: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochExcludeNullsUtcCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, excludeNulls: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochJSONPIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options SecondsSinceUnixEpochJSONPIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, jsonp: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochJSONPUtcCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintExcludeNullsJSONPIncludeInherited = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, jsonp: true, includeInherited: true);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintExcludeNullsJSONPUtc = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintExcludeNullsJSONPCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, jsonp: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintExcludeNullsIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintExcludeNullsIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintExcludeNullsUtcCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintJSONPIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintJSONPIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, jsonp: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintJSONPUtcCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochExcludeNullsJSONPIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, excludeNulls: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options SecondsSinceUnixEpochExcludeNullsJSONPIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, excludeNulls: true, jsonp: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochExcludeNullsJSONPUtcCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, excludeNulls: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochExcludeNullsIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, excludeNulls: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochJSONPIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintExcludeNullsJSONPIncludeInheritedUtc = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintExcludeNullsJSONPIncludeInheritedCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, jsonp: true, includeInherited: true, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintExcludeNullsJSONPUtcCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, jsonp: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintExcludeNullsIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintJSONPIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochExcludeNullsJSONPIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, excludeNulls: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        public static readonly Options SecondsSinceUnixEpochPrettyPrintExcludeNullsJSONPIncludeInheritedUtcCamelCase = new Options(dateFormat: DateTimeFormat.SecondsSinceUnixEpoch, prettyPrint: true, excludeNulls: true, jsonp: true, includeInherited: true, unspecifiedDateTimeKindBehavior: UnspecifiedDateTimeKindBehavior.IsUTC, serializationNameFormat: SerializationNameFormat.CamelCase);
        // End OptionsGeneration.linq generated content
#pragma warning restore 1591

        internal bool ShouldPrettyPrint { get; private set; }
        internal bool ShouldExcludeNulls { get; private set; }
        internal DateTimeFormat UseDateTimeFormat { get; private set; }
        internal bool IsJSONP { get; private set; }
        internal bool ShouldIncludeInherited { get; private set; }
        internal UnspecifiedDateTimeKindBehavior UseUnspecifiedDateTimeKindBehavior { get; private set; }
        internal SerializationNameFormat SerializationNameFormat { get; private set; }

        /// <summary>
        /// For compatibility with earlier Jil versions.
        /// 
        /// Configuration for Jil serialization options.
        /// 
        /// Available options:
        ///   prettyPrint - whether or not to include whitespace and newlines for ease of reading
        ///   excludeNulls - whether or not to write object members whose value is null
        ///   jsonp - whether or not the serialized json should be valid for use with JSONP
        ///   dateFormat - the style in which to serialize DateTimes and TimeSpans
        ///   includeInherited - whether or not to serialize members declared by an objects base types
        ///   unspecifiedDateTimeKindBehavior - how to treat DateTime's with a DateTimeKind of Unspecified (Jil converts all DateTimes to Utc for serialization, use DateTimeOffset to preserve time zone information)
        /// </summary>
        public Options(bool prettyPrint, bool excludeNulls, bool jsonp, DateTimeFormat dateFormat, bool includeInherited, UnspecifiedDateTimeKindBehavior unspecifiedDateTimeKindBehavior) :
            this(prettyPrint, excludeNulls, jsonp, dateFormat, includeInherited, unspecifiedDateTimeKindBehavior, SerializationNameFormat.Verbatim)
        { }

        /// <summary>
        /// Configuration for Jil serialization options.
        /// 
        /// Available options:
        ///   prettyPrint - whether or not to include whitespace and newlines for ease of reading
        ///   excludeNulls - whether or not to write object members whose value is null
        ///   jsonp - whether or not the serialized json should be valid for use with JSONP
        ///   dateFormat - the style in which to serialize DateTimes and TimeSpans
        ///   includeInherited - whether or not to serialize members declared by an objects base types
        ///   unspecifiedDateTimeKindBehavior - how to treat DateTime's with a DateTimeKind of Unspecified (Jil converts all DateTimes to Utc for serialization, use DateTimeOffset to preserve time zone information)
        ///   serializationNameFormat - how to serialize names of properties/objects unless specified explicitly by an attribute
        /// </summary>
        public Options(bool prettyPrint = false, bool excludeNulls = false, bool jsonp = false, DateTimeFormat dateFormat = DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch, bool includeInherited = false, UnspecifiedDateTimeKindBehavior unspecifiedDateTimeKindBehavior = UnspecifiedDateTimeKindBehavior.IsLocal, SerializationNameFormat serializationNameFormat = SerializationNameFormat.Verbatim)
        {
            ShouldPrettyPrint = prettyPrint;
            ShouldExcludeNulls = excludeNulls;
            IsJSONP = jsonp;

#pragma warning disable 618
            // upgrade from the obsolete DateTimeFormat enumeration; warning disabled to allow it, but all other references
            //  should be expunged
            if (dateFormat == DateTimeFormat.NewtonsoftStyleMillisecondsSinceUnixEpoch)
            {
                dateFormat = DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch;
            }
#pragma warning restore 618

            UseDateTimeFormat = dateFormat;
            ShouldIncludeInherited = includeInherited;
            UseUnspecifiedDateTimeKindBehavior = unspecifiedDateTimeKindBehavior;
            SerializationNameFormat = serializationNameFormat;
        }

        /// <summary>
        /// Returns a string representation of this Options object.
        /// 
        /// The format of this may change at any time, it is only meant for debugging.
        /// </summary>
        public override string ToString()
        {
            return
                string.Format(
                    "{{ ShouldPrettyPrint = {0}, ShouldExcludeNulls = {1}, UseDateTimeFormat = {2}, IsJSONP = {3}, ShouldIncludeInherited = {4}, UseUnspecifiedDateTimeKindBehavior = {5}, SerializationNameFormat = {6} }}",
                    ShouldPrettyPrint,
                    ShouldExcludeNulls,
                    UseDateTimeFormat,
                    IsJSONP,
                    ShouldIncludeInherited,
                    UseUnspecifiedDateTimeKindBehavior,
                    SerializationNameFormat
                );
        }

        /// <summary>
        /// Returns a code that uniquely identifies this set of Options.
        /// </summary>
        public override int GetHashCode()
        {
            const int isoMask = 0x20;
            const int milliMask = isoMask * 2;
            const int microsoftMask = milliMask * 2;
            const int secondsMask = microsoftMask * 2;
            const int rfc1123Mask = secondsMask * 2;
            const int localMask = rfc1123Mask * 2;
            const int utcMask = localMask * 2;
            const int verbatimMask = utcMask * 2;
            const int camelCaseMask = verbatimMask * 2;

            int dateTimeMask;
            switch (UseDateTimeFormat)
            {
                case DateTimeFormat.ISO8601: dateTimeMask = isoMask; break;
                case DateTimeFormat.MillisecondsSinceUnixEpoch: dateTimeMask = milliMask; break;

                case DateTimeFormat.MicrosoftStyleMillisecondsSinceUnixEpoch: dateTimeMask = microsoftMask; break;

                case DateTimeFormat.SecondsSinceUnixEpoch: dateTimeMask = secondsMask; break;
                case DateTimeFormat.RFC1123: dateTimeMask = rfc1123Mask; break;
                default: throw new Exception("Unexpected DateTimeFormat " + UseDateTimeFormat);
            }

            int unspecifiedMask;
            switch (UseUnspecifiedDateTimeKindBehavior)
            {
                case UnspecifiedDateTimeKindBehavior.IsLocal: unspecifiedMask = localMask; break;
                case UnspecifiedDateTimeKindBehavior.IsUTC: unspecifiedMask = utcMask; break;
                default: throw new Exception("Unexpected UnspecifiedDateTimeKindBehavior " + UseUnspecifiedDateTimeKindBehavior);
            }

            int serializationNameMask;
            switch (SerializationNameFormat)
            {
                case SerializationNameFormat.Verbatim: serializationNameMask = verbatimMask; break;
                case SerializationNameFormat.CamelCase: serializationNameMask = camelCaseMask; break;
                default: throw new Exception("Unexpected SerializationNameFormat " + SerializationNameFormat);
            }

            return
                (ShouldPrettyPrint ? 0x1 : 0x0) |
                (ShouldExcludeNulls ? 0x2 : 0x0) |
                (IsJSONP ? 0x4 : 0x0) |
                (ShouldIncludeInherited ? 0x8 : 0x0) |
                dateTimeMask |
                serializationNameMask |
                unspecifiedMask;
        }

        /// <summary>
        /// Returns true if two Options are equal, and false otherwise
        /// </summary>
        public override bool Equals(object obj)
        {
            var other = obj as Options;
            if (other == null) return false;

            return
                other.UseDateTimeFormat == this.UseDateTimeFormat &&
                other.ShouldPrettyPrint == this.ShouldPrettyPrint &&
                other.ShouldExcludeNulls == this.ShouldExcludeNulls &&
                other.IsJSONP == this.IsJSONP &&
                other.ShouldIncludeInherited == this.ShouldIncludeInherited &&
                other.UseUnspecifiedDateTimeKindBehavior == this.UseUnspecifiedDateTimeKindBehavior &&
                other.SerializationNameFormat == this.SerializationNameFormat;
        }
    }
}