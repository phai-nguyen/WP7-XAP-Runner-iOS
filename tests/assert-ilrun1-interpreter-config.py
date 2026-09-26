from pathlib import Path

root = Path(__file__).resolve().parents[1]
project = (root / "src/ILRun1Host/ILRun1Host.csproj").read_text(encoding="utf-8")
workflow = (root / ".github/workflows/wp7-ilrun1-ios.yml").read_text(encoding="utf-8")
main = (root / "src/ILRun1Host/Main.cs").read_text(encoding="utf-8")
app_delegate = (root / "src/ILRun1Host/AppDelegate.cs").read_text(encoding="utf-8")

assert "<MtouchInterpreter>all</MtouchInterpreter>" in project, "host project must interpret all assemblies"
assert '<TrimmerRootAssembly Include="System.Runtime" />' in project, "System.Runtime facade must be preserved for dynamically loaded payload metadata"
assert "-p:MtouchInterpreter=all" in workflow, "device publish must use all-assemblies interpreter mode"
assert '<BundleResource Include="Assets/IlPayload.dll" LogicalName="IlPayload.dll" />' in project, "payload must remain a raw bundle resource"
assert "<ProjectReference" not in project and '<Reference Include="IlPayload' not in project, "payload must not become a static managed reference"
assert "[APP][BUILD] ILRUN1-NET9-ROOTSR" in main, "device logs must identify this experiment"
assert "ILRUN1-NET9-ROOTSR" in app_delegate, "visible UI must identify this experiment"
print("ILRUN1 interpreter/raw-payload contract: PASS")
