from pathlib import Path

root = Path(__file__).resolve().parents[1]
project = (root / "src/ILRun1Host/ILRun1Host.csproj").read_text(encoding="utf-8")
workflow = (root / ".github/workflows/wp7-ilrun1-ios.yml").read_text(encoding="utf-8")
main = (root / "src/ILRun1Host/Main.cs").read_text(encoding="utf-8")
app_delegate = (root / "src/ILRun1Host/AppDelegate.cs").read_text(encoding="utf-8")
main_view_controller = (root / "src/ILRun1Host/MainViewController.cs").read_text(encoding="utf-8")

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
assert "window.RootViewController = new MainViewController()" in app_delegate, "the app must show the interactive controller containing the BIND1 probe button"
assert 'Text = "WP7 XAP Runner — ILRUN1"' in main_view_controller, "the interactive screen must retain the app's ILRUN1 identity"
assert '_bindButton.SetTitle("BIND1 Probe", UIControlState.Normal)' in main_view_controller, "the visible interactive screen must expose the BIND1 probe"
print("ILRUN1 interpreter/raw-payload contract: PASS")

assert "run-name: ${{ inputs.test_label || 'V9' }} | WP7 BIND1 iOS" in workflow, "workflow run must carry the external V9 test label"
assert "default: V9" in workflow, "dispatch label must default to V9"
assert "name: WP7-BIND1-${{ inputs.test_label || 'V9' }}-ios15-unsigned" in workflow, "artifact must carry the BIND1 V9 external test label"
assert "WP7-BIND1-${TEST_LABEL}-ios15-unsigned.ipa" in workflow, "IPA filename must carry the BIND1 V9 external test label"
assert "description: External test label; does not change the app version." in workflow, "test label must be distinct from app version"
assert "path: artifacts/WP7-BIND1-${{ inputs.test_label || 'V9' }}-ios15-unsigned.ipa" in workflow, "upload path must use the workflow label expression"
assert "dotnet run --project tests/Wp7Binding.Tests/Wp7Binding.Tests.csproj -c Release" in workflow, "all resolver tests must pass before upload"
assert 'test -s "$APP/Aleterated.xap"' in workflow, "IPA must contain the pinned XAP"
assert 'test -s "$APP/Aleterated.xapscan1.json"' in workflow, "IPA must contain the scanner report"
assert 'test "$APP_TITLE" = "WP7 ILRUN1"' in workflow, "external label must not change the display name"
assert 'test "$APP_ID" = "com.phai.wp7.ilrun1"' in workflow, "external label must not change the app identity"
assert 'test "$APP_VERSION" = "1"' in workflow, "external label must not change ApplicationVersion"
assert 'test "$APP_DISPLAY_VERSION" = "0.1"' in workflow, "external label must not change ApplicationDisplayVersion"

assert '<ApplicationTitle>WP7 ILRUN1</ApplicationTitle>' in project
assert '<ApplicationId>com.phai.wp7.ilrun1</ApplicationId>' in project
assert '<ApplicationVersion>1</ApplicationVersion>' in project
assert '<ApplicationDisplayVersion>0.1</ApplicationDisplayVersion>' in project
