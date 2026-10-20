# Security policy

## Reporting a vulnerability

Open a private security advisory on GitHub. Do not file a public issue for anything that could
lead to data loss.

We care specifically about:

- any path that reaches a delete call without passing through `CleanupGate`;
- a root that escapes validation (`PathSafetyValidator`);
- a reparse point or junction that is followed;
- a WSL recipe that is not in the whitelist.

## Supported versions

The latest release on the `main` branch is supported.
