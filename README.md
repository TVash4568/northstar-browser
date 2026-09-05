# Northstar Browser (temporary codename)

Northstar is an experimental Windows browser organised around task sessions instead of an endless row of tabs. It uses Microsoft's Evergreen WebView2 runtime so the rendering engine receives Chromium security updates independently of the application.

Its engine architecture—Blink rendering, V8 JavaScript, GPU compositing, multiprocess execution and site isolation—is documented in `ARCHITECTURE.md`.

## Current alpha capabilities

- Multiple task sessions and pages
- Address and DuckDuckGo search bar
- Back, forward, reload and keyboard shortcuts
- HTTPS status indicator
- Invalid-certificate cancellation
- Website permissions require a fresh, origin-labelled decision and are denied when refused
- Password saving and form autofill disabled
- New-window requests contained in a new browser page
- No application telemetry
- Strict tracking prevention by default
- Network blocking for known advertising and analytics hosts
- Reduced high-entropy fingerprinting values
- Microsoft reputation checking for malicious and phishing sites
- Browser extensions disabled to remove extension supply-chain risk
- Chromium process sandbox supplied and serviced by WebView2
- Evergreen browser-engine security updates
- Shared WebView2 environment to avoid redundant engine initialisation
- Automatic suspension of inactive pages to reduce CPU, RAM and battery use
- Named workspaces using the Session Canvas rail
- Vertical page list with visible tab-group labels
- Four quick tab groups: General, Research, Work and Later
- Two-page split view
- Inactive-page suspension, with full unload-and-restore hibernation planned
- Spacious, Compact and Focus interface layouts
- Light and dark themes
- Keyboard controls for navigation, commands, groups, split view and themes
- Address-bar command mode (`Ctrl+K`): `>split`, `>group`, `>theme`, `>layout`
- Local and web PDF viewing through the built-in Chromium PDF renderer
- Picture-in-Picture for compatible page videos
- PNG capture of the visible browser viewport
- Chromium/WebView2 developer tools via F12 or Ctrl+Shift+I
- Cryptographically secure 20-character password generation without storage
- Reproducible Windows release workflow and Inno Setup installer

See `COMPATIBILITY.md` for the web-platform matrix and `ENTERPRISE.md` for the enterprise-management gap analysis.

## Important security status

This is pre-release software. It has not undergone independent security review. The bundled blocklist is intentionally small in the first alpha; automatic list updates, per-site exceptions, signed automatic application updates, private sessions, downloads UI, history controls and accessibility testing remain incomplete. Fingerprinting protection is mitigation rather than anonymity because WebView2 controls the underlying engine. Engine updates are rapid through Evergreen WebView2, but Northstar application updates are not yet automatic or signed. Do not use this alpha for banking, healthcare, passwords or other sensitive activity.

## Build

Install the .NET 8 SDK on Windows, then run:

```powershell
dotnet restore
dotnet run
```

Create a tagged GitHub release (`v0.1.0`) to run the Windows build workflow. Release executables are initially unsigned and may trigger Microsoft SmartScreen.

## Performance status

Northstar is designed for low shell overhead, but no performance superiority is claimed before repeatable Windows benchmarks are completed. See `PERFORMANCE.md` for the acceptance criteria.

## Licence

MPL-2.0. The permanent product name and visual identity are intentionally not assigned yet.
