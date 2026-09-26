# WP7 XAP Runner for iOS

> **Project scope:** Windows Phone 7 emulator / binary-compatibility runtime research for iOS.  
> **This is not an offensive cybersecurity project.** It is not intended for unauthorized access, exploitation, malware, credential theft, persistence, command-and-control, phishing, destructive actions, or network intrusion.  
> See [Project Scope & Safety](docs/PROJECT-SCOPE-SAFETY.md) for the canonical scope statement.

Experimental compatibility runtime for running original Windows Phone 7 `.xap` applications on iPhone.

This repository is intentionally separate from the EKA2L1/Symbian project.

## Current checkpoint

The canonical, complete project handoff is:

**[docs/handoff/CURRENT.md](docs/handoff/CURRENT.md)**

Current engineering state:

- XAPSCAN1 synthetic: **GREEN**
- XAPSCAN1 real WP7 package: **GREEN**
- ILRUN1 iOS 15 arm64 build: **GREEN**
- first physical-device launch: **app stays alive, black content, no crash observed**
- persistent log directory + iOS Files sharing: **IMPLEMENTED**
- ILRUN1 moved off the UIKit main thread: **IMPLEMENTED**
- ILRUN1-IOS2-PERSISTLOG physical-device result: **PENDING**
- BIND1: **NEXT after ILRUN1 execution proof**

## Architecture

```text
Original WP7 .XAP
  -> XAP Package Reader / XAPSCAN1
  -> WP7 Assembly Resolver / BIND1
  -> managed IL execution / Mono interpreter
  -> Silverlight compatibility runtime
  -> Microsoft.Phone compatibility layer
  -> iOS host bridge
```

The project follows a **target-first HLE** strategy: prove each runtime boundary with real packages and explicit failure markers instead of recreating the entire Windows Phone OS up front.

## Current status

| Milestone | Status |
|---|---|
| XAPSCAN1 synthetic XAP | GREEN |
| XAPSCAN1 real WP7 XAP | GREEN |
| ILRUN1 iOS build | GREEN |
| ILRUN1 physical iPhone execution | DEVICE TEST REQUIRED |
| BIND1 legacy assembly resolver | NEXT |
| XAML1 / PAGE1 / NAV1 | LATER |
| PHONEAPI1 | LATER |
| XNA full runtime | PHASE 2 |

## XAPSCAN1

Static scanner for original WP7/Silverlight/XNA packages.

It extracts:

- package SHA-256 and ZIP inventory
- `AppManifest.xaml` / `AppManifest.xml`
- `WMAppManifest.xml`
- entry assembly/type and default navigation page
- CLR assembly identity and metadata version
- `AssemblyRef`, `TypeRef`, `MemberRef`
- embedded resources
- IL-only vs mixed/native images
- P/Invoke
- XNA Graphics dependencies
- compatibility tags and Failure Oracle markers

Run:

```bash
dotnet run --project src/XapScan/XapScan.csproj -- MyApp.xap --out MyApp.xapscan1.json
```

## ILRUN1

ILRUN1 is the first decisive iOS runtime experiment.

`IlPayload.dll` is built separately and placed in the iOS app bundle only as a raw resource. It is **not** a `ProjectReference` or normal managed assembly reference.

The iOS host performs:

```text
read IlPayload.dll bytes
  -> Assembly.Load(byte[])
  -> resolve IlPayload.EntryPoint
  -> resolve Run()
  -> reflection invoke
  -> expect XAP_ILRUN1_PASS:42
```

Required physical-device PASS markers:

```text
[ILRUN1][START]
[ILRUN1][BUILD] ILRUN1-IOS1
[ILRUN1][FILE_FOUND]
[ILRUN1][FILE_READ_OK]
[ILRUN1][ASSEMBLY_LOAD_OK]
[ILRUN1][TYPE_RESOLVE_OK]
[ILRUN1][METHOD_RESOLVE_OK]
[ILRUN1][METHOD_INVOKE_OK] result=XAP_ILRUN1_PASS:42
[ILRUN1][PASS] external managed IL executed on iOS
[ILRUN1][END] PASS
```

The current device build targets **iOS 15.0+ / ios-arm64** with the Mono interpreter enabled and produces an unsigned IPA intended for re-signing with ESign for testing.

See [docs/ILRUN1-CHECKPOINT.md](docs/ILRUN1-CHECKPOINT.md).

## CI

Three workflows protect the current contract:

- `WP7 XAPSCAN1` — synthetic XAP regression
- `WP7 XAPSCAN1 Real XAP` — real WP7 XNA regression
- `WP7 ILRUN1 iOS` — iOS 15 device build + unsigned IPA artifact

The real-XAP workflow uses the MIT-licensed **Aleterated** WP7/XNA sample from a pinned upstream commit. The binary is downloaded during CI and is not vendored in this repository.

## Roadmap

```text
XAPSCAN1
   ↓
ILRUN1
   ↓
BIND1
   ↓
XAML1
   ↓
PAGE1
   ↓
NAV1
   ↓
PHONEAPI1
   ↓
WPUI1
   ↓
DEVICE1
   ↓
XNA1
```

## License / provenance

A project-wide license has not yet been selected. External fixtures and referenced projects retain their original licenses. Source from third-party compatibility projects should be treated according to its own license and provenance.
