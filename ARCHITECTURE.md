# Browser architecture

Newton is a Windows browser shell over the Microsoft Evergreen WebView2 runtime. It does not claim to maintain an independent web engine.

| Layer | Implementation | Ownership and update path |
| --- | --- | --- |
| Browser foundation | Newton WPF shell plus WebView2 | Newton shell; Microsoft runtime |
| Rendering and layout | Chromium Blink | Microsoft Evergreen WebView2 |
| JavaScript | Chromium V8 | Microsoft Evergreen WebView2 |
| Graphics and compositing | Chromium GPU pipeline, Skia and Windows composition | Chromium/Microsoft/Windows |
| Multiprocess | Browser, renderer, GPU and utility processes | Chromium/WebView2 architecture |
| Site isolation | Chromium/WebView2 process isolation | Runtime and applicable enterprise policies |

## Security acceptance tests

- Verify cross-origin frames receive separate renderer processes where the runtime supports it.
- Verify a renderer crash does not terminate the Newton shell.
- Verify invalid certificates are cancelled.
- Verify extensions remain disabled at environment creation.
- Verify Microsoft reputation checking remains enabled.
- Verify the installed Evergreen runtime is supported and updated.
- Re-run the checks after every WebView2 SDK upgrade.

Site isolation must be validated on the shipped Windows runtime. Newton will not make an absolute site-isolation claim based only on Chromium ancestry because runtime policies and implementation details can change.
