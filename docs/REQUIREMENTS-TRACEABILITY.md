# Newton requirements and traceability matrix

Status values are `Planned`, `In progress`, `Implemented` and `Verified`. Implemented means code exists; Verified requires objective acceptance evidence.

| ID | Requirement | Delivery | Status | Evidence / gate |
| --- | --- | --- | --- | --- |
| NEW-ARCH-001 | Core must not reference WPF, WebView2 or SQLite | 1.0 | Verified | `scripts/check-architecture.ps1`; Windows release run |
| NEW-TAB-001 | Tab state is independent of renderer instances | 1.0 | Implemented | `Core/Domain/BrowserModels.cs`; `WebView2RendererRegistry` |
| NEW-TAB-002 | Sleeping/discarded tabs preserve Newton metadata | 1.0 | In progress | Lifecycle state and renderer registry exist; pressure tests outstanding |
| NEW-PROF-001 | Profiles and workspaces are separate concepts | 1.0 | Implemented | Profile owns many workspaces; workspace owns tabs |
| NEW-PRIV-001 | Tracking decisions use `IContentFilter` | 1.0 | Implemented | Core filter plus WebView2 adapter |
| NEW-PRIV-002 | Standard, Balanced and Strict privacy levels | 1.0 | In progress | Policy model exists; settings UI and compatibility tests outstanding |
| NEW-PRIV-003 | Remote suggestions are disabled by default | 1.0 | Implemented | Separate search surface; no remote suggestion provider enabled |
| NEW-SEC-001 | Site permissions use a central policy | 1.0 | Implemented | `IPermissionPolicy`; WebView2 permission adapter |
| NEW-SEC-002 | Navigation schemes are explicitly governed | 1.0 | Implemented | `INavigationPolicy`; JavaScript/data/blob blocked |
| NEW-DATA-001 | SQLite schemas are versioned and migration-safe | 1.0 | Implemented | Schema v3, transactional migrations and pre-migration backup |
| NEW-REL-001 | Abnormal shutdown restores valid tabs | 1.0 | Implemented | Periodic recovery snapshot and tolerant record loading |
| NEW-REL-002 | Corrupt and interrupted recovery is tested | 1.0 | Planned | Automated corruption suite required |
| NEW-UPD-001 | Application updates are signed, verified and reversible | 1.0 | Planned | Current installer is unsigned; no updater exists |
| NEW-ACC-001 | Primary workflows pass keyboard, screen-reader, contrast and 200% scaling tests | 1.0 | Planned | Manual and automated evidence required |
| NEW-EXT-001 | Extension support has explicit compatibility and trust levels | Post-1.0 | Planned | Deliberately deferred |
| NEW-AI-001 | AI is disabled by default and context sharing is explicit | Post-1.0 | Implemented architecture only | Provider/context contracts and policy; no AI UI/provider shipped |

This matrix is deliberately conservative. A successful build does not verify accessibility, compatibility, security or performance claims.
