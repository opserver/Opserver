
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "RCS1194:Implement exception constructors.", Justification = "Meh", Scope = "type", Target = "~T:StackExchange.Opserver.Data.IPNet.IPNetParseException")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "RCS1194:Implement exception constructors.", Justification = "Meh", Scope = "type", Target = "~T:StackExchange.Opserver.Data.DataPullException")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "RCS1158:Static member in generic type should use a type parameter.", Justification = "Intentional", Scope = "member", Target = "~P:StackExchange.Opserver.Data.SinglePollNode`1.ShortName")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Matching SQL fields", Scope = "type", Target = "~T:StackExchange.Opserver.Data.SQL.SQLInstance.WhoIsActiveRow")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "RCS1221:Use pattern matching instead of combination of 'as' operator and null check.", Justification = "<Pending>", Scope = "member", Target = "~M:StackExchange.Opserver.SettingsProviders.SettingsProvider.GetCurrentProvider~StackExchange.Opserver.SettingsProviders.SettingsProvider")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "<Pending>", Scope = "member", Target = "~M:StackExchange.Opserver.Data.Dashboard.Providers.WmiDataProvider.WmiNode.GetAllInterfacesAsync~System.Threading.Tasks.Task")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "<Pending>", Scope = "member", Target = "~M:StackExchange.Opserver.SettingsProviders.SettingsProvider.GetCurrentProvider~StackExchange.Opserver.SettingsProviders.SettingsProvider")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Readability", "RCS1154:Sort enum members.", Justification = "<Pending>", Scope = "type", Target = "~T:StackExchange.Opserver.Data.HAProxy.ProxyServerStatus")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "RCS1146:Use conditional access.", Justification = "<Pending>", Scope = "member", Target = "~M:StackExchange.Opserver.Data.Dashboard.DashboardModule.#cctor")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "RCS1146:Use conditional access.", Justification = "<Pending>", Scope = "member", Target = "~M:StackExchange.Opserver.Data.Redis.RedisHost.GetDownstreamHosts(System.Collections.Generic.List{StackExchange.Opserver.Data.Redis.RedisInstance})~System.Collections.Generic.List{StackExchange.Opserver.Data.Redis.RedisHost.ReplicaSummary}")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "RCS1146:Use conditional access.", Justification = "<Pending>", Scope = "member", Target = "~M:StackExchange.Opserver.Data.PollingEngine.TryRemove(StackExchange.Opserver.Data.PollNode)~System.Boolean")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "RCS1146:Use conditional access.", Justification = "<Pending>", Scope = "member", Target = "~M:StackExchange.Opserver.Helpers.FileSizeFormatProvider.Format(System.String,System.Object,System.IFormatProvider)~System.String")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "RCS1146:Use conditional access.", Justification = "<Pending>", Scope = "member", Target = "~M:StackExchange.Opserver.Data.Dashboard.Providers.BosunMetric.GetDenormalized(System.String,System.String,System.Collections.Generic.Dictionary{System.String,System.Collections.Generic.List{System.String}})~System.String")]

