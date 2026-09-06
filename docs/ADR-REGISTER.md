# Newton architecture decision register

| ADR | Decision | Status | Consequence |
| --- | --- | --- | --- |
| ADR-001 | Windows first, WPF shell and Evergreen WebView2 | Accepted | Fast Windows delivery; WPF is not treated as the permanent cross-platform UI |
| ADR-002 | Newton is a browser product, not a Chromium fork | Accepted | Microsoft services the engine; Newton owns product behaviour |
| ADR-003 | Tabs never own WebView2 controls | Accepted | Renderers can sleep, fail or be replaced without losing tab identity |
| ADR-004 | Profile, workspace, group and window are distinct | Accepted | One profile may contain many organisational workspaces |
| ADR-005 | Newton-owned browser data uses versioned SQLite | Accepted | Migrations, backups and corruption handling are release requirements |
| ADR-006 | Address, search and future commands remain separate | Accepted | Search suggestions can be disabled independently from search execution |
| ADR-007 | Privacy policy is engine-independent | Accepted | WebView2/CEF adapters enforce the same Newton decisions |
| ADR-008 | Telemetry and AI are off by default | Accepted | Any future context transfer requires explicit action-specific consent |
| ADR-009 | Extensions are deferred, not declared impossible | Accepted | Future support requires compatibility and permission review |
| ADR-010 | macOS/Linux work follows Windows quality gates | Accepted | No premature cross-platform production promise |
| ADR-011 | AI processing, context and actions use three separate contracts | Accepted | A model provider cannot grant itself data access or browser authority |
| ADR-012 | Newton 1.0 keeps extensions disabled | Accepted | Resolves the reports' conflict: v1 does not promise extension compatibility; future support must enforce least privilege |
| ADR-013 | Free browser core with optional Newton Pro services | Accepted | Essential security remains free; paid value comes from encrypted sync, backup, advanced organisation and optional AI |

New ADRs must record context, decision, alternatives, security/privacy impact, migration impact and reversal cost.
