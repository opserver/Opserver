---
layout: "default"
---
### Opserver Monitoring Dashboard

Opserver is a monitoring system by the team at [Stack Exchange](https://stackexchange.com), home of [Stack Overflow](https://stackoverflow.com).
It independently monitors several systems as well as supports pulling data for an "all servers" view with respect to CPU, Memory, Network, and hardware stats. 
Currently, Opserver can monitor:

* Servers/Switches & anything supported by Bosun, Orion, SignalFX, or direct WMI monitoring
* SQL Server Clusters & Single Instances 
* [Redis](https://redis.io/)
* [Elasticsearch](https://www.elastic.co/elasticsearch/)
* Exception Logs (from [StackExchange.Exceptional](https://github.com/NickCraver/StackExchange.Exceptional)) 
* [HAProxy](https://www.haproxy.org/)
* [PagerDuty](https://www.pagerduty.com/)
* [CloudFlare](https://www.cloudflare.com/) DNS
* ... and more as we go   

It can run under:
- Windows
- macOS
- Linux
- ...and probably any platform [supported by .NET Core](https://docs.microsoft.com/en-us/dotnet/core/introduction)

#### Building Opserver

To build, you'll need the .NET Core 3.1 or higher SDK ([available here](https://dotnet.microsoft.com/download)), or a current version of Visual Studio.

Build instructions are:
1. Clone the repo.
2. `dotnet build`

The goal is to also publish to docker hub from GitHub actions so that you can spin up and image and simply provide your config.

#### Running Opsever

You may build to a directory under IIS with the ASP.NET Core hosting module if wanting to host in the V1 model,
or you can use it as a raw executable (using "Kestrel", the ASP.NET Core web server).

For example, if you just want to clone this repo and run it from the command line:
```
dotnet run --project src/Opserver.Web
```

Full instructions on [building and running can be found here](HowTo/BuildAndRun).

Note: you'll need to [setup your configuration](Configuration) to do anything useful.


### Open Source Projects in Use
[StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) by Marc Gravell  
[Dapper](https://github.com/StackExchange/Dapper/) by Stack Exchange  
[JSON.Net](https://www.newtonsoft.com/json) by James Newton-King     
[MiniProfiler](https://miniprofiler.com/) by Stack Exchange    
[StackExchange.Exceptional](https://github.com/NickCraver/StackExchange.Exceptional) by Nick Craver  

JavaScript:  
[d3.js](https://d3js.org/) by Michael Bostock  
[ColorBrewer](http://colorbrewer2.org/) by Cynthia Brewer and Mark Harrower  
[HTML Query Plan](https://github.com/JustinPealing/html-query-plan) by Justin Pealing  
[isotope](https://isotope.metafizzy.co/) by Metafizzy  
[jQuery](https://jquery.com/) by The jQuery Foundation  
[jQuery cookie plugin](https://github.com/js-cookie/js-cookie) by Klaus Hartl  
[jQuery autocomplete](http://bassistance.de/jquery-plugins/jquery-plugin-autocomplete/) by J�rn Zaefferer  
[prettify](https://github.com/google/code-prettify) by Google  
[TableSorter](http://tablesorter.com) by Christian Bach  
[Toastr](https://github.com/CodeSeven/toastr) by John Papa and Hans Fj�llemark  

### License
Opserver is licensed under the [MIT License](https://opensource.org/licenses/MIT).

### Props
We'd like to thank several people outside Stack Exchange for large contributions to Opserver's development.

* [Brent Ozar](https://www.brentozar.com/) for lots of (continuing) help on the SQL monitoring, with some of the initial queries and help letting us know many use cases to cover.  We'll be integrating some of the awesome tooling Brent and his team have to further assist DBAs and developers (like [sp_Blitz](https://www.brentozar.com/blitz/), [sp_BlitzIndex](https://www.brentozar.com/blitzindex/), and some you'll hear about soon).  
* [Adam Machanic](http://sqlblog.com/blogs/adam_machanic/) for [sp_WhoIsActive](http://whoisactive.com/), which powers the active tab for a SQL Instance.  He also assisted with use cases and performance tuning on the use of sp_WhoIsActive.  
* The [Sentry One](https://www.sentryone.com/) guys: [Aaron Bertrand](http://sqlblog.com/blogs/aaron_bertrand/) and [Kevin Kline](http://kevinekline.com/) for even more SQL use cases.