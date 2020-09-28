---
layout: "default"
---
### How-To Upgrade From Opserver V1 to V2

Opserver v2 is a migration from .NET Full Framework (requiring IIS) to .NET Core. This means it can run in a variety of ways:

- Under IIS (you'll need the ASP.NET Core hosting module - [here's how that works'](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/?view=aspnetcore-3.1))
- Directly as an executable (using the Kestrel web server)
- Using a container (these aren't published yet, but we'll get there)

It can run on Windows, macOS, or Linux.

Legacy configuration under `src\Opserver.Web\Config` is still supported with the exception of `securitySettings.config`.
Everything is now JSON (though it may support more options in the future), and there's no XML/JSON mix.

See [the confgiuration documentation](../Configuration) for examples of all configs.