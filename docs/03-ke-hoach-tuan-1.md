# Kế hoạch tuần 1

## Mục tiêu

Có bản chạy local: **Listener/Distributor/Admin đăng nhập → đúng khu vực → dữ liệu bài từ SQL Server → phát MP3 → rời trang thì dừng**. Luồng nội dung tối thiểu: **nhà phân phối tải nhạc lên → Pending → admin kiểm tra/duyệt → người nghe thấy bài**.

DistroKite Demo và TuneKore Demo có tài khoản cấp sẵn trong Development, mỗi tài khoản gắn một nguồn. Đăng ký công khai chỉ tạo Listener. Không cần xây website thứ hai. VIP để giai đoạn tiếp theo.

## Phân công và lịch năm ngày

| Ngày | A — tài khoản/nhà phân phối | B — dữ liệu/admin/phát nhạc | Kết quả chung |
|---|---|---|---|
| 1 | Chốt ba vai trò, quyền route | Chốt Distributors/Songs, metadata và mã | Phạm vi, ERD, SQL Server, chia task |
| 2 | Identity, đăng ký/đăng nhập/đăng xuất | Migration/seed, danh mục và MP3 | Bản nền tảng chạy trên máy nhóm |
| 3 | Form tải nhạc và danh sách bài của nguồn mình | Khu vực admin, chi tiết/audio và tệp private | Ghép luồng tải bài Pending |
| 4 | Kiểm tra cách ly hai nguồn và dữ liệu lỗi | Duyệt/từ chối có lý do, mã bài và thông tin quyền | Pending không công khai, Approved nghe được |
| 5 | Review và thử route trái quyền | Đối chiếu test, dữ liệu, tài liệu | Demo, bằng chứng, lỗi/backlog tuần sau |

A/B là phân công đề xuất, cần thay bằng tên thành viên thực tế. Mỗi người làm cả giao diện và xử lý cho phần mình; review chéo. Ngày 6–7 để dự phòng.

## Checklist nghiệm thu

- [ ] Hai thành viên cấu hình SQL Server, FFmpeg/ffprobe và chạy được theo README.
- [ ] Migration/seed tạo nguồn và bốn tài khoản demo mà không tạo bản trùng khi khởi động lại.
- [ ] Đăng ký mới chỉ Listener; không tự nâng quyền bằng trường role/DistributorId gửi thêm.
- [ ] DistroKite Demo và TuneKore Demo đăng nhập, tải nhạc đúng nguồn và xem bài của mình.
- [ ] Không đọc hồ sơ hoặc tệp chưa công khai của nguồn khác bằng cách đổi ID.
- [ ] Chỉ upload FLAC ≤ 50 MB, ảnh tùy chọn 2 MB; không có ô giới thiệu/thời lượng hoặc MP3.
- [ ] Server tự đọc thời lượng, tạo MP3 128/320 kbps; FLAC hỏng/giả hoặc thiếu bộ xử lý không tạo bài/tệp thừa.
- [ ] Form hiện % byte tải lên và % chuyển mã thực ở hai giai đoạn; không kẹt chờ khi validation/mất mạng/timeout; tránh gửi lặp, có hủy.
- [ ] Chỉ user sở hữu đọc tiến độ; Start có CSRF, UploadId không dùng lại; tắt JavaScript vẫn POST và xử lý được.
- [ ] Ưu tiên ảnh chọn riêng, tự lấy bìa PNG/JPEG nhúng hợp lệ trong FLAC nếu không chọn ảnh; không có ảnh dùng được thì dùng mặc định.
- [ ] Bài mới luôn Pending; trường trạng thái hoặc người nhập gửi thêm không có hiệu lực.
- [ ] Chỉ admin duyệt/từ chối; từ chối có lý do; gửi lại quyết định không đổi bài đã xử lý.
- [ ] Chỉ Approved công khai; metadata quyền/mã nguồn không lộ cho người nghe.
- [ ] Có mã SS-TRACK nội bộ; mã nguồn ghi rõ mô phỏng, không ISRC/UPC thật.
- [ ] MP3 phát bằng controls mặc định; rời trang thì dừng.
- [ ] Chính sách/test case có tiền điều kiện, bước, mong đợi, thực tế, trạng thái và bằng chứng.
- [ ] Báo cáo phân biệt bằng chứng cũ, kiểm chứng luồng mới và việc chưa thử trên máy thứ hai.

Chỉ đánh dấu khi thực tế đã kiểm tra. Dùng bảng Cần làm → Đang làm → Chờ review → Hoàn thành; chia task nhỏ và ghép thường xuyên. [Kịch bản demo/test chi tiết](07-distribution-simulation.md).
