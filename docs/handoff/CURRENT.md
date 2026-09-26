# WP7 XAP Runner for iOS — CURRENT HANDOFF

**Date:** 2026-09-26  
**Canonical repository:** `phai-nguyen/WP7-XAP-Runner-iOS`  
**Canonical branch:** `main`  
**Project type:** Windows Phone 7 emulator / binary-compatibility runtime research for iOS  
**Security scope:** non-offensive compatibility/emulation research; see `docs/PROJECT-SCOPE-SAFETY.md`.

---

## 1. Project objective

The project investigates whether an iPhone can run original Windows Phone 7 applications distributed as `.xap` packages without requiring their source code to be rebuilt.

The intended architecture is:

```text
Original WP7 .XAP
    |
    v
XAP package reader / XAPSCAN1
    |
    v
legacy assembly resolver / BIND1
    |
    v
managed IL execution layer
    |
    v
Silverlight compatibility runtime
    |
    v
Microsoft.Phone compatibility APIs
    |
    v
iOS host bridge
```

The project does **not** attempt to emulate the entire Windows Phone OS first.

Instead it follows a **target-first HLE strategy**:

1. choose a real WP7 package or controlled runtime target;
2. execute as far as possible;
3. emit deterministic failure markers;
4. implement only the missing compatibility surface;
5. repeat.

This methodology is intentionally similar to the user's successful EKA2L1 COMPATBOOT workflow, but this repository is technically independent from the Symbian project.

---

## 2. Scope clarification

This is an emulator / compatibility-runtime project, **not a cybersecurity attack project**.

Current work is limited to legacy package parsing, managed-code execution, UI/runtime compatibility, mobile-device API mapping, diagnostics, and device testing.

It is not intended for unauthorized access, exploit delivery, malware, credential theft, persistence, command-and-control, phishing, destructive actions, or network intrusion.

See `docs/PROJECT-SCOPE-SAFETY.md` for the canonical scope statement.

---

## 3. Repository migration

The WP7 work originally began on a separate branch inside:

`phai-nguyen/Eka2l1_bot_menu_simbiam`

It has now been migrated into the dedicated standalone repository:

`phai-nguyen/WP7-XAP-Runner-iOS`

The standalone repository is now canonical.

Current root layout:

```text
WP7-XAP-Runner-iOS/
├── README.md
├── src/
│   ├── XapScan/
│   ├── IlPayload/
│   └── ILRun1Host/
├── tests/
├── docs/
│   ├── ILRUN1-CHECKPOINT.md
│   ├── PROJECT-SCOPE-SAFETY.md
│   └── handoff/
│       └── CURRENT.md
└── .github/
    └── workflows/
        ├── wp7-xapscan1.yml
        ├── wp7-xapscan1-real-wp7.yml
        └── wp7-ilrun1-ios.yml
```

The old EKA2L1 repository should be treated only as historical storage for the early WP7 branch.

---

## 4. Research conclusions that define the architecture

### 4.1 Yang Zhongke Windows-Phone-Emulator

The public `yangzhongke/Windows-Phone-Emulator` project is useful as an API and behavioral reference, but it is not itself a generic WP7 binary-XAP emulator.

The audited architecture uses desktop Silverlight 4 and replacement WP-style assemblies. It expects source to be rebuilt against that environment.

Therefore this project does **not** use a strategy of "port that simulator to iOS and open arbitrary XAPs."

Instead the simulator is treated as a **behavioral/API oracle**.

### 4.2 Required runtime layers

The working architecture separates:

- XAP package parsing;
- CLR/ECMA-335 managed IL execution;
- legacy assembly identity/binding;
- Silverlight primitives such as DependencyObject / DependencyProperty;
- XAML/resources;
- navigation/application lifecycle;
- `Microsoft.Phone` compatibility APIs;
- iOS rendering/device bridges.

### 4.3 XNA boundary

XNA applications are recognized by XAPSCAN1 but full XNA Graphics is intentionally deferred to a later phase.

Initial runtime work should prioritize managed Silverlight applications because that is the smallest path to proving binary WP7 application compatibility.

---

## 5. Milestone map

```text
XAPSCAN1                 GREEN
    |
    v
ILRUN1 build             GREEN
    |
    v
ILRUN1 physical device   CURRENT
    |
    v
BIND1                     NEXT after execution-engine proof
    |
    v
XAML1
    |
    v
PAGE1
    |
    v
NAV1
    |
    v
PHONEAPI1
    |
    v
WPUI1
    |
    v
DEVICE1
    |
    v
XNA1                     PHASE 2
```

---

## 6. XAPSCAN1 — completed

### Goal

Statically inspect an original WP7 `.xap` and produce the dependency contract required by the later runtime.

### Implemented outputs

XAPSCAN1 currently extracts:

- package SHA-256;
- ZIP inventory;
- `AppManifest.xaml` / `AppManifest.xml`;
- `WMAppManifest.xml`;
- runtime version;
- EntryPointAssembly;
- EntryPointType;
- Deployment.Parts;
- DefaultTask / NavigationPage;
- capabilities;
- managed DLL/EXE assembly identity;
- assembly version/culture/public-key token;
- CLR metadata version;
- MVID;
- IL-only vs mixed-mode status;
- AssemblyRef;
- TypeRef;
- MemberRef;
- managed resources;
- P/Invoke declarations;
- XNA Graphics dependencies;
- compatibility classification tags;
- deterministic findings / Failure Oracle output.

### Synthetic regression

Workflow:

`WP7 XAPSCAN1`

The workflow builds a controlled managed assembly, packages it into a synthetic WP7-style XAP, scans it, and verifies the report contract.

Status: **GREEN**.

Migration validation run in standalone repo:

`36245134465` — SUCCESS.

### Real WP7 regression

Workflow:

`WP7 XAPSCAN1 Real XAP`

The regression fixture is the MIT-licensed **Aleterated** WP7/XNA project, pinned to an upstream commit and downloaded during CI.

The XAP is not vendored into this repository.

Verified values include:

```text
RuntimeVersion = 3.0
EntryPointAssembly = Aleterated
EntryPointType = Aleterated.Game1
RuntimeType = XNA
```

The primary assembly is managed and IL-only.

The scanner correctly identifies references including:

```text
Microsoft.Phone
Microsoft.Xna.Framework
Microsoft.Xna.Framework.Game
Microsoft.Xna.Framework.Graphics
Microsoft.Xna.Framework.Input.Touch
System.Windows
```

and correctly classifies the package with XNA-related requirements.

Standalone-repo migration validation:

`36245134665` — SUCCESS.

---

## 7. ILRUN1 — purpose

ILRUN1 is the first decisive runtime experiment.

It answers:

> Can the Mono/.NET iOS runtime load and execute managed IL from an assembly that was not statically linked into the iOS application?

This is critical because an original WP7 XAP contains assemblies that cannot be known to the IPA at compile time.

### Controlled payload

`src/IlPayload/IlPayload.csproj`

builds:

`IlPayload.dll`

The DLL is deliberately:

- built separately;
- copied into the IPA only as a raw `BundleResource`;
- not added as a `ProjectReference`;
- not added as a normal managed `Reference`.

The runtime path is:

```text
read IlPayload.dll bytes
    -> Assembly.Load(byte[])
    -> resolve IlPayload.EntryPoint
    -> resolve Run()
    -> invoke through reflection
    -> expect XAP_ILRUN1_PASS:42
```

The payload also exercises a simple generic managed type.

### iOS build configuration

Current device build:

- `net10.0-ios`;
- `ios-arm64`;
- minimum iOS **15.0**;
- Mono interpreter enabled with `UseInterpreter=true`;
- NativeAOT not used for the payload execution experiment;
- unsigned IPA produced for later ESign re-signing/device installation;
- CI uses Xcode 26.0.1.

### CI build

Workflow:

`WP7 ILRUN1 iOS`

Migration validation in the standalone repo:

`36245137267` — SUCCESS.

Later persistent-log build with the newest runtime markers:

`36245698771` — SUCCESS.

Artifact produced by that build:

`WP7-ILRUN1-ios15-unsigned`

Artifact ID:

`10907412082`

---

## 8. First physical-device evidence

Device evidence from the first ILRUN1 build:

- IPA can be signed and installed through ESign;
- application launches;
- application does **not** crash to Home;
- process remains alive;
- user can background the application and foreground it again;
- application content remains black;
- system status bar / home indicator remain visible.

The user supplied a screen recording showing this behavior.

This is materially different from a startup crash.

### Current hypothesis

The original implementation launched the risky ILRUN1 probe synchronously from `ViewDidAppear`.

If `Assembly.Load(byte[])` blocks before UIKit presents the first rendered application frame, the user can see a persistent black screen while the process remains alive.

This is currently a **working hypothesis**, not a proven root cause.

The next build was changed specifically to distinguish UI-startup failure from runtime-loader blocking.

---

## 9. ILRUN1-IOS2-PERSISTLOG — current device-test build

The current code moves the IL runtime probe away from the UIKit main thread.

Conceptually:

```text
UIKit main thread
    |
    +--> construct + display diagnostic UI
    |
    +--> start background runtime probe
             |
             +--> read payload
             +--> Assembly.Load
             +--> resolve type
             +--> resolve method
             +--> invoke
```

This lets UIKit render even if the runtime experiment blocks.

### Persistent logging

The app now creates:

```text
Documents/
└── WP7RunnerLogs/
    ├── WP7Runner_TakeThis.log
    ├── WP7Runner_Persistent.log
    └── WP7Runner_Persistent-prev.log
```

Logging is independent from the on-screen UI.

The logger writes each marker synchronously as the runtime advances.

### Files app access

`Info.plist` enables:

```text
UIFileSharingEnabled = true
LSSupportsOpeningDocumentsInPlace = true
```

The intent is to let the tester retrieve logs from the iOS Files application even if the app UI is unavailable.

### Lifecycle markers

The build now records application startup and foreground/background boundaries, including:

```text
[APP][MAIN_ENTER]
[APP][UIApplication.Main_BEGIN]
[APP][APPDELEGATE_CTOR]
[APP][FINISHED_LAUNCHING_ENTER]
[APP][WINDOW_READY]
[APP][ROOT_VC]
[APP][FINISHED_LAUNCHING_EXIT]
[APP][ON_ACTIVATED]
[APP][ON_RESIGN_ACTIVATION]
[APP][DID_ENTER_BACKGROUND]
[APP][WILL_ENTER_FOREGROUND]
```

### UI markers

```text
[UI][VIEW_DID_LOAD_ENTER]
[UI][VIEW_DID_LOAD_EXIT]
[UI][VIEW_DID_APPEAR]
[UI][PROBE_BACKGROUND_START]
```

### Critical runtime boundaries

```text
[ILRUN1][START]
[ILRUN1][BUILD] ILRUN1-IOS2-PERSISTLOG
[ILRUN1][FILE_FOUND]
[ILRUN1][FILE_READ_OK]
[ILRUN1][ASSEMBLY_LOAD_BEGIN]
[ILRUN1][ASSEMBLY_LOAD_OK]
[ILRUN1][TYPE_RESOLVE_OK]
[ILRUN1][METHOD_RESOLVE_OK]
[ILRUN1][METHOD_INVOKE_BEGIN]
[ILRUN1][METHOD_INVOKE_OK]
[ILRUN1][PASS]
[ILRUN1][END] PASS
```

### Hang watchdog

If a background probe remains active, watchdog markers are written:

```text
[ILRUN1][WATCHDOG] probe_still_running seconds=5
[ILRUN1][WATCHDOG] probe_still_running seconds=15
[ILRUN1][WATCHDOG] probe_still_running seconds=30
```

Example interpretation:

```text
ASSEMBLY_LOAD_BEGIN
WATCHDOG 5
WATCHDOG 15
WATCHDOG 30
```

with no `ASSEMBLY_LOAD_OK` would identify dynamic assembly loading as the current blocking boundary.

---

## 10. Failure Oracle philosophy

Do not debug this project from the visual symptom alone.

Every milestone should produce a narrow deterministic boundary marker.

Examples:

```text
[XAPSCAN1][MISSING_APP_MANIFEST]
[XAPSCAN1][ENTRY_ASSEMBLY_UNRESOLVED]
[XAPSCAN1][PINVOKE_PRESENT]
[XAPSCAN1][XNA_GRAPHICS_REQUIRED]

[ILRUN1][FILE_NOT_FOUND]
[ILRUN1][ASSEMBLY_LOAD_BEGIN]
[ILRUN1][ASSEMBLY_LOAD_OK]
[ILRUN1][TYPE_RESOLVE_FAIL]
[ILRUN1][METHOD_RESOLVE_FAIL]
[ILRUN1][METHOD_INVOKE_BEGIN]
[ILRUN1][INVOKE_EXCEPTION]
[ILRUN1][WATCHDOG]
```

Later runtime phases should preserve this style:

```text
[BIND1][ASSEMBLY_BIND_FAIL]
[BIND1][TYPE_FORWARD_FAIL]

[XAML1][UNKNOWN_TYPE]
[XAML1][UNKNOWN_PROPERTY]
[XAML1][MISSING_RESOURCE]

[PAGE1][PAGE_VISIBLE]
[NAV1][NAVIGATE_OK]
```

---

## 11. GO / REDESIGN / STOP gates

### GO

Continue toward BIND1 when a physical device proves:

```text
ASSEMBLY_LOAD_OK
TYPE_RESOLVE_OK
METHOD_RESOLVE_OK
METHOD_INVOKE_OK result=XAP_ILRUN1_PASS:42
END PASS
```

This proves the current interpreter path can execute externally supplied managed IL.

### REDESIGN execution host

If the app is stable but external managed assemblies cannot be executed with the current runtime path, investigate in order:

1. alternate managed load context / loading route where supported;
2. different Mono interpreter hosting configuration;
3. explicit embedded runtime host;
4. custom ECMA-335 interpreter if necessary.

Do not build BIND1/XAML1 deeply until the execution-engine question is settled.

### Scope change / STOP for binary compatibility

Only consider moving from binary-XAP compatibility toward source-level recompilation if external legacy managed code proves impractical after the runtime-host alternatives have been tested.

---

## 12. BIND1 — planned next milestone

After ILRUN1 device PASS, build the first legacy assembly resolver.

Primary identities of interest include:

```text
mscorlib
System
System.Core
System.Windows
Microsoft.Phone
Microsoft.Phone.Interop
Microsoft.Xna.Framework     (later/partial)
```

BIND1 should not immediately implement the full assemblies.

It should first:

1. inspect requested assembly identity/version/public-key token;
2. route known WP7 identities to compatibility assemblies;
3. emit exact bind diagnostics;
4. permit a minimal original WP7 assembly to load far enough to expose the first missing type/member.

Expected markers:

```text
[BIND1][REQUEST]
[BIND1][REDIRECT]
[BIND1][RESOLVE_OK]
[BIND1][ASSEMBLY_BIND_FAIL]
[BIND1][MISSING_TYPE]
[BIND1][MISSING_MEMBER]
```

---

## 13. Later milestones

### XAML1

Minimum Silverlight UI substrate:

- DependencyObject;
- DependencyProperty;
- basic ResourceDictionary;
- Grid;
- StackPanel;
- Canvas;
- TextBlock;
- Button;
- Image;
- basic Binding/DataContext;
- XAML type/property resolution.

### PAGE1

Prove:

```text
Application
  -> PhoneApplicationFrame
  -> PhoneApplicationPage
  -> visible first page
```

### NAV1

Implement:

- NavigationPage from manifest;
- navigation URI;
- basic back stack;
- Back;
- query-string parameters.

### PHONEAPI1

Add only APIs required by selected real target applications, beginning with low-complexity managed APIs.

### DEVICE1

Bridge selected phone APIs to iOS:

- storage;
- touch/keyboard;
- network;
- location;
- sensors;
- audio;
- camera.

### XNA1

Separate later phase because full XNA compatibility requires a substantial graphics/game runtime.

---

## 14. Current test instruction

For the current `ILRUN1-IOS2-PERSISTLOG` build:

1. download the latest `WP7-ILRUN1-ios15-unsigned` artifact;
2. sign the IPA with ESign;
3. install it on the physical iPhone;
4. launch it;
5. leave it active for at least 30-40 seconds;
6. background/foreground if useful;
7. open iOS Files;
8. locate the app's Documents folder;
9. retrieve:

```text
WP7RunnerLogs/WP7Runner_TakeThis.log
WP7RunnerLogs/WP7Runner_Persistent.log
```

If the app crashes unexpectedly, also collect the iOS `.ips` crash report.

---

## 15. Current known status

At this checkpoint:

- standalone WP7 repository established: **YES**;
- project separated from EKA2L1/Symbian: **YES**;
- XAPSCAN1 synthetic regression: **GREEN**;
- XAPSCAN1 real WP7 regression: **GREEN**;
- iOS 15 arm64 ILRUN1 build: **GREEN**;
- raw external payload bundled without static ProjectReference: **VERIFIED BY CI**;
- physical iPhone launches app: **YES**;
- first build crash-to-Home: **NO**;
- first build black application content: **YES**;
- process survives background/foreground: **YES**;
- persistent file logging: **IMPLEMENTED**;
- background-thread IL probe: **IMPLEMENTED**;
- current device result for IOS2 persistent-log build: **PENDING USER TEST**;
- BIND1: **NOT STARTED**;
- XAML1: **NOT STARTED**.

---

## 16. Source-of-truth rule

When continuing this project in a new ChatGPT conversation:

1. read this file first;
2. inspect the latest GitHub Actions runs on `main`;
3. inspect the newest device logs supplied by the user;
4. continue from the first new Failure Oracle boundary;
5. do not re-investigate earlier milestones unless new evidence invalidates them.

Canonical handoff:

`docs/handoff/CURRENT.md`
