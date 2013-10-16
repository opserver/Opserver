Opserver
-----------
Opserver is a monitoring system by the team at [Stack Exchange](http://stackexchange.com), home of [Stack Overflow](http://stackoverflow.com).  It is a tool for monitoring:  
* servers
* SQL clusters/instances 
* redis 
* elastic search 
* exception logs 
* haproxy
* ... and more as we go   

Known as “status” internally, Opserver provides a fast overall view of all our major systems that also allows drilling in for more detail.  For an idea of the UI, you can see some [screenshots from our Velocity 2013 talk](http://imgur.com/a/dawwf).

Installation
-----------
Installation should be a snap, just build this project as-is and deploy it as an IIS website. 
If monitoring windows servers and using integrated auth sections (e.g. live polling, SQL, exception logs) then using a service account with needed permissions is all you need to do on the auth side.
After that, configure Opserver to monitor your systems, keep reading for how.

Security Configuration
-----------
`/Config/SecuritySettings.config` contains the security settings for the Opserver website itself, there are a few built-in providers already:
* Active Directory ("ad")
* "Everyone's an admin" ("alladmin")
* "View All" (the default)

There is a `SecuritySettings.config.example` as a reference.  You can optionally add networks that can see the main dashboard without any authentication when using any provider.  This is useful for fully automated screens like a TV in an office or data center.

Monitoring Configuration
-----------
The basic configuration implementation is via `.json` files, for which `.json.example` files are included in the `/config` directory of the Opserver project.  These `.example` files are exactly what’s running in the Stack Exchange production environment, minus any passwords or internal-only URLs.  You are also welcome to implement your own settings provider that has a completely different source, for example JSON from MongoDB, or SQL, or…whatever you can come up with.  Settings changes will be hooked up to events but that isn’t complete just yet, since we build every change and Opserver restarts, this isn’t a priority.

We recommend using a service account with the necessary permissions for monitoring, this elimiates any passwords in your configuration files and makes management easier, that's the practice in place at Stack Exchange.

Open Source Projects in Use
---------
[BookSleeve](https://code.google.com/p/booksleeve/) by Marc Gravell  
[Dapper](https://github.com/SamSaffron/dapper-dot-net) by Stack Exchange  
[JSON.Net](http://james.newtonking.com/json) by James Newton-King     
[MiniProfiler](http://miniprofiler.com/) by Stack Exchange  
[NEST](https://github.com/Mpdreamz/NEST) by Martijn Laarman  
[StackExchange.Exceptional](https://github.com/NickCraver/StackExchange.Exceptional) by Nick Craver  
[TeamCitySharp](https://github.com/stack72/TeamCitySharp) by Paul Stack  

JavaScript:  
[d3.js](http://d3js.org/) by Michael Bostock  
[HTML Query Plan](https://code.google.com/p/html-query-plan/) by Justin Pealing  
[isotope](http://isotope.metafizzy.co) by Metafizzy  
[jQuery](http://jquery.com) by The jQuery Foundation  
[jQuery cookie plugin](https://github.com/carhartl/jquery-cookie) by Klaus Hartl  
[jQuery autocomplete](http://bassistance.de/jquery-plugins/jquery-plugin-autocomplete/) by Jörn Zaefferer  
[prettify](https://code.google.com/p/google-code-prettify/) by Google  
[Simple Modal](http://simplemodal.com/) by Eric Martin  
[TableSorter](http://tablesorter.com) by Christian Bach  
[Toastr](https://github.com/CodeSeven/toastr) by John Papa and Hans Fjällemark  

License
----------
Opserver is licensed under the [MIT License](http://opensource.org/licenses/MIT).
