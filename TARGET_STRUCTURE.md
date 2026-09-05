# Target project structure

```text
Newton.Core
├── Browsing
├── Tabs
├── History
├── Bookmarks
├── Profiles
├── Sessions
├── Privacy
├── Downloads
├── Search
└── Sync

Newton.Windows
├── WPF
└── WebView2
```

`Newton.Core` must never reference WPF or WebView2. Platform engines implement `IBrowserEngine`; UI and product services consume that interface. The current alpha remains a single Windows project while behaviour is covered and extracted incrementally.

## Ownership hierarchy

```text
Profile (identity, cookies, accounts and storage)
└── Workspace (organisational tab collection)
    └── Tab group
        └── Tab
```

A window displays one profile context. A private window uses a new ephemeral profile and must never reuse the normal profile's WebView2 data directory. Profiles are fundamental data owners, not labels applied to workspaces.

## Platform decision

| Stage | Choice | Reason |
| --- | --- | --- |
| Current | WPF + Evergreen WebView2 | Free independent engine servicing, lowest delivery cost, primary Windows target |
| Architecture preparation | Platform-neutral `Newton.Core` contracts | Prevent new browser logic becoming tied to WebView2 |
| UI boundary | Platform-neutral view-models and services | WPF is a replaceable presentation adapter, not the long-term product architecture |
| Future evaluation | Qt WebEngine proof of concept | Cross-platform with greater browser control, but substantial packaging and security-maintenance cost |
| Rejected for now | Electron | Cross-platform convenience conflicts with the low-overhead target |
| Rejected for now | CEF or Chromium fork | Engineering and security-rebase burden is not sustainable for the current project |
