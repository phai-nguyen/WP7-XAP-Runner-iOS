# Kế hoạch triển khai BIND1 — Probe liên kết assembly WP7

> **Dành cho agent triển khai:** BẮT BUỘC dùng tiểu kỹ năng `superpowers:subagent-driven-development` (khuyến nghị) hoặc `superpowers:executing-plans` để thực hiện kế hoạch theo từng task. Các bước dùng cú pháp checkbox `- [ ]`.

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
- Redirect không có đích, sai đích hoặc vòng lặp không được báo thành công: Task 1 kiểm thử redirect hợp lệ và từ chối target không tồn tại.
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
- `BindingLogFormatter.FormatRequest(AssemblyIdentity)`, `FormatRedirect(AssemblyIdentity from, AssemblyIdentity to, string reason)`, `FormatResolve(AssemblyIdentity requested, AssemblyIdentity resolved, string source)`, `FormatBindFail(AssemblyIdentity requested, string exception)` và `FormatEnd(bool passed)` đều trả về `string`, đúng một marker mỗi dòng; CR/LF được escape.

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
  Thêm `FormatterEscapesNewlinesAndKeepsOneMarkerPerLine()`; xác minh CR/LF bị escape và marker kết quả không chứa newline nội bộ. Chạy lệnh test; kỳ vọng tất cả PASS.
- [ ] **Bước 7: Commit phần resolver**
  Commit các file Task 1 với thông điệp `feat: add WP7 assembly binding resolver`.

### Task 2: Xác thực và đóng gói fixture thật trong CI

**Files:**
- Tạo: `tests/validate-bind1-fixture.py`
- Sửa: `.github/workflows/wp7-ilrun1-ios.yml`
- Sửa: `tests/assert-ilrun1-interpreter-config.py`
- Sửa: `src/ILRun1Host/ILRun1Host.csproj`

**Giao diện:**
- Script nhận đường dẫn XAP và JSON report: `python3 tests/validate-bind1-fixture.py <xap> <report>`.
- Script xác nhận SHA-256 fixture là `69bd93627f4fa2c567748e36c3487c083db95c0efe01ef203bf60179bf9342a1`, runtime 3.0, entry `Aleterated.Game1`, runtime type XNA, và có assembly managed entry IL-only.
- Workflow dùng URL/commit pin hiện có của fixture; scanner sinh report; XAP và report được đặt trong bundle dưới logical names `Aleterated.xap` và `Aleterated.xapscan1.json`.
- Tài nguyên XAP/report là file dữ liệu thô; không đưa assembly Aleterated thành `ProjectReference` hay `Reference`.

- [ ] **Bước 1: Viết test đỏ cho fixture validator**
  Tạo test Python gọi script với report mẫu: đúng SHA/entry phải qua; sai hash và sai entry phải thất bại với thông báo tương ứng.
- [ ] **Bước 2: Chạy test validator để xác nhận thất bại**
  Chạy `python3 -m unittest tests/test_validate_bind1_fixture.py -v`.
  Kỳ vọng: FAIL vì validator chưa được tạo.
- [ ] **Bước 3: Cài đặt validator và kiểm tra fixture thật**
  Đọc JSON scanner theo schema hiện có; kiểm tra package SHA, AppManifest, entry resolution và thuộc tính assembly IL-only. Thêm fixture report nhỏ trong test, không thêm XAP/DLL nhị phân.
- [ ] **Bước 4: Chạy test validator**
  Chạy `python3 -m unittest tests/test_validate_bind1_fixture.py -v`; kỳ vọng PASS cho report hợp lệ và hai report lỗi.
- [ ] **Bước 5: Thêm bước lấy fixture, scan và validate vào workflow**
  Dùng URL pinned hiện có, tính SHA-256, chạy XapScan, validator; chỉ sau khi hợp lệ mới copy XAP/report vào `src/ILRun1Host/Assets`.
- [ ] **Bước 6: Cập nhật invariant ILRUN1 và tài nguyên host**
  Sửa assertion để cấm `IlPayload.dll` static reference cụ thể nhưng cho phép project reference resolver; thêm tài nguyên raw XAP/report. Giữ nguyên `MtouchInterpreter=all`, `TrimmerRootAssembly System.Runtime`, iOS 15 và raw ILRUN1 payload.
- [ ] **Bước 7: Chạy toàn bộ kiểm tra CI liên quan**
  Chạy `python3 tests/assert-ilrun1-interpreter-config.py`, `python3 tests/assert-real-wp7-xna.py <report>`, `python3 -m unittest tests/test_validate_bind1_fixture.py -v`; kỳ vọng tất cả PASS.
- [ ] **Bước 8: Commit fixture pipeline**
  Commit workflow, validator, tests và project resources với thông điệp `ci: package pinned WP7 fixture for BIND1`.

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
  Thêm test `UnresolvedIdentityProducesBindFailAndEndMarkers()`: identity không có trong catalog tạo quyết định unresolved, formatter phát `ASSEMBLY_BIND_FAIL` rồi `END FAIL`, và dữ liệu có newline vẫn chỉ chiếm một log line.
- [ ] **Bước 2: Chạy test để xác nhận thất bại**
  Chạy lệnh Task 1; kỳ vọng FAIL vì probe/formatter chưa cung cấp contract hoàn chỉnh.
- [ ] **Bước 3: Thêm nạp XAP và catalog từ manifest scanner**
  Đọc XAP bằng `ZipArchive`; lấy assembly private bằng entry path từ report; chỉ nạp bytes khi resolver có request phù hợp. Kiểm tra hash/entry trước khi bắt đầu probe.
- [ ] **Bước 4: Nối resolver vào AssemblyResolve và bắt lỗi reflection**
  Ghi REQUEST cho identity từ `ResolveEventArgs.Name`; xử lý chính xác `FileNotFoundException`, `TypeLoadException`, `MissingMemberException` và `ReflectionTypeLoadException` thành marker đầu tiên phù hợp; luôn tháo handler trong finally.
- [ ] **Bước 5: Hoàn tất test lỗi có kiểm soát**
  Chạy `dotnet run --project tests/Wp7Binding.Tests/Wp7Binding.Tests.csproj -c Release`; kỳ vọng PASS cho missing dependency/type/member và log một dòng mỗi marker.
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
  Đổi nhãn mặc định workflow sang V7 cho push và dispatch, validate dạng `V[0-9]+`, đặt tên run/artifact/IPA theo `WP7-BIND1-V7-ios15-unsigned`. Xác minh app version và display name không thay đổi.
- [ ] **Bước 2: Bổ sung kiểm tra artifact**
  Workflow xác minh IPA chứa host executable, raw ILRUN1 payload, Aleterated XAP và report; kiểm tra minimum iOS 15.0.
- [ ] **Bước 3: Chạy đầy đủ test trước khi phát hành**
  Chạy test resolver, fixture validator, hai assertion scanner/ILRUN1 và publish iOS; kỳ vọng mọi bước thành công trước khi upload IPA.
- [ ] **Bước 4: Tạo artifact unsigned**
  Workflow upload IPA với nhãn V7 bên ngoài ứng dụng. Kết quả CI chỉ được ghi là build GREEN, không ghi là device PASS.
- [ ] **Bước 5: Cập nhật handoff sau khi có bằng chứng**
  Ghi run/artifact ID, digest, trạng thái build và hướng dẫn thu log; sau khi người dùng thử iPhone iOS 18.7, bổ sung kết quả log riêng. Không tự suy diễn device PASS từ build GREEN.
- [ ] **Bước 6: Commit cập nhật pipeline/handoff**
  Commit thay đổi workflow và assertion; cập nhật handoff thành commit riêng sau khi có kết quả thiết bị.

---

## Hoàn tất kế hoạch

Hoàn thành BIND1 khi CI tạo được artifact V7, probe trên iOS 18.7 ghi đúng dependency/type/member boundary đầu tiên, lưu log và giữ host hoạt động. Đây là thành công chẩn đoán BIND1, không phải tuyên bố ứng dụng XNA đã chạy.
