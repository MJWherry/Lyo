# Security policy

Lyo includes cryptography and other security-sensitive libraries. If you believe
you have found a security vulnerability, please report it responsibly using the
process below.

## Reporting a vulnerability

- **Do not** open a public GitHub issue, pull request, or discussion for a
  security problem.
- Report privately via GitHub's
  [private vulnerability reporting](https://github.com/mjwherry/Lyo/security/advisories/new)
  ("Report a vulnerability" under the repository's **Security** tab). If that is
  unavailable, contact the maintainer privately through their GitHub profile
  ([@mjwherry](https://github.com/mjwherry)).

Please include enough detail to reproduce and assess the issue:

- affected package(s) and version(s) or commit;
- a description of the vulnerability and its impact;
- reproduction steps or a minimal proof of concept;
- any suggested remediation, if known.

## What to expect

This is a personal, best-effort project (not a commercially supported product),
so there is no guaranteed response SLA. That said, reports are taken seriously:
you can expect an acknowledgement, an assessment, and — for confirmed issues — a
fix and a coordinated disclosure once a remedy is available. Please allow a
reasonable amount of time before any public disclosure.

## Scope and expectations

- Supported target: the current `main` branch and the latest released package
  versions. Older versions are generally not patched.
- Lyo packages secure payloads and data, not the surrounding deployment. Issues
  that depend on caller misconfiguration (for example using `LocalKeyStore` in
  production, committing key material, or missing TLS) are out of scope as library
  vulnerabilities — see the [security model](docs/security/README.md) for the
  division of responsibility.
- Vulnerabilities in third-party dependencies should also be reported upstream to
  the relevant project.

## Related documentation

- Project security model and threat overview: [`docs/security/README.md`](docs/security/README.md)
- Encryption design notes: [`docs/security/encryption.md`](docs/security/encryption.md)
