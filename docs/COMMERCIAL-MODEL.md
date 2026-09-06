# Newton commercial model

Status: Product direction accepted; services not implemented.

## Positioning

The Newton browser is the free distribution product. Revenue should come from optional services that create recurring operating costs or substantial additional value, not from weakening the free browser.

## Product boundary

| Newton Free | Newton Pro |
| --- | --- |
| Core browsing and WebView2 security updates | End-to-end encrypted workspace synchronisation |
| Tracking and advertising protection | Secure encrypted cloud backup and recovery |
| Profiles, tabs and standard workspaces | Advanced workspace and tab organisation |
| Bookmarks, history, downloads and crash recovery | Cross-device continuity when supported |
| Private browsing and permission controls | Optional provider-independent AI features |
| Essential accessibility and security fixes | Priority support may be evaluated later |

## Pricing hypothesis

- Working target: **£5.99 per month**.
- Annual pricing, trials, regional pricing and team plans are undecided.
- This is a hypothesis, not a promise. It must be validated against infrastructure cost, tax, payment fees, support burden, willingness to pay and churn.

## Non-negotiable rules

- Do not sell browsing history or behavioural profiles.
- Do not insert sponsored results or advertising into browsing.
- Do not paywall essential security, privacy fixes or engine/application updates.
- Do not transmit browsing context to an AI provider without an explicit, scoped user action.
- Do not describe sync as end-to-end encrypted until the protocol and clients have been independently reviewed.
- A Newton account must not be required for the free local browser.
- Loss of a Pro subscription must not make locally owned user data inaccessible.

## Delivery gates

Newton Pro work begins only after the free browser has dependable profiles, versioned storage, migration and recovery tests, private-mode isolation, signed automatic updates and a documented threat model.

Before paid launch, Newton also needs:

1. A sync protocol and cryptographic design review.
2. Key recovery and device-revocation design.
3. Data export, deletion and account lifecycle controls.
4. UK GDPR and consumer-contract review.
5. Payment, tax and refund processes.
6. Measured hosting and support costs.
7. Clear service availability and backup limitations.
8. Trademark clearance for Newton.

## Open-source boundary

The current MPL-2.0 browser code may remain public. Newton's name, logo and service trademarks require separate protection. Hosting, account services and operational infrastructure may be proprietary, but this boundary must be documented before accepting payment.
