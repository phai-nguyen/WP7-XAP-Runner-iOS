# ILRUN1 checkpoint — iOS external managed IL execution probe

Status: **BUILD GREEN / DEVICE TEST REQUIRED**

## Goal

Prove on a physical iPhone that the Mono interpreter can execute managed IL from an assembly that was not statically referenced or AOT-compiled into the iOS host.

## Contract

`IlPayload.dll` is built separately as `netstandard2.0`, copied into the app bundle only as `BundleResource`, and is deliberately not a `ProjectReference` or assembly `Reference`.

At runtime the host performs:

1. read `IlPayload.dll` bytes from the app bundle;
2. `Assembly.Load(byte[])`;
3. resolve `IlPayload.EntryPoint`;
4. resolve public static `Run()`;
5. invoke with reflection;
6. require exact result `XAP_ILRUN1_PASS:42`.

The payload creates and mutates a generic managed type before returning the result.

## Required device markers

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

Any missing marker is the new failure boundary.

## iOS build contract

- Target: `net10.0-ios`
- Device RID: `ios-arm64`
- Minimum iOS: **15.0**
- Mono interpreter: `UseInterpreter=true`
- NativeAOT: not used
- Payload: raw bundle file, no static assembly reference
- CI Xcode: 26.0.1
- Distribution for this experiment: unsigned IPA, intended to be re-signed with ESign for device testing.

## Next gates

- **PASS on device** → start BIND1: legacy assembly identity/binding layer for `mscorlib`, `System.Windows`, `Microsoft.Phone`.
- `Assembly.Load` fails → test stream/AssemblyLoadContext path, then embedded Mono/runtime-host alternatives.
- Assembly loads but invocation fails → classify missing runtime/BCL/generic/reflection dependency and reduce the payload to the failing IL feature.


## Device evidence — ILRUN1-IOS1

Physical iPhone test on 2026-09-26:

- app installs and launches successfully after ESign signing;
- process remains alive and can be backgrounded/foregrounded;
- no crash was observed;
- app content remains black, with system status/home UI still present.

Working hypothesis: the first ILRUN1 probe was invoked synchronously from `ViewDidAppear`; if `Assembly.Load(byte[])` blocks, UIKit may not get a chance to render the first application frame.

## ILRUN1-IOS2-PERSISTLOG

The next device build moves the risky probe to a background task and adds synchronous persistent logging before each blocking boundary.

Logs are written to:

```text
Documents/
└── WP7RunnerLogs/
    ├── WP7Runner_TakeThis.log
    ├── WP7Runner_Persistent.log
    └── WP7Runner_Persistent-prev.log
```

The app enables iOS file sharing so the directory can be retrieved from Files even when the in-app UI is unavailable.

New decisive markers include:

```text
[ILRUN1][ASSEMBLY_LOAD_BEGIN]
[ILRUN1][ASSEMBLY_LOAD_OK]
[ILRUN1][METHOD_INVOKE_BEGIN]
[ILRUN1][METHOD_INVOKE_OK]
[ILRUN1][WATCHDOG] probe_still_running seconds=5
[ILRUN1][WATCHDOG] probe_still_running seconds=15
[ILRUN1][WATCHDOG] probe_still_running seconds=30
```

If `ASSEMBLY_LOAD_BEGIN` is present but `ASSEMBLY_LOAD_OK` never appears while watchdog markers continue, the current Mono/iOS runtime is blocking inside dynamic assembly loading rather than in UIKit startup.
