# Thiết kế BIND1 — Thăm dò liên kết assembly WP7

**Trạng thái:** Bản thiết kế chờ người dùng xem xét  
**Ngày:** 2026-09-27  
**Kho mã:** `phai-nguyen/WP7-XAP-Runner-iOS`

## Mục tiêu và ràng buộc

ILRUN1-NET9-ROOTSR đã PASS trên iPhone thật chạy iOS 18.7: thiết bị nạp payload managed bên ngoài, phân giải được `IlPayload.EntryPoint.Run()` và trả về `XAP_ILRUN1_PASS:42`. BIND1 là milestone tiếp theo.

BIND1 sẽ tạo một ranh giới liên kết assembly managed có kết quả xác định cho assembly trong ứng dụng WP7 gốc. Mục tiêu đầu tiên là fixture WP7/XNA Aleterated đã được ghim phiên bản và có giấy phép MIT. Đây chỉ là phép thăm dò liên kết; không đồng nghĩa triển khai đồ họa XNA. Nhãn ứng dụng V6 chỉ là nhãn bên ngoài để nhận diện bản build thử nghiệm, không được đổi tên hiển thị hay phiên bản ứng dụng.

Dự án tiếp tục theo chiến lược target-first: nạp assembly của ứng dụng thật, báo chính xác ranh giới chưa hỗ trợ đầu tiên, rồi chỉ triển khai phần tương thích được duyệt sau đó. BIND1 không được tuyên bố hỗ trợ mọi XAP hay chạy thành công trò chơi.

## Các hướng đã cân nhắc

1. **Chỉ mở rộng XAPSCAN1.** Cách này liệt kê AssemblyRef/TypeRef nhưng không kiểm tra được hành vi liên kết thực tế của runtime iOS.
2. **Chỉ dùng payload ILRUN1 hiện có.** Cách này đã chứng minh chạy IL động, nhưng không đại diện cho đồ thị phụ thuộc của một ứng dụng WP7 thật.
3. **Thêm bộ thăm dò liên kết runtime dùng fixture thật (được chọn).** Tái sử dụng fixture đã ghim và thông tin scanner, nạp assembly gốc trên thiết bị, quan sát chính xác ranh giới liên kết/type/member mà runtime gặp. Cách này bổ sung bằng chứng runtime cần thiết nhưng không kéo theo XNA, XAML hay mô phỏng giao diện.

## Kiến trúc và luồng dữ liệu

1. CI lấy mã nguồn Aleterated đã ghim phiên bản thông qua workflow quét WP7 thật hiện có; không đưa file XAP vào repo.
2. BIND1 dùng assembly managed entry cùng các dependency managed riêng của ứng dụng trong gói fixture. Trước khi thử nạp hoặc phân giải, hệ thống ghi lại identity của assembly: tên đơn, phiên bản, culture và public-key token.
3. Một thành phần liên kết riêng áp dụng thứ tự phân giải theo identity chính xác và có tính xác định: assembly riêng trong gói trước, tiếp theo là bảng chuyển hướng tương thích được khai báo tường minh, cuối cùng mới đến bộ phân giải của platform/runtime. Chỉ chuyển hướng đến đích đang có mặt và tương thích; không tự fallback chỉ theo tên hoặc phiên bản gần đúng.
4. Bộ thăm dò phân giải entry type của fixture và kiểm tra các type/member được tham chiếu trực tiếp, đủ để bộc lộ ranh giới chưa hỗ trợ đầu tiên. Không chạy phần dựng hình trò chơi hoặc gọi API XNA native.
5. Host iOS hiển thị kết quả PASS/FAIL ngắn gọn và lưu log chẩn đoán chi tiết, đồng thời giữ nguyên đường khởi động ILRUN1 đã được chứng minh.

Resolver được tách sau một interface để có thể kiểm thử thứ tự và quyết định phân giải mà không cần khởi chạy UIKit. XAPSCAN1 tiếp tục đảm nhiệm phân tích tĩnh gói và báo cáo; BIND1 chịu trách nhiệm bằng chứng nạp/phân giải ở runtime.

## Quy ước log chẩn đoán

Mỗi lần thăm dò ghi các dòng ổn định, có cấu trúc để quyết định phân giải có thể được tái hiện:

- `[BIND1][REQUEST] name=… version=… culture=… pkt=…`
- `[BIND1][REDIRECT] from=… to=… reason=…`
- `[BIND1][RESOLVE_OK] requested=… resolved=… source=package|compat|runtime`
- `[BIND1][ASSEMBLY_BIND_FAIL] requested=… exception=…`
- `[BIND1][MISSING_TYPE] type=… assembly=…`
- `[BIND1][MISSING_MEMBER] member=… type=… assembly=…`
- `[BIND1][END] PASS|FAIL`

Giá trị identity phải được escape hoặc mã hóa để mỗi marker vẫn nằm trên một dòng log có thể phân tích. Lỗi liên kết/type/member phải trở thành kết quả thăm dò có kiểm soát, không làm ứng dụng crash ngoài dự kiến. Bản đầu tiên báo lỗi đầu tiên theo thứ tự xác định; có thể bỏ qua các lỗi phát sinh sau đó.

## Phạm vi và phần không làm

Bao gồm:

- lấy fixture thông qua workflow đã ghim phiên bản hiện có;
- ghi nhận identity assembly runtime và phân giải theo identity chính xác;
- quyết định chuyển hướng tường minh, có thể kiểm tra;
- chẩn đoán xác định ranh giới còn thiếu đầu tiên;
- kiểm thử unit/regression cho so khớp identity, thứ tự phân giải, chuyển hướng và định dạng marker;
- artifact iOS có nhãn V bên ngoài ứng dụng.

Không bao gồm:

- triển khai XNA Graphics hoặc chạy game loop XNA;
- nạp Silverlight/XAML, điều hướng trang, dựng hình hay API `Microsoft.Phone`;
- hợp nhất phiên bản diện rộng, tự tạo binding redirect hoặc giả vờ assembly không có mặt là đã tồn tại;
- thay đổi app identity, tên hiển thị, `ApplicationVersion` hoặc `ApplicationDisplayVersion`;
- đổi toolchain .NET/iOS đã xác thực hoặc nghiên cứu lại các thử nghiệm .NET 10 đã loại bỏ.

## Xác minh và tiêu chí chấp nhận

BIND1 sẵn sàng để đánh giá trên thiết bị thật khi đáp ứng các điều kiện sau:

1. CI build được probe fixture; các test xác minh so khớp identity chính xác, thứ tự phân giải xác định, chuyển hướng hợp lệ và marker lỗi ổn định.
2. Trên thiết bị iOS 18.7, tìm thấy/đọc được assembly entry của fixture và log được identity của nó.
3. Probe hoặc phân giải được dependency thật với marker `RESOLVE_OK` chính xác, hoặc ghi nhận identity/type/member thực sự không hỗ trợ đầu tiên bằng marker `*_FAIL` có kiểm soát, tiếp theo là `[BIND1][END] FAIL`; ứng dụng vẫn hoạt động và lưu log.
4. Probe BIND1 chỉ được xem là xanh về mặt chẩn đoán khi log thiết bị xác định được ranh giới liên kết thật đầu tiên mà không crash. Điều này **không** có nghĩa ứng dụng WP7 đã chạy; công việc tương thích tiếp theo sẽ dựa trên ranh giới được báo cáo.

Không chấp nhận chuyển hướng tương thích chỉ vì fixture có tham chiếu tới assembly đó. Đích chuyển hướng phải hiện diện và được chứng minh tương thích API; nếu không, báo lỗi liên kết là kết quả đúng.

## Ràng buộc khi triển khai

- Giữ baseline kiểm thử: .NET 9.0.303, Microsoft.iOS 18.5.9207, Xcode 16.4, iOS SDK 18.5, minimum iOS 15.0 và thiết bị chính iOS 18.7.
- Giữ nguyên bằng chứng ILRUN1 hiện có (không tham chiếu tĩnh; nạp payload thô), trừ khi một yêu cầu BIND1 cụ thể chứng minh host contract phải thay đổi.
- Không thêm binary fixture vào repo; dùng cơ chế lấy fixture đã ghim phiên bản và xác thực pin upstream trước khi đóng gói.
