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
assert '<ProjectReference Include="../Wp7Binding/Wp7Binding.csproj" />' in project, "host must reference the managed BIND1 resolver"
assert '<Reference Include="IlPayload' not in project and 'ProjectReference Include="../IlPayload/' not in project, "payload must not become a static managed reference"
assert '<Reference Include="Aleterated' not in project and 'ProjectReference Include="../Aleterated/' not in project, "WP7 fixture assemblies must remain opaque raw data"
assert '<BundleResource Include="Assets/Aleterated.xap" LogicalName="Aleterated.xap" />' in project, "XAP must remain a raw bundle resource"
assert '<BundleResource Include="Assets/Aleterated.xapscan1.json" LogicalName="Aleterated.xapscan1.json" />' in project, "scanner report must remain a raw bundle resource"
assert "[APP][BUILD] ILRUN1-NET9-ROOTSR" in main, "device logs must identify this experiment"
assert "ILRUN1-NET9-ROOTSR" in app_delegate, "visible UI must identify this experiment"
print("ILRUN1 interpreter/raw-payload contract: PASS")

assert "run-name: ${{ inputs.test_label || 'V6' }} | WP7 ILRUN1 iOS" in workflow, "workflow run must carry the external test label"
assert "name: WP7-ILRUN1-${{ inputs.test_label || 'V6' }}-ios15-unsigned" in workflow, "artifact must carry the external test label"
assert "WP7-ILRUN1-${TEST_LABEL}-ios15-unsigned.ipa" in workflow, "IPA filename must carry the external test label"
assert "description: External test label; does not change the app version." in workflow, "test label must be distinct from app version"
assert "path: artifacts/WP7-ILRUN1-${{ inputs.test_label || 'V6' }}-ios15-unsigned.ipa" in workflow, "upload path must use the workflow label expression"
