# Northstar Browser (temporary codename)

Northstar is an experimental Windows browser organised around task sessions instead of an endless row of tabs. It uses Microsoft's Evergreen WebView2 runtime so the rendering engine receives Chromium security updates independently of the application.

## Current alpha capabilities

- Multiple task sessions and pages
- Address and DuckDuckGo search bar
- Back, forward, reload and keyboard shortcuts
- HTTPS status indicator
- Invalid-certificate cancellation
- Website permissions denied by default
- Password saving and form autofill disabled
- New-window requests contained in a new browser page
- No application telemetry
- Reproducible Windows release workflow and Inno Setup installer

## Important security status

This is pre-release software. It has not undergone independent security review. Tracker blocking, per-site permission decisions, signed automatic application updates, private sessions, downloads UI, history controls and accessibility testing remain incomplete. Do not use this alpha for banking, healthcare, passwords or other sensitive activity.

## Build

Install the .NET 8 SDK on Windows, then run:

```powershell
dotnet restore
dotnet run
```

Create a tagged GitHub release (`v0.1.0`) to run the Windows build workflow. Release executables are initially unsigned and may trigger Microsoft SmartScreen.

## Licence

MPL-2.0. The permanent product name and visual identity are intentionally not assigned yet.
