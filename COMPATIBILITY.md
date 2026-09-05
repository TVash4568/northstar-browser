# Compatibility and implementation status

Northstar uses Microsoft Evergreen WebView2. Engine features follow the installed WebView2 runtime and compatible Windows hardware; they are not independent Northstar implementations.

| Area | Current implementation | Status or constraint |
| --- | --- | --- |
| Engine | Blink, V8, Skia/Chromium GPU pipeline | Supplied by Evergreen WebView2 |
| Web platform | WebAssembly, WebGL 1/2, WebGPU, WebRTC, workers, service workers, IndexedDB, WebSockets, WebTransport, WebCodecs, MSE and WebAuthn | Runtime, hardware and site dependent |
| Media | H.264, VP8/VP9, AV1, AAC, MP3, Opus and FLAC where supported by Windows/WebView2 | HEVC may require licensed Windows components |
| Picture-in-Picture | Northstar command for compatible page video | Implemented |
| PDF and screenshots | WebView2 PDF renderer and PNG viewport capture | Implemented |
| Permissions | Per-request origin and capability prompt; decisions are not persisted | Implemented; deny is the default response |
| Developer tools | DOM/CSS inspection, JS debugging, network, performance, memory, accessibility and service-worker tooling | WebView2 DevTools enabled locally; no remote debugging port |
| Password generation | 20-character CSPRNG-generated value copied to the clipboard and never stored | Implemented |
| Password storage | WebView2 autosave and autofill remain disabled | Awaiting a reviewed Windows-protected vault |
| Passkeys/FIDO2 | WebAuthn support inherited from WebView2 and Windows | Site, runtime and authenticator dependent |
| Extensions | Disabled at WebView2 environment creation | Manifest V2 unsupported; restricted MV3-like design remains future work |
| PWA installation | Service-worker and manifest web APIs may run in pages | Browser-level installation, shortcuts and app management not implemented |
| Push/background work | Subject to WebView2 support and explicit permission | Background execution must remain constrained |
| DRM | EME/DRM only where runtime, service and device permit | Netflix, Prime Video, 1080p/4K/HDR and hardware DRM are not guaranteed |

## Deliberately gated features

A password vault, breach monitoring, encrypted cross-device sync, installable PWAs, enterprise policy, extension installation, certified streaming playback, hardware DRM guarantees and signed automatic Northstar updates require further engineering, external infrastructure, independent review or provider approval. They must not be advertised as complete.
