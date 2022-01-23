---
layout: "default"
---
### How-To Build and Run Opserver

Since Opserver v2 is based on .NET Core, it has several deployment options. Once you've setup basic configuration (the app will run and prompt you alerting to zero configuration if you skip this step), you can run it via:

- `donet run` via the command line (requires [the .NET 6.0+ runtime](https://dotnet.microsoft.com/download) to be installed - works on any OS)
- Building via `dotnet publish`, and running elsewhere.
- Using IIS on Windows (requires the [ASP.NET Core Runtime](https://dotnet.microsoft.com/download/dotnet-core/6.0), which includes the IIS module for .NET Core) - this is like Opserver v1.
- Via Docker - there's a `Dockerfile` in the root of this repo, you'd just want to mount your `/Config` folder as a volume. The hope is to publish this to DokerHub soon.

#### Via `dotnet run`

This is the simplest option if you already have the .NET Runtime for testing, etc. 
```bash
dotnet run -p src/Opserver.Web
```

Output will look like this:
```
Hosting environment: Development
Content root path: /Users/<user>/git/Opserver/OpserverCore/src/Opserver.Web
Now listening on: http://localhost:56058
Application started. Press Ctrl+C to shut down.
```

Note: in addition to the supported `Kestrel` section of [Opserver's configuration](), you can specify the URL it runs at via `dotnet run -p src/Opserver.Web --urls https://localhost:5000`.

#### Via `dotnet publish`

This is the build option to publish the output elsewhere, for a specific platform. The basics here are: build for the target you want, and copy that publish directory to wherever you want to deploy it.

For example, for Windows:
```bash
dotnet publish src/Opserver.Web -o ./publish -r win10-x64
cd publish
Opserver.Web.exe
```
...or for Linux:
```bash
dotnet publish src/Opserver.Web -o ./publish -r linux-x64
cd publish
./Opserver.Web
```
...or for macOS:
```bash
dotnet publish src/Opserver.Web -o ./publish -r osx-x64
cd publish
./Opserver.Web
```
For the `-r` arguments (called a "runtime identifier", see [the runtime identifier catalog](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog) - supports many operating systems).

#### Via IIS

```
TODO
```

#### Via Docker

While there isn't a CI image up to DockerHub just yet, there is a working `Dockerfile` at the root of the repo and a preview is available as `opserver/opserver:preview1`. There's nothing special here - it's listening on `:80`. To get your config, you'll want to mount a `/Config` directory to `/app/Config`.

To run the preview on Docker Hub:
```
docker run --rm -p 4001:80 -v ~/git/Opserver/Config:/app/Config/ opserver/opserver:preview1
```

For example, here's buildng, running, and disdcarding all in one. It's using a "Config" folder beside the repo with your configuration `.json`, e.g. `../Config/opserverSettings.json`, or individual files if migrating from v1 perhaps. To run:
```bash
docker run -p 4001:80 -v ~/git/Opserver/Config:/app/Config/ --rm -it $(docker build -q .)
```

This is building the image (and throwing it away) via `docker build`, the `-q` is to return only the identifier, which is what we're using to run it. You can just `docker build` with a tag as well. The port mapping is mapping 4001 on the host (can be anything you like) to 80 in the container. The volume is using our `Config` directory path which has our configuration files and is mapping them to `/app/Config`, where the app in the container is looking for them to be.