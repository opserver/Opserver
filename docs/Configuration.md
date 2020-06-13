---
title: "Configuration"
layout: "default"
---
# Opserver Configuration

## Basics
Configuration is accepted in several paths.
1. All modules are optional. Just configure what you intend to montior and ignore the rest.
2. The following paths are searched:
   - `/appSettings.json`
   - `/localSettings.json`
   - `/opserverSettings.json` (note: ignored in `.gitignore` for local testing)
   - `/Config/opserverSettings.json`
3. For compatibility with Opserver v1 and general flexibility, the app will also look in the `/Config` directory under the web for separate module settings in the `<Module>Settngs.json` format, for example: `HAProxySettings.json`.
  - This is true except for the SecuritySettings.config - it is not expected to be in a json format (see below).
4. All JSON keys are case-insensitive because no one's wants to spend their life chasing down nonsense.

The structure of the main config is:
```json
{
  "Kestrel": {}, // (Optional) Kestrel-specific settings e.g. ports, certificates, etc.
  "Security": {}, // (Required) Global security settings for Opserver
  "Modules": { // (All Optional) Per-module settings, only include what you want to monitor
    "CloudFlare": {}, // The example below would be in place of the empty object here.
    "Dashboard": {},
    "Elastic": {},
    "Exceptions": {},
    "HAProxy": {},
    "PagerDuty": {},
    "Redis": {},
    "Sql": {}
  }
}
```

...or, for compat, you can use the old v1 structure for everything except `SecuritySettings.config` (port it to JSON - see below). It'd look like this:
```
Config/
  CloudFlareSettings.json
  RedisSettings.json
  SQLSettings.json
```

- The Kestrel (.NET Core's web server) settings [are documented here](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-3.1#kestrel-options).
- Security settings have their own section below.
- Each module also has it's own configuration (in alphabetical order below).

Or if you're like me, you would just like to see a big comprehensive example of all possible options. 
You got it.
[Here's an example configuration monitoring lots of things with comments](src/Opserver.Web/Config/opserverSettings.example.json)

### Overall Notes
We recommend using a service account with the necessary permissions for monitoring, this eliminates any passwords in your configuration files and makes management easier, that's the practice in place at Stack Exchange.

Even if you have correctly configured your monitors, you still may not see any data if you do not have access via the security provider. You can browse to `/about` to review which monitors have been enabled. 

### Security Configuration
The `Security` section of the config determines the security of Opserver itself, built-in providers:
* Active Directory (`"ActiveDirectory"`)
* "Everyone's an admin" (`"EveryonesAnAdmin"`)
* "View All" (`"EveryonesReadOnly"`)

Here's a full example for Active Directory:
```json
{
    "provider": "ActiveDirectory",
    "server": "<Domain Controller DNS or IP>", // (Optional) If the web server is on the domain, leave this off
    "authUser": "<AD Bind Username>", // (Optional) If the process is running ith bindable credentials, leave this off
    "authPassword": "<AD Bind Password>", // (Optional) If the process is running ith bindable credentials, leave this off
    "apiKey": "<Global API Key>",
    "internalNetwork": [
        { "name": "My Internal", "cidr": "10.0.0.0/8" }
    ],
    "viewEverythingGroups": "Opserver-View",
    "adminEverythingGroups": "Opserver-Admins;Opserver-MoreAdmins"
}
```
If you just trust everyone because that's the kind of person you are and you've never been burned by love hey, who am I to judge? We've got a config for that:
```json
{
    "provider": "EveryonesAnAdmin"
}
```
But if that scar from your backstabbing co-admin in '08 still lingers, we can do read-only to play it safe:
```json
{
    "provider": "EveryonesReadOnly"
}
```

You can optionally add networks that can see the main dashboard without any authentication when using any provider.
This is useful for fully automated screens like a TV in an office or data center.

If you are using Active Directory authentication, you should edit the ViewGroups and AdminGroups. You can also edit the ViewGroups and AdminGroups on a per monitor basis by adding `"AdminGroups": "GroupName",` or `"ViewGRoups": "GroupName",` to the json config file.

- Each module has it's own settings but there are a few global properties in each related to security:
  - `"ViewGroups"` - the groups (from your security provider, e.g. an AD group if it's AD, or `*` for everyone) that can view the module.
  - `"AdminGroups"` - the groups that can admin the module (again `*` for everyone).
  - Both of the above are semicolon-delimited.

One cause of the 'No Configuration' message being displayed is if you do not have any permissions to any of your configured monitors.
You can see what you were authenticated as, and what roles you were granted by browsing to `/about`. 

### Module settings

#### CloudFlare Settings
```json
{
  "email": "<Email>", // The login email for the Cloudflare API
  "apiKey": "<API key>", // The API key for the Cloudflare API
  // (Optional) The origin data centers for UI labelling
  "datacenters": [
    // name: The name of the data center to use in the UI
    // ranges: The outside ranges of *your* network, these are matches against CloudFlare's A records,
    //         to indicate where traffic is pointed to in the dashboard.
    // maskedRanges: The same function as ranges, but with the first 3 octets masked in the dashboard.
    //               This allows people to use the dashboard without exposing sensitive IPs for instance.
    {
      "name": "New York",
      "ranges": [ "10.2.1.0/24" ],
      "maskedRanges": [ "12.34.56.0/24" ]
    },
    {
      "name": "Denver",
      "ranges": [ "10.6.1.0/24" ],
      "maskedRanges": [ "12.34.57.0/24" ]
    }
  ]
}
```

#### Dashboard Settings
The dashboard can currently monitor via Bosun, Orion or limited amounts through direct WMI.
```json
/* Configuration for the main dashboard */
{
  /* Which provider to use for dashboard data 
     Multiple providers can be used...but things may get crazy when a node is monitored by more than 1 */
  "providers": {
    /* If using SignalFX */
    "signalfx": {
      "realm": "<realm>", // Realm for SignalFx, e.g. "us1", "eu0"
      "accessToken": "<Access Token>"
    }
    /* If using bosun, an API and a key (in recent bosun versions) needs to be provided */
    // "bosun": {
    //   "host": "https://bosun.mydomain.com",
    //   "apiKey": "<API Key>"
    // }
    /* If using WMI, a list of nodes to monitor needs to be provided */
    //"wmi": {
    //  "nodes": [ "ny-web01" ], // List of nodes to monitor
    //  "staticDataTimeoutSeconds": 300, // (Optional) How long to cache static data (node name, hardware, etc.) - defaults to 5 minutes
    //  "dynamicDataTimeoutSeconds": 5, // (Optional) How long to cache dynamic data (utilizations, etc.) - defaults to 30 seconds
    //  "historyHours": 2 // (Optional) How long to retain data (in memory) - defaults to 24 hours
    //}
    /* If using Orion, a host (for links, not API) and a connection string needs to be provided */
    //"orion": {
    //  "host": "orion.mydomain.com",
    //  "connectionString": "Data Source=ny-orionsql01;Initial Catalog=SolarWindsOrion;Integrated Security=SSPI;Timeout=10"
    //}
  },
  /* General dashboard UI settings */
  "excludePattern": "redis|\\.com", // (Optional) Regex node name pattern to exclude from the dashboard
  "cpuWarningPercent": 50, // (Optional) How much CPU usage before a node is treated as a warning
  "cpuCriticalPercent": 60, // (Optional) How much CPU usage before a node is treated as critical
  "memoryWarningPercent": 90, // (Optional) How much memory usage before a node is treated as a warning
  "memoryCriticalPercent": 95, // (Optional) How much memory usage before a node is treated as critical
  "diskWarningPercent": 85, // (Optional) How much disk usage before a node is treated as a warning
  "diskCriticalPercent": 95, // (Optional) How much disk usage before a node is treated as critical
  // "showVolumePerformance": true, // (Optional - default: false) Whether to show volume performance (columns) on the dashboard
  /* (Optional) Specific category settings,for grouping servers and setting specific thresholds on them */
  "categories": [
    {
      "name": "Database Servers", // Name for this group of servers
      "pattern": "-sql", // Regex pattern of server names to put in this group
      "cpuWarningPercent": 20, // How much CPU usage before a node is treated as a warning (defaults to the setting above if not specified)
      "cpuCriticalPercent": 60, // How much CPU usage before a node is treated as critical (defaults to the setting above if not specified)
      "memoryWarningPercent": 98, // How much memory usage before a node is treated as a warning (defaults to the setting above if not specified)
      "memoryCriticalPercent": 99.2, // How much memory usage before a node is treated as critical (defaults to the setting above if not specified)
      "primaryInterfacePattern": "-TEAM$" // (If the provider supports it) Regex pattern of interface names to treat as "primary" (shown in the dashboard aggregates)
    },
    {
      "name": "Web Servers",
      "pattern": "-web|-promoweb|-vmweb",
      "cpuWarningPercent": 25,
      "memoryWarningPercent": 75,
      "primaryInterfacePattern": "-TEAM$|-TEAM · Local"
    }
  ],
  /* Like categories, per-node overrides for any of the settings above - illustrating 1 setting but all work */
  "perNodeSettings": [
    {
      "pattern": "EDGE\\d+ \\(INAP\\)", // Regex pattern to match against
      "primaryInterfacePattern": "GigabitEthernet0/0/0" // Example setting - any of the above (e.g. warning/critical thresholds) work
    }
  ]
}
```

#### Elastic Settings
In elastic, it's not a good idea to trust any specific node to be up. It's suggested to either add the entire cluster, or point to a master VIP-style shared address that is always up.
```json
{
  "clusters": [ // The clusters to monitor
    {
      "name": "NY Production", // Name of the cluster (used in the UI before fetching of info is complete)
      "refreshIntervalSeconds": 10, // (Optional - default: 120) How often to poll the cluster
      // Nodes in this cluster, use "node:port" if not using :9200 for elastic
      "nodes": [
        "ny-search01",
        "ny-search02",
        "ny-search03"
      ]
    },
    {
      "name": "NY Development",
      "refreshIntervalSeconds": 20,
      "nodes": [
        "ny-devsearch01",
        "ny-devsearch02"
      ]
    }
  ]
}
```

#### Exception Settings
Exceptions from a [StackExchange.Exceptional](https://nickcraver.com/StackExchange.Exceptional/) store are visible in Opserver. Applications can be grouped by teams, and multiple stores can be added: for example multiple data centers or prod, dev, etc. Each store will appear as a tab in the UI.
```json
{
  //"recentSeconds": 600,// (Optional) How many seconds an error is considered recent (for the next 2 settings below) - defaults to 600
  "warningRecentCount": 100, // (Optional) How many recent errors before the header turns to a warning color
  "criticalRecentCount": 200, // (Optional) How many recent errors before the header turns to a critical color
  // "warningCount": null, // (Optional) How many errors (regardless of recency) before the header turns to a warning color
  // "criticalCount": null, // (Optional) How many errors (regardless of recency) before the header turns to a critical color
  // "enablePreviews": true, // (Optional) Whether to enable hover previews for exceptions - defaults to true
  //"perAppCacheCount": 5000, // (Optional) How many errors to cache and display per exceptional application name - defaults to 5000

  /* (Optional) A list of un-grouped applications to display - for simple or few-application scenarios */
  //"applications": [
  //  "Core",
  //  "Chat",
  //  "StackExchange.com"
  //],
  /* Or, you can use a grouped approach. This creates grouped dropdowns for larger scenarios
     For instance you may want to group by application type, or the team that handles it
     An application can appear under multiple groups, just list it on both.
  */
  "groups": [
    {
      "name": "Core Q&A", // Group name in the UI
      "applications": [ // Exceptional applications to include in this group
        "Core",
        "Chat",
        "Stack Auth",
        "StackExchange.com",
      ]
    },
    {
      "name": "Careers",
      "applications": [
        "Careers",
        "BackOffice",
        "BackOffice",
        "Control Panel",
        "StackShop",
        "CareersAuth"
      ]
    }
  ],
  /* The database connection to the exception store(s) */
  "stores": [
    {
      "name": "New York", // Name of the exception store (mainly for debugging)
      "queryTimeoutMs": 2000, // (Optional - default: 30000) The query timeout before giving up on this store (when shit hits the fan...maybe a store isn't available)
      "pollIntervalSeconds": 30, // (Optional - default: 300) How often to poll this store for new/changed exceptions
      // SQL Server connection string to the Exceptional store
      "connectionString": "Server=ny-sql01;Database=NY.Exceptions;Integrated Security=SSPI;"
    }
  ],
  /* (Optional) Replacements for Stack Trace descriptions. It's general purpose with specific uses in mind.
     Example:
       If using SourceLink, instead of a file path we'll get a URL for the pieces in the stack.
       This replaces those URLs from the raw content wo the usable file browser (at the correct commit).
       Essentially: you get a one-click path to see the source for that line of code. Awesome, eh?
  */
  "stackTraceReplacements": [
    {
      "name": "github",
      "pattern": "(?<= in )https?://raw.githubusercontent.com/([^/]+/)([^/]+/)([^/]+/)(.*?):line (\\d+)",
      "replacement": "<a href=\"https://github.com/$1$2blob/$3$4#L$5\">$4:line $5</a>"
    }
  ]
}
```

#### HAProxy Settings
Opserver can monitor backends in HAProxy and optionally (if given admin credentials) control HAPRoxy by controlling backends and entire servers.
```json
{
  "queryTimeout": 3000, // (Optional - default 60,000) The amount of time (in milliseconds) before timing out
  "user": "<View Username>", // The read-only username to use by default for all instances below
  "password": "<View  Password>", // The read-only password to use by default for all instance below
  // (Optional): If no admin credentials are provided, admin features simply won't be enabled
  "adminUser": "<Admin Username>", // (Optional) The admin username to use by default for all instances below
  "adminPassword": "<Admin Password>", // (Optional) The admin password to use by default for all instance below
  /* HAProxy groups of servers, where they are active/active, active/passing, or some mirrored config
     These are displayed together in the UI and can be managed as a unit */
  "groups": [
    {
      "name": "NY T1: Primary", // Name to display in the UI
      "description": "Primary", // Tooltip description for the UI
      "instances": [ // Instances in this group
        {
          "name": "LB05", // Name to display in the UI (column name, should be short)
          "url": "http://10.0.4.10:7001/haproxy" // URL to hit the statistics API
        },
        {
          "name": "LB06",
          "url": "http://110.0.4.11:7001/haproxy"
        }
      ]
    },
    {
      "name": "NY T2: Promo/Support",
      "description": "Promotions",
      "instances": [
        {
          "name": "LB05",
          "url": "http://10.0.4.10:7002/haproxy"
        },
        {
          "name": "LB06",
          "url": "http://10.0.4.11:7002/haproxy"
        }
      ]
    }
  ],
  /* (Optional) A list of aliases for frontend and backend names
     The key is what backend name exists in HAProxy
     The value is what friendly name you want displayed in the Opserver UI */
  "aliases": {
    "be_so": "Stack Overflow",
    "be_others": "Others - SE 2.0",
    "be_meta_so": "Meta Stack Overflow",
    "be_area51_stackexchange_com": "Area 51 & StackExchange.com",
    "be_sstatic": "sstatic.net",
    "be_stackauth": "Stack Auth",
    "be_so_crawler": "Stack Overflow - Crawlers",
    "be_careers": "Careers",
    "be_openid": "OpenID - StackID",
    "be_internal_api": "Internal API",
    "be_api_1.1": "API v1.1",
    "be_api": "API v2.0",
  }
}
```

### PagerDuty Configuration
You can connect OpServer to your PagerDuty installation.
You need a PagerDuty ReadWrite API Key (RO will work for viewing but will throw errors when you do a RW action). 
You need to set the following options in PagerDutySettings.json to get a minimally working setup: 

  * APIKey: Your Pager Duty API Key
  * APIBaseURL: https://<your_domain>.pagerduty.com/api/v1

The UserNameMap is an array of OpserverName (login Name) and EmailUser (user part of email associated to PagerDuty Account). You can have as many User Name Mappings as you need.

There is also a username map option for when your email address does not match your OpServer login credentials. 

For example George has an email of george@example.com, and a login of gsock. The plugin needs to be told how to map the email on the pagerduty side to the username on the opserver side. To setup a map to allow George to be discovered and associated correctly, you would do the following: 
```json
{
  // (Required) PagerDuty API key
  "APIKey": "<API Key>",
  "onCallToShow": 3, // (Optional - default: 2) The number of users to show per schedule in the dashboard (top N by escalation)
  "daysToCache": 60, // (Optional - default: 60) How many days of incidents to cache in the dashboard
  "headerTitle": "Contacting SRE On Call", // (Optional) Title for the information section
  // (Optional) Section of HTML to show in the info section, for providing on-call phone numbers, etc.
  "headerHtml": "<p>First, <b>if this tab is <span class=\"text-danger\">RED<span></b> our alerting systems have notified us of the problem. If you are having an issue ...</p></div>",
  "primaryScheduleId": "AEDJIL", // (Optional) If multiple schedule exist, this one is treated as the primary
  /* Map of Opserver account names (usually Active Directory) to PagerDuty emails
   With this provided, known users can take on-call because we know how to map the action to. */
  "userNameMap": [
    {
      "opServerName": "jsmith", // Opserver login (probably Active Directory user name)
      "emailUser": "jsmith@mycompany.com" // PagerDuty email
    }
  ]
}
```

#### Redis Settings
```json
{
  /* If a server doesn't specify any instances, then these will be used as a fallback.
     For most simple setups, this is a single :6379 instance 
     This is intended for setups with a lot of the same mirrors.
     If that's not use, just configure per-server below under "Servers". */
  "defaults": {
    "instances": [
      {
        "name": "Core (Q&A)", // Name of the instance for the UI
        "port": 6379, // Port redis is running on
        /* (Optional) Regexes to use in memory analysis
           Each key is evaluated against these and they're use for bucketing.
           This helps find out what the heavy key patterns are. */
        "analysisRegexes": {
          "**local**": "^local-",
          "**dev**": "^dev-",
          "**local:**": "^local:",
          "**dev:**": "^dev:"
        }
      },
      {
        "name": "Careers",
        "port": 6380
      }
    ]
  },
  /* Individually specified servers (DNS entries or IPs)
     Each of these may specify their own instances. 
     If they don't, each will inherit the instances configured in allServers above. */
  "Servers": [
    // Simple server, inheriting the instances from allServers
    { "name": "ny-redis01" },
    { "name": "ny-redis02" },
    // Server specifying it's own instance list, NOT inheriting from allServers above
    {
      "name": "ny-mlredis01",
      "instances": [
        {
          "name": "Machine Learning",
          "port": 6379,
          "password": "<Instance Password>", // Instance has a password
          "useSSL": true // (Optional) Connect via SSL (not built into redis itself - default is false)
        }
      ]
    }
  ]
}
```

#### SQL Settings
Opserver is setup to handle both standalone Microsoft SQL Server Instances and AlwaysOn Availability Groups (not FCI clusters, etc.).

Because AlwaysOn AGs can get into a state where the master does not know about the disconnected replicas, Opserver monitors _each member directly_ to get the whole picture.

```json
{
  // (Optional) The default connection string used unless specifically provided on a node
  // $ServerName$ gets replaces with the name property of the instance
  "defaultConnectionString": "Data Source=$ServerName$;Initial Catalog=master;Integrated Security=SSPI;",
  "refreshIntervalSeconds": 30, // (Optional - default: 60) How often to poll all servers
  "clusters": [ // (Optional) Always On Availability Group Clusters
    {
      "name": "NY-Cluster01", // Used purely for display
      "refreshIntervalSeconds": 20, // How often to poll the server
      "nodes": [ // The list of nodes (servers) in this AG setup
        { "name": "NY-SQL01" },
        { "name": "NY-SQL02" },
        { "name": "CO-SQL01" }
      ]
    }
  ],
  "instances": [ // (Optional) Standalone instances
    { // An example with all the options configured
      "name": "NY-DB05",
      "connectionString": "Data Source=NY-DB05;Initial Catalog=bob;Integrated Security=SSPI;",
      "refreshIntervalSeconds": 200
    },
    // Some standalone servers (default instance) using default refresh and connection strings:
    { "name": "NY-DESQL01" },
    { "name": "NY-RESTORESQL01" },
    { "name": "NY-UTILSQL01" },
    // Example of a named instance
    { "name": "NY-SQL05\\MYINSTANCENAME" }
  ]
}
```