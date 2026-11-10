# Soul Signal

Website nghe nhạc phục vụ đồ án nhóm hai người trong bốn tuần, dùng **ASP.NET Core MVC (.NET 10), Razor, Identity, EF Core và SQL Server**.

Bản hiện tại có ba vai trò: **người nghe (Listener)**, **nhà phân phối (Distributor)** và **admin**. Nhà phân phối đăng nhập ngay trên Soul Signal để tải nhạc lên; bài mới ở trạng thái chờ duyệt. Admin nghe thử, kiểm tra thông tin rồi duyệt hoặc từ chối; người nghe chỉ tìm và nghe các bài đã duyệt.

Các nguồn trong bảng tài khoản demo mang tên hư cấu để minh họa việc phân phối. Dữ liệu demo lưu trong Soul Signal. Tên nghệ sĩ là thông tin của bài hát.

## Chạy trên máy cá nhân

Cần .NET **10 SDK**, SQL Server đang chạy và tài khoản có quyền tạo/cập nhật CSDL đồ án. Luồng tải FLAC cần **FFmpeg và ffprobe** trên PATH của tiến trình ứng dụng. Kiểm tra bằng `dotnet --version`, `ffmpeg -version`, `ffprobe -version`. Python không cần để chạy website.

Kết nối mặc định, khóa ConnectionStrings:DefaultConnection:

```text
Server=localhost;Database=SoulSignal_Week1;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True
```

Chuỗi này dùng Windows Authentication của người đang chạy ứng dụng. Nếu máy dùng instance khác, thay localhost bằng tên thực tế, chẳng hạn localhost\SQLEXPRESS hoặc (localdb)\MSSQLLocalDB nếu instance đó đã cài. Có thể đặt cấu hình riêng trong phiên PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Server=localhost\SQLEXPRESS;Database=SoulSignal_Week1;Trusted_Connection=True;TrustServerCertificate=True;'
```

Chạy từ thư mục chứa SoulSignal.csproj:

```powershell
dotnet restore
dotnet run --launch-profile http
```

Mở **http://localhost:5023**. Profile http dùng Development: ứng dụng áp dụng migration và seed khi khởi động. Nếu kết nối lỗi, kiểm tra dịch vụ SQL Server, tên instance, quyền đăng nhập và quyền tạo CSDL.

## Xử lý FLAC trên máy chạy ứng dụng

Nếu FFmpeg/ffprobe chưa có trong PATH, đặt đường dẫn riêng bằng cấu hình `MediaProcessing:FfmpegPath` và `MediaProcessing:FfprobePath` (hoặc biến môi trường cùng tên với `__` thay `:`):

```powershell
$env:MediaProcessing__FfmpegPath = 'C:\Tools\ffmpeg\bin\ffmpeg.exe'
$env:MediaProcessing__FfprobePath = 'C:\Tools\ffmpeg\bin\ffprobe.exe'
```

Đây là ví dụ cấu hình, cần thay bằng vị trí thực tế trên máy. Ứng dụng lưu FLAC gốc, xác nhận codec/thời lượng rồi giải mã để tạo MP3 CBR 128 và 320 kbps, stereo 44.100 Hz. Không nâng chất lượng từ một MP3 đã mất dữ liệu thành FLAC. Probe và chuyển mã có timeout; tệp lỗi, thiếu bộ xử lý hoặc quá thời lượng không tạo bài. Các tệp vừa tạo được dọn nếu xử lý/lưu thất bại. Xử lý vẫn diễn ra trong yêu cầu upload, chưa có hàng đợi nền.

Hai form tải nhạc có một tệp JavaScript nhỏ để báo tiến độ: **% tải tệp** lấy từ số byte trình duyệt đã gửi; sau khi tải xong, **% chuyển mã** lấy từ thời gian âm thanh FFmpeg thực sự xử lý. Đọc tệp, lấy bìa và lưu bài hiển thị trạng thái riêng. Tải lên 100% chưa có nghĩa đã chuyển mã và lưu xong; không dùng bộ đếm thời gian để tạo % giả. Nút gửi bị khóa trong lúc chạy, có hủy và báo lỗi tại form. Khi tắt JavaScript, form vẫn POST thông thường, chờ kết quả rồi chuyển trang; không có tiến độ trực tiếp.

## Tài khoản demo

| Vai trò | Tên đăng nhập ngắn | Email vẫn dùng được | Mật khẩu demo |
|---|---|---|---|
| Người nghe | listener | listener@soulsignal.local | 123456 |
| Người nghe thường | thuong | thuong@soulsignal.local | 123456 |
| Người nghe VIP còn hạn khi tạo mẫu | vip | vip@soulsignal.local | 123456 |
| Người nghe VIP hết hạn khi tạo mẫu | viphethan | viphethan@soulsignal.local | 123456 |
| Người nghe để thử mua VIP | listener2 | listener2@soulsignal.local | 123456 |
| DistroKite Demo | distrokite | distrokite@soulsignal.local | 123456 |
| TuneKore Demo | tunekore | tunekore@soulsignal.local | 123456 |
| CD Babi Demo | cdbabi | cdbabi@soulsignal.local | 123456 |
| RouteNoot Demo | routenoot | routenoot@soulsignal.local | 123456 |
| Amusee Demo | amusee | amusee@soulsignal.local | 123456 |
| Dittoo Demo | dittoo | dittoo@soulsignal.local | 123456 |
| Admin | admin | admin@soulsignal.local | 123456 |

Dữ liệu đăng nhập mẫu, bí danh ngắn và mật khẩu `123456` chỉ phục vụ Development. Bạn có thể đăng nhập bằng tên ngắn hoặc email trong bảng. Đăng ký công khai tạo tài khoản Listener. Mỗi tài khoản nhà phân phối trong bảng gắn cố định với một nguồn riêng.

## Demo chính — nhà phân phối tải lên, admin duyệt

1. Đăng nhập **distrokite** hoặc **tunekore**, mật khẩu **123456**, mở **/Distributor**, chọn **Tải nhạc lên**. Nguồn phân phối được xác định từ tài khoản, không chọn nguồn khác.
2. Nhập tên bài, nghệ sĩ, thể loại và thông tin quyền. Chọn **một FLAC gốc** và ảnh bìa nếu có. Dùng `Media/Audio/saigon-dawn.flac` để demo. Xác nhận quyền phát trực tuyến và chọn thêm quyền tải FLAC cho VIP nếu được phép. Server đọc thời lượng từ FLAC, tạo **MP3 128 kbps / 320 kbps** rồi lưu bài **Pending** với mã SS-TRACK. Nhà phân phối theo dõi kết quả trong khu vực của mình.
3. Đăng nhập admin, mở **/Admin**, vào **Kiểm tra bản nhạc**, nghe thử rồi duyệt hoặc từ chối kèm lý do. Chỉ **Approved** công khai/nghe MP3; hồ sơ quyền và mã nguồn chỉ admin cùng nhà phân phối sở hữu xem.

**Giới hạn tải lên:** FLAC gốc bắt buộc ≤ 50 MB; ảnh PNG/JPG tùy chọn ≤ 2 MB. Ảnh chọn riêng được ưu tiên; nếu không chọn, hệ thống tìm ảnh nhúng PNG/JPEG hợp lệ trong FLAC (ưu tiên bìa trước), tối đa 2 MB, tạo JPEG tối đa 1.600 px mỗi chiều. Không có ảnh dùng được thì dùng bìa mặc định. Thời lượng thực đọc từ tệp phải lớn hơn 0 và không vượt 1.800 giây; giá trị lưu làm tròn lên giây. MP3 được tạo tự động từ nguồn FLAC, không nhận MP3 tải kèm. Hai mã nguồn tùy chọn ≤ 64 ký tự mỗi trường, là mã mô phỏng, không phải ISRC/UPC được cấp/xác minh. **/Admin/Import** vẫn là tùy chọn nhập thủ công cho admin.

Xem [hướng dẫn mô phỏng và test case](docs/07-distribution-simulation.md).

## Demo thanh phát nhạc

1. Bấm **Phát** ở một bài trong danh mục hoặc ở trang chi tiết. Thanh phát cố định ở đáy màn hình hiện ảnh bìa, tên bài và nghệ sĩ.
2. Thử tạm dừng, phát tiếp, tua, chỉnh âm lượng và tắt tiếng. Cuộn trang để kiểm tra thanh vẫn hiện. Nút đóng dừng bài và ẩn thanh.
3. Trong danh mục, thử bài trước, bài tiếp và để bài phát hết. Thanh dùng thứ tự của danh sách đang mở và tự chuyển khi còn bài tiếp theo. Trang chi tiết phát một bài.
4. Mở một trang khác để kiểm tra phiên phát kết thúc khi MVC tải trang mới. Khi tắt JavaScript, mở trang chi tiết để dùng điều khiển audio của trình duyệt.

[Thanh phát trên desktop](docs/screenshots/18-thanh-phat-desktop.png), [màn hình hẹp](docs/screenshots/19-thanh-phat-mobile.png) và [phiên VIP phát FLAC](docs/screenshots/20-thanh-phat-vip.png).

## Demo VIP

1. Đăng nhập `listener2` để thử mua gói, hoặc dùng `thuong`, `vip` và `viphethan` để so sánh ba trạng thái. Phát **Saigon Dawn** để kiểm tra nhãn MP3 hoặc FLAC trên thanh.
2. Mở **Gói VIP**, chọn **Tạo đơn đăng ký VIP** cho gói **29.000 đ / 30 ngày**. Đơn chuyển khoản mô phỏng ghi số tiền, nội dung và hạn 15 phút. Nếu đóng trang, mở lại Gói VIP để tiếp tục đơn còn hạn.
3. Chọn **Mô phỏng đã nhận tiền** hoặc **Mô phỏng giao dịch thất bại**. Chỉ kết quả thành công cấp VIP. Đơn đã xử lý hoặc hết hạn giữ nguyên kết quả khi gửi lại. Trang Gói VIP hiển thị hạn thuê bao theo giờ Việt Nam và các đơn gần đây.
4. Phát lại **Saigon Dawn**. Thanh có nút **Tải FLAC** nếu bài cho phép tải và VIP còn hạn. Máy chủ kiểm tra hạn VIP và trạng thái duyệt khi cấp tệp.
5. Bấm nhãn **MP3** hoặc **FLAC** trên thanh phát hay trang bài hát để mở menu chất lượng. VIP còn hạn có thể đổi nguồn tại vị trí đang nghe. Listener thường và hết hạn thấy khóa VIP ở bài có FLAC.

## Demo bài hát yêu thích

1. Đăng nhập bằng tài khoản Listener như `thuong` hoặc `vip`. Mở bài đã duyệt và bấm trái tim cạnh nút **Phát** hoặc cạnh tên bài trên thanh phát.
2. Mở **Yêu thích** ở thanh bên. Danh sách riêng của tài khoản hiển thị bài lưu gần nhất trước. Bấm **Phát** để nghe bài từ danh sách.
3. Bấm lại trái tim để bỏ lưu. Khi thao tác trên thanh phát, nhạc tiếp tục ở vị trí hiện tại.

## Phạm vi và giới hạn

- Thanh phát dùng JavaScript để điều khiển phần tử audio HTML. Hai form gửi nhạc dùng JavaScript riêng cho tiến độ và hủy. Phiên phát gắn với trang MVC đang mở.
- SQL Server lưu metadata/tên tệp; âm thanh và ảnh upload ở vùng riêng, cấp qua endpoint kiểm tra quyền.
- VIP là thuê bao có thời hạn dành cho người nghe. Tạo và xác nhận đơn chuyển khoản mô phỏng chỉ hoạt động trong Development.
- Không có tiền bản quyền, chia doanh thu, ISRC/UPC thật hay trao đổi server-to-server. Luồng mới đã thêm chuyển FLAC sang MP3 128/320 kbps trên máy chạy ứng dụng.
- Dữ liệu mẫu gồm Saigon Dawn, Afterglow, Quiet Orbit và một bản nháp, do nhóm tạo; [nguồn/cách tái tạo](docs/media-assets.md).
- `/Songs/FlacPreview/{id}` và `/Songs/HighQualityPreview/{id}` phục vụ admin kiểm tra kỹ thuật trong Development. Người nghe VIP sử dụng `/Songs/StreamFlac/{id}` và `/Songs/Download/{id}`.

## Tài liệu và bằng chứng

- [Phạm vi](docs/01-pham-vi.md)
- [ERD và phân quyền](docs/02-du-lieu-va-phan-quyen.md)
- [Kế hoạch tuần 1](docs/03-ke-hoach-tuan-1.md)
- [Test case và trạng thái kiểm chứng](docs/04-demo-va-kiem-thu.md)
- [Báo cáo tuần 1](docs/05-bao-cao-tuan-1.md)
- [Chính sách nền tảng/bản quyền](docs/06-quy-dinh-nen-tang.md)
- [Nhà phân phối tải lên → admin xét duyệt](docs/07-distribution-simulation.md)
- [Kiểm thử thanh phát trên trình duyệt](docs/player-browser-verification.json)

**Kiểm chứng ngày 27/10/2026, 12:26–12:27 UTC+07:** build **0 lỗi/0 cảnh báo**, **59/59 ca HTTP đạt**: [17 tiến độ/ảnh nhúng](docs/upload-progress-verification.json), [16 xử lý FLAC](docs/flac-processing-verification.json), [26 tài khoản nguồn](docs/distributor-account-verification.json). Đã quan sát tiến độ FFmpeg từ 0 đến 97%, hoàn thành và lấy JPEG nhúng 320×240; ảnh chọn riêng được ưu tiên và ảnh trích xuất không dùng được dọn. Hai MP3 vẫn đúng 128000/320000 bit/s, FLAC gốc không đổi. Kết quả HTTP không thay cho kiểm tra % byte/cancel/retry trong trình duyệt; chưa thử mất bộ xử lý, ép timeout, ngắt kết nối, toàn bộ biên lớn hoặc nghe qua loa. [Test case và giới hạn bằng chứng](docs/08-tien-do-va-anh-bia.md). [Ảnh form 15](docs/screenshots/15-upload-flac-tu-dong.png) là giao diện trước bổ sung tiến độ, chỉ giữ để đối chiếu lịch sử.

**Đăng nhập demo:** [9/9 ca HTTP đạt](docs/demo-login-verification.json), 12:28:57–12:28:58 UTC+07: ô đăng nhập kiểu text, bốn tên ngắn và bốn email tương ứng dùng mật khẩu `123456`, đúng khu vực/vai trò; không tạo/sửa tài khoản hay bài trong bộ test. Tổng bốn bộ là **68/68 HTTP**, gồm 59 ca chức năng và 9 ca đăng nhập.

Đã quan sát đăng nhập `distrokite / 123456`, form mới và [bìa FLOWER (18)](docs/screenshots/17-flower-cover.png) trong trình duyệt; bài vẫn Pending. Quyền chọn tệp để thử upload qua trình duyệt bị từ chối trong phiên kiểm thử, nên chưa quan sát % byte/hủy/thử lại qua UI và không có ảnh tiến độ upload. Kết quả HTTP/chuyển mã nêu trên vẫn được ghi riêng.

**Bằng chứng lịch sử:** [44 ca HTTP nền tảng](docs/verification-results.json), [24 ca admin nhận/nhập gói](docs/distributor-verification.json), [7 quan sát Edge](docs/admin-browser-verification.json) của bản hai vai trò trước đây. Các kết quả này không chứng minh luồng tài khoản nhà phân phối mới đã đạt; xem [test case hiện tại](docs/04-demo-va-kiem-thu.md) và kết quả kiểm chứng tương ứng. Giới hạn dung lượng lớn, một số giá trị biên và máy thành viên thứ hai cần kiểm tra riêng.

## CSDL khi bàn giao

- ZIP nguồn chỉ kèm bốn tệp âm thanh demo gốc; nhạc/ảnh người dùng tải lên lúc chạy được giữ nguyên trên máy và không đưa vào ZIP. Nếu bàn giao cả CSDL local đã có các bài tải lên, cần sao chép riêng các tệp media tương ứng.
- Các bản xuất SQL dưới đây lưu lại schema trước khi bổ sung VIP và bài hát yêu thích. CSDL hiện tại được cập nhật từ migration EF trong `Data/Migrations`. [Script migration lịch sử](database/migrations.sql), [schema lịch sử](database/schema.sql), [tool manifest](dotnet-tools.json).
- Chạy dotnet tool restore nếu cần công cụ EF. Cách chạy thông thường là để ứng dụng áp dụng migrations trong Development; không chạy hai script tạo cùng schema nối tiếp.
- Lịch sử migrations có thể chứa mô hình đã thay đổi; đối chiếu schema/snapshot sau migration mới nhất để biết thiết kế đang dùng.
- Việc chạy trên một máy không thay cho kiểm tra trên máy thành viên thứ hai.
