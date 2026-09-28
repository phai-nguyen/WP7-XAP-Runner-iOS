import json
import runpy
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch


ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "tests" / "assert-real-wp7-xna.py"
EXPECTED_SHA256 = "69bd93627f4fa2c567748e36c3487c083db95c0efe01ef203bf60179bf9342a1"


def valid_report():
    return {
        "scanner": "XAPSCAN1",
        "package": {"sha256": EXPECTED_SHA256},
        "appManifest": {
            "runtimeVersion": "3.0",
            "entryPointAssembly": "Aleterated",
            "entryPointType": "Aleterated.Game1",
        },
        "wmAppManifest": {"runtimeType": "XNA"},
        "entryPoint": {"assemblyResolved": True, "typeResolved": True},
        "files": [{"path": "Aleterated.dll"}],
        "compatibilityTags": ["NEEDS_XNA_FULL", "SILVERLIGHT", "WP7_PHONE_API"],
        "assemblies": [
            {
                "path": "Aleterated.dll",
                "name": "Aleterated",
                "version": "1.0.0.0",
                "culture": None,
                "publicKeyToken": None,
                "isManaged": True,
                "isIlOnly": True,
                "isMixedMode": False,
                "typeDefinitions": ["Aleterated.Game1"],
                "assemblyReferences": [
                    {"name": "Microsoft.Xna.Framework", "version": "4.0.0.0"},
                    {"name": "Microsoft.Xna.Framework.Game", "version": "4.0.0.0"},
                    {"name": "Microsoft.Xna.Framework.Graphics", "version": "4.0.0.0"},
                    {"name": "Microsoft.Xna.Framework.Input.Touch", "version": "4.0.0.0"},
                ],
            },
            {"name": "Phone.API", "assemblyReferences": [{"name": "Microsoft.Phone", "version": "7.0.0.0"}]},
        ],
        "findings": [{"code": "XNA_GRAPHICS_REQUIRED"}],
    }


def load_validator(report):
    with tempfile.NamedTemporaryFile(mode="w", encoding="utf-8", suffix=".json") as source:
        json.dump(valid_report(), source)
        source.flush()
        with patch.object(sys, "argv", [str(SCRIPT), source.name]):
            namespace = runpy.run_path(str(SCRIPT))
    validator = namespace.get("validate_report")
    if validator is None:
        raise AssertionError("validate_report is not implemented")
    return validator


class RealWp7XnaContractTests(unittest.TestCase):
    def test_rejects_entry_without_public_key_token_field(self):
        report = valid_report()
        del report["assemblies"][0]["publicKeyToken"]

        with self.assertRaisesRegex(ValueError, "publicKeyToken"):
            load_validator(report)(report)

    def test_rejects_entry_missing_path_version_or_culture(self):
        for field in ("path", "version", "culture"):
            with self.subTest(field=field):
                report = valid_report()
                del report["assemblies"][0][field]
                with self.assertRaisesRegex(ValueError, field):
                    load_validator(report)(report)

    def test_rejects_entry_that_is_not_managed_il_only(self):
        report = valid_report()
        report["assemblies"][0]["isIlOnly"] = False

        with self.assertRaisesRegex(ValueError, "IL-only"):
            load_validator(report)(report)

    def test_accepts_pinned_entry_identity(self):
        report = valid_report()

        load_validator(report)(report)


if __name__ == "__main__":
    unittest.main()
