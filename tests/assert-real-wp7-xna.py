#!/usr/bin/env python3
import json
import pathlib
import re
import sys


EXPECTED_PACKAGE_SHA256 = "69bd93627f4fa2c567748e36c3487c083db95c0efe01ef203bf60179bf9342a1"


def _require(condition, message):
    if not condition:
        raise ValueError(message)


def _required_mapping(parent, key, context):
    value = parent.get(key)
    _require(isinstance(value, dict), f"{context}.{key} must be an object")
    return value


def validate_report(report: dict) -> None:
    """Validate the pinned real-WP7 report and its loadable entry identity."""
    _require(isinstance(report, dict), "report must be an object")
    _require(report.get("scanner") == "XAPSCAN1", "scanner must be XAPSCAN1")

    package = _required_mapping(report, "package", "report")
    _require(package.get("sha256") == EXPECTED_PACKAGE_SHA256, "package SHA-256 does not match the pinned fixture")

    app = _required_mapping(report, "appManifest", "report")
    _require(app.get("runtimeVersion") == "3.0", "runtime version must be 3.0")
    _require(app.get("entryPointAssembly") == "Aleterated", "entry assembly must be Aleterated")
    _require(app.get("entryPointType") == "Aleterated.Game1", "entry type must be Aleterated.Game1")

    wm_app = _required_mapping(report, "wmAppManifest", "report")
    _require(wm_app.get("runtimeType") == "XNA", "runtime type must be XNA")

    entry_point = _required_mapping(report, "entryPoint", "report")
    _require(entry_point.get("assemblyResolved") is True, "entry assembly must resolve")
    _require(entry_point.get("typeResolved") is True, "entry type must resolve")

    assemblies = report.get("assemblies")
    _require(isinstance(assemblies, list), "assemblies must be an array")
    matches = [a for a in assemblies if isinstance(a, dict) and a.get("name") == "Aleterated"]
    _require(len(matches) == 1, "expected exactly one Aleterated assembly")
    main = matches[0]

    path = main.get("path")
    _require(isinstance(path, str) and path.strip(), "entry assembly path is required")
    files = report.get("files")
    _require(isinstance(files, list), "files must be an array")
    package_paths = {item.get("path") for item in files if isinstance(item, dict)}
    _require(path in package_paths, "entry assembly path must exist in package files")
    version = main.get("version")
    _require(isinstance(version, str) and re.fullmatch(r"\d+(?:\.\d+){1,3}", version) is not None,
             "entry assembly version is required")
    _require("culture" in main, "entry assembly culture field is required")
    culture = main["culture"]
    _require(culture is None or (isinstance(culture, str) and culture.strip()),
             "entry assembly culture must be neutral/null or a non-empty string")
    _require("publicKeyToken" in main, "entry assembly publicKeyToken field is required")
    token = main["publicKeyToken"]
    _require(token is None or (isinstance(token, str) and re.fullmatch(r"[0-9a-fA-F]{16}", token) is not None),
             "entry assembly publicKeyToken must be null or 16 hexadecimal characters")
    _require(main.get("isManaged") is True, "entry assembly must be managed")
    _require(main.get("isIlOnly") is True, "entry assembly must be IL-only")
    _require(main.get("isMixedMode") is False, "entry assembly must not be mixed-mode")
    type_definitions = main.get("typeDefinitions")
    _require(isinstance(type_definitions, list), "entry assembly typeDefinitions must be an array")
    _require(app["entryPointType"] in type_definitions, "entry type must be defined by the entry assembly")

    tag_values = report.get("compatibilityTags")
    _require(isinstance(tag_values, list), "compatibilityTags must be an array")
    tags = set(tag_values)
    _require({"NEEDS_XNA_FULL", "SILVERLIGHT", "WP7_PHONE_API"} <= tags,
             "expected WP7/XNA compatibility tags")

    main_refs = main.get("assemblyReferences")
    _require(isinstance(main_refs, list), "entry assembly assemblyReferences must be an array")
    refs = {(x.get("name"), x.get("version")) for x in main_refs if isinstance(x, dict)}
    for required in (
        ("Microsoft.Xna.Framework", "4.0.0.0"),
        ("Microsoft.Xna.Framework.Game", "4.0.0.0"),
        ("Microsoft.Xna.Framework.Graphics", "4.0.0.0"),
        ("Microsoft.Xna.Framework.Input.Touch", "4.0.0.0"),
    ):
        _require(required in refs, f"missing assembly reference {required[0]} {required[1]}")

    phone_refs = {
        x.get("version")
        for assembly in assemblies
        if isinstance(assembly, dict)
        for x in assembly.get("assemblyReferences", [])
        if isinstance(x, dict) and x.get("name") == "Microsoft.Phone"
    }
    _require("7.0.0.0" in phone_refs, "Microsoft.Phone 7.0.0.0 reference is required")

    finding_values = report.get("findings")
    _require(isinstance(finding_values, list), "findings must be an array")
    findings = {x.get("code") for x in finding_values if isinstance(x, dict)}
    _require("XNA_GRAPHICS_REQUIRED" in findings, "XNA graphics finding is required")
    _require("PINVOKE_PRESENT" not in findings, "fixture must not contain P/Invoke")
    _require("MIXED_MODE_IMAGE" not in findings, "fixture must not contain mixed-mode images")


def main() -> int:
    if len(sys.argv) != 2:
        print("Usage: assert-real-wp7-xna.py <xapscan-report.json>", file=sys.stderr)
        return 2
    try:
        report = json.loads(pathlib.Path(sys.argv[1]).read_text(encoding="utf-8"))
        validate_report(report)
    except (OSError, json.JSONDecodeError, ValueError) as exc:
        print(f"XAPSCAN1 real WP7 XNA contract: FAIL: {exc}", file=sys.stderr)
        return 1
    print("XAPSCAN1 real WP7 XNA contract: PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
