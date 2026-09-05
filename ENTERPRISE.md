# Enterprise capability status

Newton is not currently an enterprise-managed browser. WebView2 provides a maintained Windows engine, but it does not automatically give the Newton shell Microsoft Edge's management, identity or compliance estate.

| Capability | Current status | Required before a production claim |
| --- | --- | --- |
| Group Policy | Not implemented | Versioned ADMX/ADML templates, policy registry schema, precedence rules, tests and administrator documentation |
| Active Directory | No Newton-specific integration | Managed deployment, authentication design and domain-environment testing |
| Microsoft Entra ID | Web pages may use normal web authentication | Brokered identity/SSO design, tenant controls, privacy review and Microsoft-supported integration |
| Microsoft 365 | Ordinary website compatibility only | No Newton-native integration is claimed |
| Intune | Generic packaged-app deployment may be possible | Supported packaging, configuration and compliance documentation; no dedicated policy channel yet |
| Stable channel | Evergreen WebView2 engine plus alpha Newton shell | Signed Newton releases, staged rollout, rollback and supported release lifecycle |
| Kiosk mode | Not implemented | Origin allow-list, escape controls, policy enforcement, crash recovery and administrator override |
| DLP | Not implemented | Approved enterprise security provider integration and independently tested enforcement |
| Windows security | Reputation checking, invalid-certificate cancellation and WebView2 sandbox | Code signing, secure updater, Windows Hello-backed credential design and security review |

Newton must not reuse Microsoft Edge enterprise claims merely because both use WebView2/Chromium components. Enterprise controls belong to the complete browser product and its management infrastructure.
