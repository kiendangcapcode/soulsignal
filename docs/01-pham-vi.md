# Phạm vi Soul Signal

## Mục tiêu và mô hình hiện tại

Website nghe nhạc cho đồ án hai người/bốn tuần. Luồng chính: **nhà phân phối đăng nhập và tải nhạc lên → bài chờ duyệt → admin kiểm tra và duyệt/từ chối → người nghe tìm/nghe MP3**.

Có ba vai trò **Listener, Distributor, Admin**. Hai nguồn giả lập DistroKite Demo và TuneKore Demo có tài khoản được cấp sẵn ngay trên Soul Signal, không cần dựng website hãng đĩa riêng. Nghệ sĩ là metadata, không phải vai trò đăng nhập. Đăng ký công khai luôn tạo Listener; không có form tự xin hoặc tự cấp quyền Distributor/Admin.

Mô hình phục vụ đồ án, không phải bản sao hệ thống Amazon Music. Không kết nối DistroKid/TuneCore hay hãng thật; không DDEX XML, SFTP, S3 hoặc phát hành ra ngoài.

## Công nghệ và trải nghiệm đã chốt

- ASP.NET Core MVC trên .NET 10; Razor, Bootstrap/CSS; Identity; EF Core và SQL Server.
- Chỉ thêm JavaScript nhỏ trên hai form tải nhạc để báo % tải, trạng thái/% chuyển mã, hủy và lỗi. Phát bằng audio controls mặc định; không xây SPA.
- Điều hướng thông thường tải lại trang; rời trang nghe trong cùng tab thì nhạc dừng.
- Chạy local phục vụ báo cáo; triển khai VPS không nằm trong yêu cầu đã chốt.
- CSDL lưu metadata và tên tệp; âm thanh/ảnh tải lên được quản lý riêng trên máy chạy ứng dụng.
- FFmpeg/ffprobe trên máy server đọc thời lượng và tạo MP3 từ FLAC. Không cần JavaScript để thực hiện bước này.

## Phạm vi triển khai

| Đối tượng | Chức năng |
|---|---|
| Khách/Listener | Danh mục, tìm kiếm, chi tiết và nghe MP3 Approved; đăng ký/đăng nhập/đăng xuất |
| Distributor | Tải nhạc cho nguồn gắn với tài khoản; xem hồ sơ bài của nguồn mình, trạng thái và lý do từ chối |
| Admin | Xem tất cả nguồn/bài, nghe thử, kiểm tra metadata/quyền, duyệt hoặc từ chối Pending; nhập thủ công khi cần |
| Người nghe có VIP | Thuê bao có hạn, thanh toán mô phỏng, nghe FLAC/tải chính thức dự kiến làm sau |

Form tải lên chỉ yêu cầu **FLAC gốc ≤ 50 MB**, ảnh PNG/JPG tùy chọn ≤ 2 MB. Thứ tự ảnh bìa: ảnh chọn riêng → PNG/JPEG nhúng hợp lệ trong FLAC → ảnh mặc định. Ảnh nhúng ưu tiên bìa trước, được chuyển thành JPEG tối đa 1.600 px mỗi chiều. Không có ô giới thiệu hoặc nhập thời lượng, không cần tải MP3. Server đọc thời lượng thực từ FLAC (>0 đến 1.800 giây), tạo MP3 128/320 kbps rồi mới lưu Pending. Cần tên bài, nghệ sĩ, thể loại hợp lệ, chủ thể/căn cứ quyền và xác nhận phạm vi nghe/tải dự kiến. Dữ liệu giới thiệu cũ được giữ cho bài đã có.

% tải lên là số byte đã gửi, % chuyển mã là tiến độ thực do FFmpeg báo; hai giai đoạn hiển thị riêng. Khi tải đạt 100%, người dùng còn chờ kiểm tra/chuyển mã/lấy bìa/lưu. Tắt JavaScript vẫn gửi form và xử lý được bằng POST thường, không có tiến độ trực tiếp. Không có hàng đợi nền hay tự tiếp tục lượt xử lý khi khởi động lại server.

Nguồn phân phối và người tải do server lấy từ tài khoản; không tin nguồn/trạng thái do trình duyệt gửi thêm. Bài mới luôn Pending. Nhà phân phối không duyệt bài, không đọc hồ sơ riêng hay tệp bài chưa công khai của nguồn khác. Admin là người quyết định Approved hoặc Rejected; từ chối cần lý do.

Mã SS-TRACK-000001 theo dõi nội bộ. Hai mã nguồn tùy chọn ExternalRecordingCode/ExternalReleaseCode tối đa 64 ký tự mỗi trường, là mã mô phỏng, không phải ISRC/UPC được cấp/xác minh thật. Nhập/duyệt là thao tác vận hành, không tự chứng minh quyền sở hữu.

## Tiến độ báo cáo

| Mốc | Kết quả cần có |
|---|---|
| Tuần 1 — nền tảng và luồng tối thiểu | CSDL, ba vai trò, tài khoản, danh mục/MP3, tải nhạc chờ duyệt, chính sách và test case |
| Tuần 2 | Hoàn thiện luồng nhà phân phối tải lên–admin duyệt; kiểm tra cách ly nguồn, tệp và lỗi |
| Tuần 3 | VIP/thanh toán mô phỏng, thời hạn, nghe FLAC/tải theo quyền |
| Tuần 4 | Hoàn thiện dữ liệu, kiểm thử, hướng dẫn chạy, báo cáo và demo dự phòng |

Kết quả kiểm thử phải ghi theo phiên bản. JSON/ảnh của bản chỉ có Listener/Admin là bằng chứng lịch sử, không chứng minh luồng Distributor mới đã Pass.

## Ngoài phạm vi

Thanh toán thật, tiền bản quyền/chia doanh thu, cấp ISRC/UPC thật, gửi phát hành ra ngoài, sandbox/certification của hãng khác, livestream, AI gợi ý, lời bài đồng bộ, phát liên tục giữa các trang. Luồng mới chỉ chuyển FLAC gốc thành hai MP3 128/320 kbps, không phải hệ thống phân phối/chuyển mã quy mô lớn.

## Điều kiện hoàn thành

Sản phẩm chạy được, đạt tiêu chí test với bằng chứng thực tế, đã kiểm tra quyền/tệp/lỗi, người còn lại review và tài liệu cấu hình được cập nhật. Có mã hoặc bảng CSDL không đồng nghĩa ca kiểm thử đã Pass.
