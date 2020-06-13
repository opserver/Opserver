## Opserver
[![Build status](https://ci.appveyor.com/api/projects/status/7m0b1e4orimk5nvr/branch/master?svg=true)](https://ci.appveyor.com/project/StackExchange/opserver/branch/master)
[![Open Source Helpers](https://www.codetriage.com/opserver/opserver/badges/users.svg)](https://www.codetriage.com/opserver/opserver)

Opserver is a monitoring system originally from the team at [Stack Exchange](https://stackexchange.com), home of [Stack Overflow](https://stackoverflow.com).  It is a tool for monitoring:  
* Servers/Switches & anything supported by Bosun, Orion, or direct WMI monitoring
* SQL Clusters & Single Instances 
* Redis 
* Elasticsearch 
* Exception Logs (from StackExchange.Exceptional) 
* HAProxy
* PagerDuty
* CloudFlare DNS
* ... and more as we go   

Known as “status” internally, Opserver provides a fast overall view of all our major systems that also allows drilling in for more detail.  For an idea of the UI, you can see some [screenshots from our Velocity 2013 talk](https://imgur.com/a/dawwf).

### Building
Building Opserver (unless using Docker below) requires the .NET Core 3.1 SDK or higher ([downloaded here](https://dotnet.microsoft.com/download)). Once that's in place, in the repo root, run:
```bash
dotnet build -c Release
```
Or just run the app directly if you like (for debugging, etc.):
```bash
cd /src/Opserver.Web
dotnet run -c Release
```
Note: you'll want to configure it - see below!

### Configuration
Configuring Opserver is per-module. For details on each section, see [the configuration doc](docs/Configuration.md)!

### Running
Running Opserver has a few options thanks to .NET Core, you can:
- `dotnet run` as deploy the service as raw Kestrel listening on a port.
- Use in-process behind IIS.
- Use Docker.
  - When the v2 release is final - I'll publish an image to the Opserver Docker Org.
  - To build locally for now:
```
docker build --target web -t opserver-ci .
docker run --rm -p 4000:80 -v <localConfigPath>:/app/Config/ opserver-ci
```

### Open Source Projects in Use
[StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) by Marc Gravell  
[Dapper](https://github.com/StackExchange/Dapper/) by Stack Exchange  
[JSON.Net](https://www.newtonsoft.com/json) by James Newton-King     
[MiniProfiler](https://miniprofiler.com/dotnet) by Stack Exchange    
[StackExchange.Exceptional](https://github.com/NickCraver/StackExchange.Exceptional) by Nick Craver  

JavaScript:  
[d3.js](https://d3js.org/) by Michael Bostock  
[ColorBrewer](http://colorbrewer2.org/) by Cynthia Brewer and Mark Harrower  
[HTML Query Plan](https://github.com/JustinPealing/html-query-plan) by Justin Pealing  
[isotope](https://isotope.metafizzy.co/) by Metafizzy  
[jQuery](https://jquery.com/) by The jQuery Foundation  
[jQuery cookie plugin](https://github.com/js-cookie/js-cookie) by Klaus Hartl  
[jQuery autocomplete](http://bassistance.de/jquery-plugins/jquery-plugin-autocomplete/) by Jörn Zaefferer  
[prettify](https://github.com/google/code-prettify) by Google  
[TableSorter](https://github.com/christianbach/tablesorter) by Christian Bach  
[Toastr](https://github.com/CodeSeven/toastr) by John Papa and Hans Fjällemark  

### License
Opserver is licensed under the [MIT License](https://opensource.org/licenses/MIT).

### Props
We'd like to thank several people outside Stack Exchange for large contributions to Opserver's development.

* [Brent Ozar](https://www.brentozar.com/) for lots of (continuing) help on the SQL monitoring, with some of the initial queries and help letting us know many use cases to cover.  We'll be integrating some of the awesome tooling Brent and his team have to further assist DBAs and developers (like [sp_Blitz](https://www.brentozar.com/blitz/), [sp_BlitzIndex](https://www.brentozar.com/blitzindex/), and some you'll hear about soon).  
* [Adam Machanic](http://sqlblog.com/blogs/adam_machanic/) for [sp_WhoIsActive](http://whoisactive.com/), which powers the active tab for a SQL Instance.  He also assisted with use cases and performance tuning on the use of sp_WhoIsActive.  
* The [Sentry One](https://www.sentryone.com/) guys: [Aaron Bertrand](http://sqlblog.com/blogs/aaron_bertrand/) and [Kevin Kline](http://kevinekline.com/) for even more SQL use cases, and their help with upcoming integration with SQL Sentry.
