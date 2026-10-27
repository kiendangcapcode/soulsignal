# Báo cáo tuần 1 — Soul Signal

## Thông tin nhóm

| Mục | Nội dung cần điền |
|---|---|
| Thành viên A | [Họ tên — mã sinh viên] |
| Thành viên B | [Họ tên — mã sinh viên] |
| Lớp/giảng viên | [Điền thông tin] |
| Kỳ báo cáo | Tuần 1 và phần bổ sung luồng nhà phân phối |
| Công nghệ | ASP.NET Core MVC .NET 10, Razor, Identity, EF Core, SQL Server |
| Phiên bản nộp | [Điền commit/tag hoặc ngày đóng gói] |

## Chức năng và luồng trình bày

Soul Signal dùng ba vai trò **Listener, Distributor, Admin** trên cùng một website. Người nghe đăng ký/đăng nhập, tìm và nghe MP3 bài đã duyệt. DistroKite Demo và TuneKore Demo có tài khoản cấp sẵn, tải một FLAC gốc, ảnh và metadata; server gắn đúng nguồn, tự đọc thời lượng và tạo MP3 128/320 kbps rồi lưu Pending. Không nhập giới thiệu hoặc thời lượng và không cần chuẩn bị MP3. Admin xem bài chờ duyệt, kiểm tra mã/thông tin quyền, nghe thử rồi duyệt hoặc từ chối có lý do. Nhà phân phối theo dõi kết quả của nguồn mình; không đọc hồ sơ/tệp chưa công khai của nguồn khác.

Các đơn vị là tên giả, không kết nối hãng thật. Không cần website thứ hai. Đăng ký công khai chỉ tạo Listener. Mã SS-TRACK là mã nội bộ; mã nguồn mô phỏng không phải ISRC/UPC đã xác minh. Admin/Import vẫn cho tiếp nhận thủ công khi cần.

Khi báo cáo, demo **Distributor tải bài → Pending chưa công khai → Admin duyệt → Listener tìm và nghe bài**. Form báo % tải tệp, sau đó trạng thái/% chuyển mã thực và tự lấy bìa có sẵn trong FLAC nếu không chọn ảnh riêng. Sau đó thử nguồn B truy cập bài chưa công khai của nguồn A để chứng minh kiểm tra quyền phía server.

## Tài liệu đã chuẩn bị

| Hạng mục | Tài liệu |
|---|---|
| Phạm vi, công nghệ, tiến độ | [Phạm vi](01-pham-vi.md), [kế hoạch](03-ke-hoach-tuan-1.md) |
| Dữ liệu và phân quyền | [ERD/ma trận quyền](02-du-lieu-va-phan-quyen.md) |
| Test case | [Nền tảng và cách ghi kết quả](04-demo-va-kiem-thu.md), [DIST-01–11](07-distribution-simulation.md) |
| Chính sách | [Quy định nền tảng](06-quy-dinh-nen-tang.md), trang /Home/Policy |
| Hướng dẫn chạy/tài khoản | [README](../README.md) |

## Phần bổ sung tiến độ và ảnh bìa

Đã bổ sung JavaScript nhỏ chỉ trên hai form tải nhạc: % tải lấy từ số byte thực, % chuyển mã lấy từ FFmpeg; không tạo % bằng bộ đếm thời gian. Phân biệt tải xong với xử lý/lưu xong, hiển thị lỗi tại form, tránh gửi lặp và hỗ trợ hủy/chờ có giới hạn. Tắt JavaScript vẫn gửi POST thường. Nhạc vẫn phát bằng controls mặc định và dừng khi chuyển trang.

Ưu tiên ảnh người gửi chọn riêng; nếu không có, trích PNG/JPEG nhúng trong FLAC (ưu tiên bìa trước, ≤ 2 MB), tạo JPEG giữ tỷ lệ tối đa 1.600 px mỗi chiều; không có bìa dùng được thì dùng mặc định. Việc tự lấy ảnh không thay đổi yêu cầu về quyền nội dung. Bằng chứng mới theo [test case tiến độ/ảnh](08-tien-do-va-anh-bia.md).

## Bằng chứng hiện tại

Ngày **27/10/2026, 12:26–12:27 UTC+07**, build **0 lỗi/0 cảnh báo**, **59/59 ca HTTP đạt**: [17 tiến độ/ảnh nhúng](upload-progress-verification.json), [16 xử lý FLAC](flac-processing-verification.json), [26 tài khoản nguồn](distributor-account-verification.json). Bộ HTTP kiểm tra trạng thái server, JSON lỗi, tệp và quyền; không khẳng định đã thử % byte/cancel/retry trong trình duyệt hoặc nghe qua loa. [Ảnh form 15](screenshots/15-upload-flac-tu-dong.png) thuộc bản trước tiến độ, chỉ giữ lịch sử.

[Bộ đăng nhập demo](demo-login-verification.json) đạt thêm **9/9 HTTP** lúc 12:28:57–12:28:58: form text, bốn bí danh và bốn email với mật khẩu `123456`, đúng vai trò/khu vực; không tạo/sửa dữ liệu. Tổng bốn bộ là **68/68 HTTP** (59 chức năng + 9 đăng nhập). Trong trình duyệt đã đăng nhập được `distrokite / 123456`, quan sát form mới và [bìa FLOWER 18](screenshots/17-flower-cover.png), giữ Pending. Thao tác chọn tệp qua trình duyệt không được cấp phép trong phiên kiểm thử, nên % byte/hủy/thử lại UI chưa được xác nhận; không có ảnh tiến độ upload.

| Phạm vi nghiệm thu | Kết quả thực tế |
|---|---|
| Bỏ giới thiệu/thời lượng/MP3 đầu vào | Hai form đạt HTTP:FLAC-02; JavaScript chỉ phục vụ tiến độ ở hai form |
| Tải FLAC rồi tự xử lý ở cả hai đường nhập | HTTP:FLAC-07/08/11; bài 21/22 tự đọc 24 giây, Pending trước khi duyệt |
| Hai bản MP3 và FLAC nguyên gốc | HTTP:FLAC-09–12; bitrate 128000/320000 bit/s, stereo 44.100 Hz, 24 giây, SHA256 FLAC giữ nguyên |
| FLAC giả/header/cắt dữ liệu | HTTP:FLAC-04–06; không tạo bài hoặc để lại tệp mới |
| Nguồn cố định, không tin dữ liệu client, cách ly bài chưa duyệt | HTTP:DIST-14/17–19 và HTTP:FLAC-08/13; ImportedBy cần đối chiếu SQL riêng |
| Duyệt/từ chối, phản hồi và công khai đúng quyền | HTTP:DIST-21–24; HTTP:FLAC-15 xác nhận chỉ MP3 128 công khai,320/FLAC giữ quyền admin Development |
| HTTP range/MIME | HTTP:FLAC-14 đạt cho MP3 128/320 và FLAC; không thay cho nghe qua loa |
| Tiến độ server và quyền phiên | HTTP:PROGRESS-02–05/09/10/12; đúng owner/CSRF, không tái dùng UploadId, quan sát encoding 0–97% không giảm |
| Lấy ảnh nhúng và ảnh chọn riêng ưu tiên | HTTP:PROGRESS-11/13/16; JPEG nhúng 320×240, ảnh riêng thắng và bìa trích không dùng được dọn |
| Lỗi AJAX và FLAC hỏng | HTTP:PROGRESS-07/08; JSON lỗi/trạng thái failed, không để lại bài hoặc tệp của lượt thất bại |

Bộ tiến độ giữ bài 19/20 Pending; FLAC giữ 21 Approved / 22 Pending; tài khoản giữ 23 Approved / 24 Rejected. Các bộ HTTP không sửa dữ liệu có trước. Riêng FLOWER (18) được bổ sung bìa từ chính FLAC người dùng đã gửi theo yêu cầu, giữ Pending. Không ấn định số bài trong CSDL vì demo có thể thêm dữ liệu hợp lệ. Chỉ dọn tệp/tài khoản thử khi xác định đúng dữ liệu tạm.

Kết quả 44 HTTP, 24 ca admin, 7 quan sát Edge và mốc 42 ca trước phần tiến độ thuộc phiên bản trước; không cộng chúng vào 59 ca hiện tại. [Ảnh form 14 cũ](screenshots/14-tai-nhac-nha-phan-phoi.png) còn giới thiệu/thời lượng/MP3, chỉ giữ để đối chiếu lịch sử.

## Phần bổ sung FLAC gốc

Đã đổi giao diện sang FLAC-only, bỏ giới thiệu/thời lượng nhập tay, hiển thị thời lượng tự đọc và hai bản MP3 sinh từ nguồn. Xem [FLAC-01–09 và phạm vi đã thử](04-demo-va-kiem-thu.md). Bộ xử lý cần FFmpeg/ffprobe theo README. Các bài lịch sử được giữ nguyên, không suy ra chúng đã có bản MP3 320 kbps.

## Giới hạn và tuần tiếp theo

- Chưa coi VIP, thanh toán mô phỏng, FLAC/tải chính thức cho người nghe là hoàn thành. FLAC preview hiện dành cho admin kiểm tra kỹ thuật trong Development.
- Luồng mới đã kiểm chứng đọc thời lượng và sinh MP3 128/320 kbps bằng tệp mẫu 24 giây; chưa thử thiếu bộ xử lý, timeout, hủy yêu cầu hoặc lỗi lưu DB. Không xác minh bản quyền tự động và chưa có hàng đợi xử lý nền.
- Chưa chạy lại đăng ký công khai trong bộ mới; chưa xác nhận tệp quá dung lượng, mã 64/65 ký tự, mọi giá trị biên, máy thành viên thứ hai, trình duyệt khác, VPS hoặc kiểm thử tải.
- Không cấp ISRC/UPC/DPID, DDEX/SFTP/S3, royalty/chia doanh thu hay quy trình công nghiệp của hãng thật.
- Tuần 2 hoàn thiện và kiểm tra luồng tải–duyệt, tuần 3 làm VIP, tuần 4 kiểm thử/bàn giao và báo cáo.

## Phân công trình bày

| Thành viên | Phần cần nắm |
|---|---|
| A — [Tên, mã sinh viên] | Identity, đăng ký/đăng nhập, vai trò, tài khoản nguồn, form tải lên và cách ly nguồn |
| B — [Tên, mã sinh viên] | SQL/ERD, danh mục/audio, mã bài/quyền, admin duyệt/từ chối và công khai |

Nhóm điền công việc thực tế và review chéo. Có mã/tài liệu được chuẩn bị sẵn không thay cho việc hai thành viên tự chạy, kiểm thử và hiểu luồng.
