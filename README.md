# Newton Browser

Newton is an experimental Windows browser organised around task sessions instead of an endless row of tabs. It uses Microsoft's Evergreen WebView2 runtime so the rendering engine receives Chromium security updates independently of the application.

Its engine architecture—Blink rendering, V8 JavaScript, GPU compositing, multiprocess execution and site isolation—is documented in `ARCHITECTURE.md`.

## Current alpha capabilities

- Multiple task sessions and pages
- Separate website address bar and web-search box
- User-selectable DuckDuckGo, Bing, Brave Search and Google providers
- Private-by-default search UI with no remote query suggestions
- Back, forward, reload and keyboard shortcuts
- HTTPS status indicator
- Invalid-certificate cancellation
- Website permissions require a fresh, origin-labelled decision and are denied when refused
- Central permission policy separated from the WebView2 event adapter
- Explicit navigation policy blocks JavaScript and direct embedded-data schemes
- Password saving and form autofill disabled
- New-window requests contained in a new browser page
- No application telemetry
- Strict tracking prevention by default
- Network blocking for known advertising and analytics hosts
- Reduced high-entropy fingerprinting values
- Microsoft reputation checking for malicious and phishing sites
- Browser extensions disabled in this alpha while a controlled future model is evaluated
- Chromium process sandbox supplied and serviced by WebView2
- Evergreen browser-engine security updates
- Evergreen WebView2 environment isolated by browser profile
- Automatic suspension of inactive pages to reduce CPU, RAM and battery use
- Named workspaces using the Session Canvas rail
- Fundamental profile → workspace → tab-group → tab ownership model
- Versioned SQLite storage for Newton-owned history, bookmarks, profiles, workspaces and recovery state
- Schema-v3 recovery records with stable tab/workspace identifiers and malformed-record tolerance
- Automatic database backup before schema migration
- Periodic crash-recovery snapshots with clean-shutdown detection
- First-class tab lifecycle subsystem
- Vertical page list with visible tab-group labels
- Four quick tab groups: General, Research, Work and Later
- Two-page split view
- Inactive-page suspension, with full unload-and-restore hibernation planned
- Spacious, Compact and Focus interface layouts
- Light and dark themes
- Keyboard controls for navigation, commands, groups, split view and themes
- Dedicated search focus with `Ctrl+K` or `Ctrl+E`; `Ctrl+L` focuses the address bar
- Local and web PDF viewing through the built-in Chromium PDF renderer
- Picture-in-Picture for compatible page videos
- PNG capture of the visible browser viewport
- Chromium/WebView2 developer tools via F12 or Ctrl+Shift+I
- Internal `newton://version`, `newton://policy`, `newton://diagnostics`, `newton://performance` and `newton://crashes` pages
- Cryptographically secure 20-character password generation without storage
- Reproducible Windows release workflow and Inno Setup installer

See `COMPATIBILITY.md` for the web-platform matrix and `ENTERPRISE.md` for the enterprise-management gap analysis.

The incremental cross-platform boundary and proposed project split are documented in `TARGET_STRUCTURE.md`.

Engineering governance is tracked in `docs/REQUIREMENTS-TRACEABILITY.md`, `docs/ADR-REGISTER.md`, `docs/NEWTON-1.0-SCOPE.md`, `docs/THREAT-MODEL.md` and `docs/RELEASE-POLICY.md`.

AI is a separate optional subsystem, disabled by default. See `AI_POLICY.md` for provider independence and explicit page-context rules.
The architecture now separates AI processing, context disclosure and browser-action authority. No AI provider or agentic action implementation is enabled in this build.

## Commercial direction

Newton's browser core is intended to remain free. The first proposed paid service is **Newton Pro**, centred on end-to-end encrypted workspace synchronisation, advanced organisation, secure backup and optional provider-independent AI. The working target is **£5.99 per month**, subject to cost analysis and user validation. Essential security, privacy and browser updates will not be paywalled. See `docs/COMMERCIAL-MODEL.md`.

## Important security status

This is pre-release software. It has not undergone independent security review. The bundled, versioned blocklist is intentionally small in the first alpha; signed ruleset updates, per-site exceptions, signed automatic application updates, private sessions, downloads UI, history controls and accessibility testing remain incomplete. Fingerprinting protection is mitigation rather than anonymity because WebView2 controls the underlying engine. Engine updates are rapid through Evergreen WebView2, but Newton application updates are not yet automatic or signed. Do not use this alpha for banking, healthcare, passwords or other sensitive activity.

## Build

Install the .NET 8 SDK on Windows, then run:

```powershell
dotnet restore
dotnet run
```

Create a tagged GitHub release (`v0.1.0`) to run the Windows build workflow. Release executables are initially unsigned and may trigger Microsoft SmartScreen.

## Performance status

Newton is designed for low shell overhead, but no performance superiority is claimed before repeatable Windows benchmarks are completed. See `PERFORMANCE.md` for the acceptance criteria.

## Licence

MPL-2.0. Newton is the selected product name, subject to formal trademark clearance before commercial release.
