# Newton release policy

## Channels

- **Alpha:** incomplete and unsigned; unsuitable for sensitive use.
- **Beta:** feature-complete candidate with automated gates, still under broader validation.
- **Stable:** permitted only after all Newton 1.0 release gates pass.

## Mandatory Stable gates

1. Release build, unit, integration and end-to-end suites pass.
2. Architecture dependency rules pass.
3. No unresolved Critical or unaccepted High security finding.
4. NuGet vulnerability audit and supply-chain review pass.
5. Data migration, corruption and rollback tests pass.
6. Compatibility matrix passes at its approved threshold.
7. Accessibility checks pass for keyboard, screen reader, contrast and 200% scaling.
8. Performance budgets show no blocking regression on fixed hardware/workloads.
9. Installer and updater are signed, verified, health-checked and reversible.
10. Requirements matrix and user/security documentation match the shipped build.

The present Newton build is Alpha. Passing compilation and installer creation alone does not satisfy this policy.
