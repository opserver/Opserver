---
title: "Release Notes"
layout: "default"
---
### Release Notes
This page tracks major changes included in any update starting with version 2.0

#### Version 2.0.0 (In preview)
- **New**:
  - Runs on .NET Core (previous .NET Framework)
  - Now supports Windows, Linux, and MacOS
    - Windows: Doesn't require IIS - can spin up via `dotnet run`, or via a publish and copy
  - Supports running in a container (via Docker)
  - Adds SignalFX as a dashboard provider
  - Overhauled configuration system (though most old formats are supported for a smooth transition)
