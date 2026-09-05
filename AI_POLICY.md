# Optional AI policy

AI is disabled by default and is not part of Newton's core browsing path.

```text
Settings
└── AI
    ├── Disabled (default)
    ├── OpenAI
    ├── Gemini
    ├── Claude
    └── Local model
```

Provider implementations sit behind Newton-owned `IAIProvider`; no provider owns the browser integration. Newton must not send a URL, page text, browsing history, cookies, form contents, downloads or profile data implicitly.

Passwords and private-page content are never eligible for AI sharing. Ordinary page context requires a visible confirmation for every action, identifying the destination provider, page origin and information being sent. Choosing a provider is not continuing consent for history, other tabs or future pages. The core `AIContextPolicy` refuses private-profile context and refuses context without per-action confirmation.

Provider credentials must use operating-system-protected storage and must never be committed to source control or written to logs.

Disabling AI must prevent provider initialisation and network requests, not merely hide AI controls. AI failures must never block navigation or weaken browser security decisions.
