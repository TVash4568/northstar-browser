# Performance acceptance criteria

Newton may be described as excellent only when a signed release is tested against current stable Edge, Chrome, Firefox and Brave on the same Windows 11 laptop, power mode and network.

## Required tests

1. Cold start: median time to interactive across ten launches.
2. Warm start: median time to interactive across ten launches.
3. Page load: median Speedometer and WebXPRT results plus five representative sites.
4. Many pages: working set and responsiveness with 10, 30 and 100 pages.
5. Inactive pages: memory and CPU after ten minutes in the background.
6. Battery: energy use during a controlled 60-minute browsing script.
7. Ad-heavy pages: load time, transferred bytes, CPU time and visual breakage.

## Release gate

- No material regression versus the WebView2/Edge baseline in raw page performance.
- Lower inactive-page CPU and memory after suspension.
- No user-interface hang in the 100-page test.
- No unexplained background network traffic from Newton.
- All results, hardware, versions and methodology published together.

Marketing descriptions must report measured results and limitations rather than using unsupported superlatives.
