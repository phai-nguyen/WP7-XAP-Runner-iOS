# Project Scope & Safety Clarification

## What this project is

**WP7 XAP Runner for iOS** is an emulator / compatibility-runtime research project.

Its goal is to investigate whether original **Windows Phone 7** applications packaged as `.xap` files can be loaded and executed on iPhone by recreating the minimum runtime environment they expect:

- XAP package parsing;
- ECMA-335 / managed IL execution;
- legacy .NET / Silverlight assembly compatibility;
- XAML and UI compatibility;
- `Microsoft.Phone` API compatibility;
- mapping ordinary phone capabilities such as storage, touch, audio, camera, sensors, and networking to iOS equivalents.

The design follows a **target-first HLE (high-level emulation)** approach rather than emulating the complete Windows Phone hardware/OS stack.

## What this project is NOT

This repository is **not a cybersecurity attack project** and is not intended for:

- unauthorized access to computers, phones, accounts, or networks;
- exploitation of security vulnerabilities;
- credential theft or token/session theft;
- malware, ransomware, spyware, or persistence;
- command-and-control infrastructure;
- phishing;
- exploit delivery;
- evasion of security controls;
- destructive actions;
- network scanning or intrusion against third-party systems.

No current project milestone is designed around those activities.

## About networking APIs

Windows Phone applications may legitimately use APIs such as HTTP, WebClient, sockets, or network-status queries. If this project later implements those APIs, the purpose is **application compatibility**: allowing an old WP7 app to use its expected networking interface.

That is separate from offensive-security tooling.

## About dynamic managed-code loading

The `ILRUN1` experiment intentionally loads a small, project-owned managed DLL at runtime to answer an emulator/runtime engineering question:

> Can the Mono interpreter on iOS execute managed IL that was not statically linked into the IPA?

The payload is a controlled test assembly produced by this repository. The experiment exists to validate the execution layer required for legacy WP7 application compatibility.

## Research boundaries

The project focuses on:

1. package compatibility;
2. managed runtime compatibility;
3. Silverlight/XAML compatibility;
4. WP7 application-model compatibility;
5. iOS host integration;
6. diagnostics and deterministic failure-oracle logging.

Security bypasses, exploitation, and unauthorized access are outside project scope.

## Relationship to other projects

This project is independent from the user's **EKA2L1 / Symbian Nokia 5800** compatibility work.

The two projects share an engineering methodology — target-first HLE, compatibility shims, Failure Oracle logging, device testing, and incremental boot/runtime milestones — but they target different legacy platforms and live in separate repositories.
