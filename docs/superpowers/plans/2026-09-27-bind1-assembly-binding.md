# Kế hoạch triển khai BIND1 — Probe liên kết assembly WP7

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Mục tiêu:** Nạp assembly Aleterated WP7/XNA gốc trong host iOS, ghi nhận chính xác kết quả liên kết assembly và báo ranh giới tương thích đầu tiên mà không crash.

**Kiến trúc:** Tạo thư viện resolver .NET độc lập UIKit để so khớp identity chính xác, giải quyết assembly trong gói và áp dụng redirect tường minh. Workflow CI lấy XAP Aleterated theo pin hiện có, kiểm tra SHA-256, đóng gói như tài nguyên thô; host iOS dùng resolver khi dò entry type và ghi log BIND1. Giữ nguyên đường ILRUN1 và không triển khai XNA.

**Công nghệ:** C#/.NET 9, .NET for iOS, reflection/AssemblyResolve, ZIP/XAP, workflow GitHub Actions, test console C# không thêm gói NuGet ngoài.

**Spec:** `docs/superpowers/specs/2026-09-27-bind1-assembly-binding-design.md`

## Ràng buộc chung

- .NET 9.0.303, Microsoft.iOS 18.5.9207, Xcode 16.4, iOS SDK 18.5.
- Minimum iOS 15.0; thiết bị chính để thử là iOS 18.7.
- Giữ nguyên ILRUN1 dùng payload thô; tuyệt đối không thêm tham chiếu tĩnh tới `IlPayload.dll`.
- Fixture Aleterated lấy từ URL/commit đã ghim trong workflow; không commit XAP/DLL fixture vào repo.
- Nhãn build kế tiếp là V7, chỉ áp dụng ngoài ứng dụng; không đổi app identity, tên hiển thị, `ApplicationVersion` hoặc `ApplicationDisplayVersion`.
- Lỗi liên kết/type/member là kết quả probe có kiểm soát, không phải crash.
- Không triển khai XNA Graphics, Silverlight/XAML hoặc `Microsoft.Phone` trong BIND1.

## Điểm cần rà soát khi review

- Sai khác version, culture hoặc public-key token không được phân giải nhầm: Task 1 kiểm thử exact identity và trường hợp token/culture/version khác.
- Redirect không có đích hoặc sai đích không được báo thành công: Task 1 kiểm thử redirect hợp lệ, target không tồn tại và không fallback theo tên.
- XAP thiếu, sai hash hoặc thiếu entry assembly phải cho kết quả lỗi xác định: Task 2 kiểm thử fixture validation; Task 3 kiểm thử marker lỗi.
- Giá trị identity có newline/ký tự phân cách không được làm vỡ định dạng log: Task 1 kiểm thử formatter luôn tạo một dòng.
- Dependency/type/member không có phải được báo đúng boundary và host vẫn lưu log: Task 3 kiểm thử lỗi có kiểm soát; thiết bị xác minh app còn hoạt động.

---

## Cấu trúc file dự kiến

- Tạo `src/Wp7Binding/Wp7Binding.csproj`: thư viện .NET thuần chứa logic BIND1.
- Tạo `src/Wp7Binding/AssemblyIdentity.cs`: chuẩn hóa và so khớp identity.
- Tạo `src/Wp7Binding/PackageAssemblyCatalog.cs`: `PackageAssembly(AssemblyIdentity Identity, string EntryPath, byte[] Image)` và danh mục assembly riêng trong XAP.
- Tạo `src/Wp7Binding/AssemblyBindingResolver.cs`: exact match, redirect và quyết định fallback.
- Tạo `src/Wp7Binding/BindingLogFormatter.cs`: định dạng marker BIND1 an toàn theo dòng.
- Tạo `tests/Wp7Binding.Tests/Wp7Binding.Tests.csproj` và `tests/Wp7Binding.Tests/Program.cs`: bộ kiểm thử console không cần UIKit hoặc NuGet ngoài.
- Sửa: `tests/assert-real-wp7-xna.py`: mở rộng contract fixture và tách hàm kiểm tra để test được.\n- Tạo: `tests/test_real_wp7_xna_contract.py`: regression test report hợp lệ và identity bị thiếu.
- Sửa `.github/workflows/wp7-ilrun1-ios.yml`: tải/kiểm tra fixture, đóng gói XAP và report làm raw bundle resource; build artifact BIND1 với nhãn V7.
- Sửa `tests/assert-ilrun1-interpreter-config.py`: chỉ cấm tham chiếu tĩnh tới payload IlPayload; cho phép tham chiếu thư viện resolver BIND1.
- Sửa `src/ILRun1Host/ILRun1Host.csproj`: tham chiếu thư viện resolver và khai báo tài nguyên XAP/report; giữ nguyên cấu hình interpreter/trimmer.
- Tạo `src/ILRun1Host/Bind1ProbeExecutor.cs`: nạp fixture và kết nối resolver với `AssemblyResolve`.
- Sửa `src/ILRun1Host/MainViewController.cs`: thêm nút probe riêng, giữ nguyên nút và kết quả ILRUN1.
- Sửa handoff `docs/handoff/CURRENT.md` sau khi artifact/thiết bị có kết quả, ghi trạng thái chính xác và không gọi build CI là device PASS.

### Task 1: Core identity, resolver và kiểm thử thuần

**Files:**
- Tạo: `src/Wp7Binding/Wp7Binding.csproj`
- Tạo: `src/Wp7Binding/AssemblyIdentity.cs`
- Tạo: `src/Wp7Binding/PackageAssemblyCatalog.cs`
- Tạo: `src/Wp7Binding/AssemblyBindingResolver.cs`
- Tạo: `src/Wp7Binding/BindingLogFormatter.cs`
- Tạo: `tests/Wp7Binding.Tests/Wp7Binding.Tests.csproj`
- Tạo: `tests/Wp7Binding.Tests/Program.cs`

**Giao diện:**
- `AssemblyIdentity.FromAssemblyName(AssemblyName name)` chuẩn hóa name, version, culture (`neutral` khi rỗng) và public-key token (`null` khi rỗng).
- `PackageAssembly(AssemblyIdentity Identity, string EntryPath, byte[] Image)` biểu diễn một assembly trong gói. `PackageAssemblyCatalog.TryGetExact(AssemblyIdentity identity, out PackageAssembly assembly)` chỉ thành công khi cả bốn trường khớp; so sánh name/culture không phân biệt hoa thường, version/token chính xác.
- `BindingResolution(AssemblyIdentity Requested, PackageAssembly? Target, string? Source, string? RedirectReason)` biểu diễn quyết định. `AssemblyBindingResolver.Resolve(AssemblyName requested)` trả nguồn `package|compat` khi tìm thấy; nếu không, target/source là null để host trả `null` cho runtime. Resolver nhận catalog và bảng redirect được inject lúc khởi tạo.
- `BindingLogFormatter.FormatRequest(AssemblyIdentity)`, `FormatRedirect(AssemblyIdentity from, AssemblyIdentity to, string reason)`, `FormatResolve(AssemblyIdentity requested, AssemblyIdentity resolved, string source)`, `FormatMissingType(string type, string assembly)` và `FormatMissingMember(string member, string type, string assembly)` trả về marker `[BIND1][REQUEST]`, `[BIND1][REDIRECT]`, `[BIND1][RESOLVE_OK]`, `[BIND1][MISSING_TYPE]`, `[BIND1][MISSING_MEMBER]`. Mỗi phương thức trả về một dòng và escape CR/LF. Task 3 bổ sung `FormatBindFail(AssemblyIdentity requested, string exception)` và `FormatEnd(bool passed)` cho marker `[BIND1][ASSEMBLY_BIND_FAIL]` và `[BIND1][END]`.

- [ ] **Bước 1: Viết test đỏ cho so khớp identity**
  Trong `Program.cs`, thêm `IdentityRequiresExactVersionCultureAndToken()`: cùng identity phải khớp; thay lần lượt version, culture hoặc token thì không khớp.
- [ ] **Bước 2: Chạy test để xác nhận thất bại**
  Chạy `dotnet run --project tests/Wp7Binding.Tests/Wp7Binding.Tests.csproj -c Release`.
  Kỳ vọng: FAIL vì project/type/method chưa có.
- [ ] **Bước 3: Viết test đỏ cho thứ tự package rồi redirect**
  Thêm `PackageExactMatchPrecedesRedirect()`, `RedirectRequiresPresentTarget()` và `NoNameOnlyFallback()`; xác minh exact package thắng redirect, redirect thiếu target bị từ chối, và cùng tên nhưng identity khác không được lấy.
- [ ] **Bước 4: Chạy test để xác nhận các ca resolver thất bại**
  Chạy cùng lệnh; kỳ vọng các test mới FAIL vì resolver chưa có.
- [ ] **Bước 5: Cài đặt identity/catalog/resolver tối thiểu**
  Thêm các kiểu ở trên; redirect dùng dictionary từ identity nguồn chính xác sang identity target đã có trong catalog. Không thêm redirect mặc định cho XNA.
- [ ] **Bước 6: Viết và chạy test cho marker an toàn**
  Thêm `FormatterEscapesNewlinesAndKeepsOneMarkerPerLine()` và `FormatterEmitsRequestRedirectResolveAndMissingMarkers()`; xác minh các marker REQUEST/REDIRECT/RESOLVE_OK/MISSING_TYPE/MISSING_MEMBER đúng tên, CR/LF bị escape và không tạo newline nội bộ. Chạy lệnh test; kỳ vọng tất cả PASS.
- [ ] **Bước 7: Commit phần resolver**
  Commit các file Task 1 với thông điệp `feat: add WP7 assembly binding resolver`.

### Task 2: Xác thực và đóng gói fixture thật trong CI

**Files:**
- Sửa: `tests/assert-real-wp7-xna.py`
- Tạo: `tests/test_real_wp7_xna_contract.py`
- Sửa: `.github/workflows/wp7-ilrun1-ios.yml`
- Sửa: `tests/assert-ilrun1-interpreter-config.py`
- Sửa: `src/ILRun1Host/ILRun1Host.csproj`

**Giao diện:**
- `validate_report(report: dict) -> None` trong assertion script xác nhận SHA-256 package, runtime/entry type, entry path, version/culture/public-key token, IL-only và entry/type resolution.
- Workflow lấy fixture qua URL/commit pin hiện có; scanner tạo JSON report. XAP và report được đặt trong bundle dưới logical names `Aleterated.xap` và `Aleterated.xapscan1.json`.
- Fixture/report là tài nguyên dữ liệu thô; không đưa Aleterated assembly thành `ProjectReference` hay `Reference`.

- [ ] **Bước 1: Viết test đỏ cho schema entry identity**
  Trong `tests/test_real_wp7_xna_contract.py`, dùng `runpy.run_path()` để lấy hàm validator; thêm `test_rejects_entry_without_public_key_token_field()` với report hợp lệ tối thiểu nhưng thiếu trường token.
- [ ] **Bước 2: Chạy test để xác nhận thất bại**
  Chạy `python3 -m unittest tests/test_real_wp7_xna_contract.py -v`; kỳ vọng FAIL vì hàm `validate_report` chưa tồn tại.
- [ ] **Bước 3: Tách assertion thành hàm và bổ sung contract**
  Thêm `validate_report(report: dict) -> None`; giữ nguyên SHA-256 `69bd93627f4fa2c567748e36c3487c083db95c0efe01ef203bf60179bf9342a1`, runtime 3.0, entry `Aleterated.Game1`, XNA AssemblyRef checks; bổ sung path/version/culture/token/IL-only entry. CLI đọc JSON và gọi hàm này.
- [ ] **Bước 4: Hoàn tất test hợp lệ và lỗi**
  Thêm `test_accepts_pinned_entry_identity()`; chạy unittest. Kỳ vọng report hợp lệ PASS và report thiếu token FAIL. Script CLI cũng phải PASS với report fixture thật do workflow tạo.
- [ ] **Bước 5: Lấy, quét và đóng gói fixture trong workflow**
  Dùng URL pinned hiện có, chạy XapScan, assertion script; sau khi contract PASS mới copy XAP/report vào `src/ILRun1Host/Assets`.
- [ ] **Bước 6: Giữ invariant ILRUN1 và khai báo tài nguyên**
  Sửa assertion để cấm static reference tới `IlPayload.dll` nhưng cho phép project reference resolver; thêm raw XAP/report resources. Giữ nguyên interpreter/trimmer, minimum iOS 15 và raw payload ILRUN1.
- [ ] **Bước 7: Chạy kiểm tra liên quan**
  Chạy `python3 tests/assert-ilrun1-interpreter-config.py`, `python3 -m unittest tests/test_real_wp7_xna_contract.py -v`, và assertion scanner trên report thật; kỳ vọng tất cả PASS.
- [ ] **Bước 8: Commit fixture pipeline**
  Commit workflow, assertion update, tests và resource contract với thông điệp `ci: package pinned WP7 fixture for BIND1`.

### Task 3: Probe runtime, log và UI trên iOS

**Files:**
- Sửa: `src/Wp7Binding/PackageAssemblyCatalog.cs`
- Sửa: `src/Wp7Binding/AssemblyBindingResolver.cs`
- Tạo: `src/ILRun1Host/Bind1ProbeExecutor.cs`
- Sửa: `src/ILRun1Host/ILRun1Host.csproj`
- Sửa: `src/ILRun1Host/MainViewController.cs`
- Tạo/sửa: `tests/Wp7Binding.Tests/Program.cs`

**Giao diện:**
- `Bind1ProbeExecutor.Execute(Action<string>? onLine = null) -> Bind1ProbeResult` đọc XAP/report từ bundle, xác thực entry identity, nạp `Aleterated` bằng bytes và tìm `Aleterated.Game1`.
- `Bind1ProbeResult(bool DiagnosticSuccess, string Log, string? SavedLogPath)` dùng `DiagnosticSuccess=true` khi đã xác định được ranh giới đầu tiên một cách xác định, kể cả khi kết quả cuối là assembly/type/member missing.
- Resolver trong `AssemblyResolve` phát `REQUEST`, thử catalog exact, phát `REDIRECT` nếu áp dụng và `RESOLVE_OK` khi thực sự nạp được; nếu không, trả null cho runtime và host ghi `ASSEMBLY_BIND_FAIL`.
- Marker cuối `[BIND1][END] PASS` chỉ khi entry/type và bước kiểm tra tối thiểu hoàn tất không thiếu binding; nếu phát hiện boundary chưa hỗ trợ thì kết thúc `[BIND1][END] FAIL` nhưng không crash. Cả hai trường hợp vẫn lưu log.

- [ ] **Bước 1: Viết test đỏ cho kết quả probe và marker**
  Thêm test `UnresolvedIdentityProducesBindFailAndEndMarkers()`: identity không có trong catalog tạo quyết định unresolved, formatter phát `[BIND1][ASSEMBLY_BIND_FAIL]` rồi `[BIND1][END] FAIL`, và dữ liệu có newline vẫn chỉ chiếm một log line.
- [ ] **Bước 2: Chạy test để xác nhận thất bại**
  Chạy lệnh Task 1; kỳ vọng FAIL vì probe/formatter chưa cung cấp contract hoàn chỉnh.
- [ ] **Bước 3: Thêm nạp XAP và catalog từ manifest scanner**
  Đọc XAP bằng `ZipArchive`; lấy assembly private bằng entry path từ report; chỉ nạp bytes khi resolver có request phù hợp. Kiểm tra hash/entry trước khi bắt đầu probe.
- [ ] **Bước 4: Nối resolver vào AssemblyResolve và bắt lỗi reflection**
  Ghi REQUEST cho identity từ `ResolveEventArgs.Name`; xử lý chính xác `FileNotFoundException`, `TypeLoadException`, `MissingMemberException` và `ReflectionTypeLoadException` thành marker đầu tiên phù hợp; luôn tháo handler trong finally.
- [ ] **Bước 5: Hoàn tất test lỗi có kiểm soát**
  Chạy `dotnet run --project tests/Wp7Binding.Tests/Wp7Binding.Tests.csproj -c Release`; kỳ vọng PASS cho unresolved identity, định dạng missing-type/member và log một dòng mỗi marker.
- [ ] **Bước 6: Thêm nút BIND1 riêng trong host**
  Thêm nút “Run BIND1 Probe”, chạy trên worker task như ILRUN1, cập nhật log/status trên main thread; không đổi hành vi nút ILRUN1.
- [ ] **Bước 7: Build iOS host trên cấu hình chuẩn**
  Chạy `dotnet publish src/ILRun1Host/ILRun1Host.csproj -f net9.0-ios -c Release -r ios-arm64 -p:RuntimeIdentifier=ios-arm64 -p:UseInterpreter=false -p:MtouchInterpreter=all -p:TrimMode=full -p:MtouchUseLlvm=false -p:EnableCodeSigning=false -p:CodesignRequireProvisioningProfile=false -p:CodesignKey="" -p:CodesignProvision="" -p:ArchiveOnBuild=false`.
  Kỳ vọng: publish thành công, app bundle có `IlPayload.dll`, `Aleterated.xap`, report; minimum iOS vẫn 15.0.
- [ ] **Bước 8: Commit probe iOS**
  Commit executor, UI và resolver integration với thông điệp `feat: add on-device WP7 BIND1 probe`.

### Task 4: Artifact V7, xác minh và bàn giao device test

**Files:**
- Sửa: `.github/workflows/wp7-ilrun1-ios.yml`
- Sửa: `tests/assert-ilrun1-interpreter-config.py`
- Sửa sau khi có kết quả: `docs/handoff/CURRENT.md`

- [ ] **Bước 1: Cập nhật nhãn build ngoài ứng dụng**
  Đổi nhãn mặc định workflow sang V7 cho push và dispatch; validate dạng `V[0-9]+`; đặt tên run/artifact/IPA theo `WP7-BIND1-V7-ios15-unsigned`. Xác minh app version và display name không thay đổi.
- [ ] **Bước 2: Bổ sung kiểm tra artifact**
  Workflow xác minh IPA chứa host executable, raw ILRUN1 payload, Aleterated XAP và report; kiểm tra minimum iOS 15.0.
- [ ] **Bước 3: Chạy toàn bộ kiểm tra trước khi upload**
  Chạy test resolver, fixture contract, assertion ILRUN1/scanner và publish iOS; chỉ khi mọi bước qua mới upload IPA.
- [ ] **Bước 4: Tạo artifact unsigned**
  Workflow upload IPA với nhãn V7 bên ngoài ứng dụng. Kết quả CI chỉ ghi là build GREEN, không ghi là device PASS.
- [ ] **Bước 5: Chờ device test trên iPhone iOS 18.7**
  Người dùng cài IPA V7, chạy riêng ILRUN1 và BIND1, xác nhận host còn hoạt động và gửi `WP7Runner_TakeThis.log` cùng `WP7Runner_Persistent.log`. Kỳ vọng BIND1 có một trong các chuỗi thật: `[BIND1][REQUEST]`, `[BIND1][RESOLVE_OK]`, `[BIND1][ASSEMBLY_BIND_FAIL]`, `[BIND1][MISSING_TYPE]`, `[BIND1][MISSING_MEMBER]`, kết thúc bằng `[BIND1][END]` và có `[LOG_SAVED]`.
- [ ] **Bước 6: Cập nhật handoff theo bằng chứng**
  Ghi run/artifact ID, digest, trạng thái build và kết quả log thiết bị; không suy diễn device PASS từ build GREEN.
- [ ] **Bước 7: Commit cập nhật pipeline/handoff**
  Commit pipeline riêng; cập nhật handoff sau khi có log thật từ thiết bị.

## Hoàn tất kế hoạch

Hoàn thành BIND1 khi CI tạo được artifact V7, probe trên iOS 18.7 ghi đúng dependency/type/member boundary đầu tiên, lưu log và giữ host hoạt động. Đây là thành công chẩn đoán BIND1, không phải tuyên bố ứng dụng XNA đã chạy.
