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

If you are using Active Directory authentication, you should edit the ViewGroups and AdminGroups in the Web.Config. You can also edit the ViewGroups and AdminGroups on a per monitor basis by adding `"AdminGroups": "GroupName",` or `"ViewGRoups": "GroupName",` to the json config file.

One cause of the 'No Configuration' message being displayed is if you do not have any permissions to any of your configured monitors. You can see what you were authenticated as, and what roles you were granted by browsing to /about. 

Monitoring Configuration
-----------
The basic configuration implementation is via `.json` files, for which `.json.example` files are included in the `/config` directory of the Opserver project.  These `.example` files are exactly what’s running in the Stack Exchange production environment, minus any passwords or internal-only URLs.  You are also welcome to implement your own settings provider that has a completely different source, for example JSON from MongoDB, or SQL, or…whatever you can come up with.  Settings changes will be hooked up to events but that isn’t complete just yet, since we build every change and Opserver restarts, this isn’t a priority.

We recommend using a service account with the necessary permissions for monitoring, this eliminates any passwords in your configuration files and makes management easier, that's the practice in place at Stack Exchange.

Even if you have correctly configured your monitors, you still may not see any data. Each monitor configuration has an enabled flag which must return true for a monitor section to appear in OpServer. 

You can browse to /about to review which monitors have been enabled. 

Jira Actions Configuration
-----------
You can use Jira to create issues using the links rendered in the exception details page. 

In order to use Jira 

* You have to enable Exception monitoring 
* Add JiraSettings.json file under `/config` folder (JiraSettings.json.example file is included)
* Enable REST api on Jira

[Jira action screenshots](http://imgur.com/a/XtW0j)

PagerDuty Configuration
------------
You can connect OpServer to your pagerduty istallation.

You need a PagerDuty ReadWrite API Key (RO will work for viewing but will throw errors when you do a RW action). 

You need to set the following options in PagerDutySettings.json to get a minimally working setup: 

  * APIKey: Your Pager Duty API Key
  * APIBaseURL: https://<your_domain>.pagerduty.com/api/v1

There is a username map option for when your email address does not match your OpServer login credentials. 

For example George has an email of george@example.com, and a login of gsock. The plugin needs to be told how to map the email on the pagerduty side to the username on the opserver side. To setup a map to allow George to be discovered and associated correctly, you would do the following: 

    "UserNameMap": [
            {"OpServerName": "gsock", "EmailUser": "george"},
        ],

The UserNameMap is an array of OpserverName (login Name) and EmailUser (user part of email associated to PagerDuty Account). You can have as many User Name Mappings as you need.

There is a HeaderHTML tag that lets you put arbitrary html above all content. You can use this to place notices, or messages for people who do not view the page all the time at the top. We use it to explain how to contact the on call Engineer. 



Open Source Projects in Use
---------
[StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) by Marc Gravell  
[Dapper](https://github.com/StackExchange/dapper-dot-net/) by Stack Exchange  
[JSON.Net](http://james.newtonking.com/json) by James Newton-King     
[MiniProfiler](http://miniprofiler.com/) by Stack Exchange  
[NEST](https://github.com/Mpdreamz/NEST) by Martijn Laarman  
[StackExchange.Exceptional](https://github.com/NickCraver/StackExchange.Exceptional) by Nick Craver  
[TeamCitySharp](https://github.com/stack72/TeamCitySharp) by Paul Stack  

JavaScript:  
[d3.js](http://d3js.org/) by Michael Bostock  
[ColorBrewer](http://colorbrewer2.org/) by Cynthia Brewer and Mark Harrower  
[HTML Query Plan](https://code.google.com/p/html-query-plan/) by Justin Pealing  
[isotope](http://isotope.metafizzy.co) by Metafizzy  
[jQuery](http://jquery.com) by The jQuery Foundation  
[jQuery cookie plugin](https://github.com/js-cookie/js-cookie) by Klaus Hartl  
[jQuery autocomplete](http://bassistance.de/jquery-plugins/jquery-plugin-autocomplete/) by Jörn Zaefferer  
[prettify](https://code.google.com/p/google-code-prettify/) by Google  
[Simple Modal](http://simplemodal.com/) by Eric Martin  
[TableSorter](http://tablesorter.com) by Christian Bach  
[Toastr](https://github.com/CodeSeven/toastr) by John Papa and Hans Fjällemark  

License
----------
Opserver is licensed under the [MIT License](http://opensource.org/licenses/MIT).

Props
----------
We'd like to thank several people outside Stack Exchange for large contributions to Opserver's development.

* [Brent Ozar](http://www.brentozar.com/) for lots of (continuing) help on the SQL monitoring, with some of the initial queries and help letting us know many use cases to cover.  We'll be integrating some of the awesome tooling Brent and his team have to further assist DBAs and developers (like [sp_Blitz](http://www.brentozar.com/blitz/), [sp_BlitzIndex](http://www.brentozar.com/blitzindex/), and some you'll hear about soon).  
* [Adam Machanic](http://sqlblog.com/blogs/adam_machanic/) for [sp_WhoIsActive](http://sqlblog.com/blogs/adam_machanic/archive/tags/who+is+active/default.aspx), which powers the active tab for a SQL Instance.  He also assisted with use cases and performance tuning on the use of sp_WhoIsActive.  
* The [SQL Sentry](http://www.sqlsentry.com/) guys, [Aaron Bertrand](http://sqlblog.com/blogs/aaron_bertrand/) and [Kevin Kline](http://kevinekline.com/) for even more SQL use cases, and their help with upcoming integration with SQL Sentry.  If you have SQL Sentry, Opserver will be providing historical data and some more dashboards using the data you already have from SQL Sentry, without increasing load to do the same monitoring twice.
