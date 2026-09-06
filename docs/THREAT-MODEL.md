# Newton security and privacy threat model

| Threat | Attack surface | Current mitigation | Detection / recovery | Remaining evidence |
| --- | --- | --- | --- | --- |
| Malicious website | Renderer, permissions, navigation, messages | WebView2 sandbox, web messages disabled, central permission/navigation policies, certificate failure cancellation | Renderer failure state and tab recovery | Adversarial navigation/permission tests |
| Malicious download | Download interception and saved files | No automatic execution; download subsystem contracts separate scanning/history | Not yet complete | Download manager, reputation/scanner integration and tests |
| Malicious extension | Broad host/native permissions | Extensions disabled in current environment | Future trust levels required | Post-v1 compatibility programme |
| Compromised update | Installer and release channel | GitHub build plus SHA-256 artefact digest | Manual rollback only | Signing, staged updater, health check and rollback |
| Compromised dependency | NuGet/build actions | Architecture gate and dependency audit in CI | Build failure/advisory review | SBOM, pinning and formal licence inventory |
| Local malicious process | Profile, SQLite and secrets | Profile separation; no stored Newton passwords | Database backup before migration | OS credential-store implementation and tamper tests |
| Malformed recovery data | SQLite recovery rows | Transactions, schema v3, URL validation and tolerant defaults | Invalid records skipped; pre-migration backup | Automated corruption/interruption suite |
| AI disclosure | Page/history/credentials | AI disabled; provider and context contracts separated; private context prohibited | No AI provider shipped | Provider-specific review before activation |

## Trust boundary

Arbitrary page content has no general Newton native bridge. Any future bridge must perform origin validation, schema validation, central permission/policy evaluation and expose only narrow Newton-owned commands. Filesystem, shell, credentials and unrestricted OS capabilities must never be exposed directly.

## Release rule

Unresolved Critical findings block every public build. Unresolved High findings normally block Stable. Risk acceptance must identify an owner, expiry and compensating controls.
