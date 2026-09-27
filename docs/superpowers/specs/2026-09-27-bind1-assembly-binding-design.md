# BIND1 Design — WP7 Assembly Binding Probe

**Status:** Design draft for user review  
**Date:** 2026-09-27  
**Repository:** `phai-nguyen/WP7-XAP-Runner-iOS`

## Purpose and constraints

ILRUN1-NET9-ROOTSR has now passed on a physical iPhone running iOS 18.7: the device loaded the external managed payload, resolved `IlPayload.EntryPoint.Run()`, and returned `XAP_ILRUN1_PASS:42`. BIND1 is the next milestone.

BIND1 will establish a deterministic managed-assembly binding boundary for original WP7 package assemblies. The first target is the existing pinned, MIT-licensed Aleterated WP7/XNA regression fixture. It is a binding probe only: no XNA graphics implementation is implied. The application label V6 is an external test-build label and must not alter the app's display name or application version.

The project keeps its target-first strategy: load a real package assembly, report the first unsupported boundary precisely, and implement only later-approved compatibility behavior. BIND1 must not claim arbitrary XAP support or successful game execution.

## Approaches considered

1. **Extend XAPSCAN1 only.** This would list AssemblyRef/TypeRef metadata but would not exercise the iOS runtime's actual binding behavior.
2. **Use the existing raw ILRUN1 payload only.** This has proven dynamic IL execution, but it does not represent a real WP7 application dependency graph.
3. **Add a small runtime binding probe around the real fixture (selected).** Reuse the pinned fixture and scanner findings, load its original managed assembly on-device, and observe the runtime's exact bind/type/member boundary. This adds the needed runtime evidence while keeping XNA, XAML, and UI emulation out of scope.

## Architecture and data flow

1. CI obtains the already-pinned Aleterated source using the existing real-WP7 scanner workflow; the XAP is not vendored.
2. BIND1 consumes the original managed entry assembly and its private managed dependencies from the fixture package. It records the assembly identity (simple name, version, culture, public-key token) before attempting load/resolve.
3. A dedicated binding component applies exact-identity resolution in a deterministic order: package-private assemblies first, then an explicit compatibility redirect table, then the platform/runtime resolver. Redirects are allowed only to a present, compatible target; there is no silent version/name-only fallback.
4. The probe resolves the fixture entry type and inspects its immediate referenced types/members far enough to surface the first unsupported binding boundary. It does not invoke game rendering or native XNA APIs.
5. The iOS host displays a concise PASS/FAIL result and saves the detailed diagnostic log, preserving the proven ILRUN1 startup path.

The resolver is isolated behind an interface so the resolution order and identity decisions can be tested without launching UIKit. XAPSCAN1 remains the static package/reporting layer; BIND1 owns runtime load and resolution evidence.

## Diagnostic contract

Each probe run uses stable, line-oriented records with enough identity data to reproduce decisions:

- `[BIND1][REQUEST] name=… version=… culture=… pkt=…`
- `[BIND1][REDIRECT] from=… to=… reason=…`
- `[BIND1][RESOLVE_OK] requested=… resolved=… source=package|compat|runtime`
- `[BIND1][ASSEMBLY_BIND_FAIL] requested=… exception=…`
- `[BIND1][MISSING_TYPE] type=… assembly=…`
- `[BIND1][MISSING_MEMBER] member=… type=… assembly=…`
- `[BIND1][END] PASS|FAIL`

Identity values must be escaped or encoded so each marker remains a single parseable log line. A failed bind/type/member is a controlled probe result, not an unhandled app crash. The earliest failure is reported deterministically; later failures may be omitted in this first pass.

## Scope and exclusions

Included:
- fixture acquisition through the existing pinned workflow;
- runtime assembly identity capture and exact-identity resolution;
- explicit, auditable redirect decisions;
- deterministic first-missing-boundary diagnostics;
- unit/regression coverage for identity matching, resolution order, redirects, and marker output;
- an iOS test artifact with an external V-label only.

Excluded:
- implementing XNA Graphics or invoking the XNA game loop;
- Silverlight/XAML loading, page navigation, rendering, or `Microsoft.Phone` APIs;
- broad version unification, automatic binding redirects, or pretending absent assemblies exist;
- changing app identity, app display name, or `ApplicationVersion`/`ApplicationDisplayVersion`;
- changing the validated .NET/iOS toolchain or revisiting the rejected .NET 10 experiments.

## Validation and acceptance

BIND1 is ready for physical-device evaluation when:

1. CI builds the fixture probe and its tests verify exact identity comparisons, deterministic resolution precedence, valid redirects, and stable failure markers.
2. On the iOS 18.7 device, the fixture entry assembly is found/read and its identity is logged.
3. The probe either resolves an actual dependency with a truthful `RESOLVE_OK` marker or emits the first actual unsupported identity/type/member as a controlled `*_FAIL` marker followed by `[BIND1][END] FAIL`; it remains alive and saves the log.
4. A BIND1 probe is considered diagnostically green only when the device log identifies the real first binding boundary without a crash. This is not a claim that the WP7 app itself runs; compatibility work proceeds from that reported boundary.

No compatibility redirect is accepted merely because the fixture references an assembly. Every target must be present and explicitly justified by API compatibility; otherwise the binding failure is the correct result.

## Open implementation constraints

- Keep the primary test baseline at .NET 9.0.303, Microsoft.iOS 18.5.9207, Xcode 16.4, iOS SDK 18.5, minimum iOS 15.0, and primary device iOS 18.7.
- Preserve the existing no-static-reference/raw-payload ILRUN1 proof unless a concrete BIND1 requirement demonstrates that the host contract must evolve.
- Do not add fixture binaries to the repository; use the existing pinned acquisition mechanism and validate the upstream pin before packaging.
