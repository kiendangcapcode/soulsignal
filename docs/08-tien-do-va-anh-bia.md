# Tiến độ tải nhạc và ảnh bìa nhúng

## Hành vi cần báo cáo

Hai form Distributor/Upload và Admin/Import dùng một tệp JavaScript nhỏ để hiển thị tiến độ/lỗi tại chỗ. Đây là bổ sung theo yêu cầu người dùng; cấu trúc ASP.NET Core MVC và điều hướng tải lại trang vẫn giữ nguyên. Khi không chạy JavaScript, form vẫn POST thường, xử lý và chuyển đến hồ sơ bài.

| Giai đoạn | Dữ liệu hiển thị |
|---|---|
| Chuẩn bị lượt tải | Đang mở phiên; chưa có % |
| Tải tệp lên server | loaded/total của XMLHttpRequest; % byte thực |
| Đọc FLAC | Thông báo kiểm tra định dạng/thời lượng, không % giả |
| Chuyển mã | out_time_us FFmpeg / thời lượng nguồn; 0–99% thực |
| Lấy bìa và lưu bài | Trạng thái riêng, chưa tự công khai |
| Hoàn thành | Chuyển hồ sơ Pending sau khi lưu thành công |

Tải byte đạt 100% không đồng nghĩa đã tạo MP3 hoặc lưu bài. Không dùng thời gian chờ để tăng % giả. Một lượt đang chạy khóa nút gửi để tránh nhấn lặp; có hủy, giới hạn chờ 4 phút, thông báo khi phiên đăng nhập/mạng/lưu/xử lý gặp lỗi. Khi chưa chắc server đã lưu vì kết nối bị cắt, kiểm tra danh mục trước khi thử lại.

## Phiên tiến độ và quyền

POST /UploadProgress/Start có chống CSRF, chỉ Admin/Distributor mở được. ID ngẫu nhiên gắn user; GET /UploadProgress/Status/{id} chỉ trả phase/percent/message cho đúng user. POST /UploadProgress/Cancel/{id} có CSRF và đúng owner để yêu cầu dừng xử lý; JavaScript gửi yêu cầu khi hủy/dừng chờ. UploadId chỉ nhận một lần để xử lý, không dùng lại cho bài thứ hai. Trạng thái tạm lưu RAM, hết hạn sau 30 phút, giới hạn số lượt đang mở; khởi động lại ứng dụng không khôi phục tiến độ. Không thêm bảng SQL, không có hàng đợi xử lý nền.

API tiến độ không thay thế kiểm tra quyền nguồn/metadata/tệp. POST thường không có UploadId vẫn giữ phân quyền/validation và tạo Pending như trước. Nội dung trường quyền sử dụng không đổi trong lần bổ sung này.

## Ảnh bìa tự động

Thứ tự chọn ảnh: **ảnh người gửi chọn riêng → ảnh nhúng hợp lệ trong FLAC → bìa mặc định**. Trong khối PICTURE của FLAC, ưu tiên bìa trước; nếu không có thì dùng ảnh nhúng hợp lệ đầu tiên. Chỉ nhận PNG/JPEG thực, tối đa 2 MB. Ảnh nhúng được giải mã, bỏ metadata, chuyển JPEG, giữ tỷ lệ và giới hạn 1.600 px mỗi chiều. Không sử dụng đường dẫn/URL bên trong metadata để tải ảnh từ ngoài.

Ảnh nhúng hỏng, không hỗ trợ hoặc quá giới hạn không làm mất bài nhạc hợp lệ; hệ thống dùng bìa mặc định khi không chọn ảnh riêng. Ảnh chọn riêng vẫn phải qua validation của form; ảnh riêng lỗi cần được sửa hoặc bỏ chọn rồi gửi lại. Tệp ảnh tạm phải được dọn. Ảnh được lấy từ nhạc vẫn cần nằm trong quyền sử dụng người gửi khai báo; trích ảnh không chứng minh sở hữu.

## Bằng chứng hiện tại

Ngày **27/10/2026, 12:26:43–12:26:52 UTC+07**, [17/17 ca HTTP tiến độ/ảnh nhúng đạt](upload-progress-verification.json), ID **HTTP:PROGRESS-00–16**. Cùng phiên bản, [16/16 FLAC](flac-processing-verification.json) và [26/26 tài khoản](distributor-account-verification.json) chạy lại đạt; tổng **59/59 HTTP**, build **0 lỗi/0 cảnh báo**. Kết quả 42 ca trước thay đổi không được cộng thêm vào tổng này.

Fixture FLAC thực dài **180 giây**, **10.917.124 byte**, có bìa JPEG trước 320×240. Poll độc lập của cùng owner quan sát waiting/probing/encoding/cover/completed; các giá trị encoding 0, 10, 23, 34, 45, 55, 66, 77, 87, 97 không giảm. Giai đoạn saving có thể quá ngắn để poll bắt được, không giả nhận đã quan sát mọi trạng thái. Bìa lấy tự động là JPEG **320×240, 673 byte**; ảnh PNG chọn riêng **1×1, 70 byte** được giữ thay bìa nhúng, tệp trích bị thay thế được dọn.

Bài thử mới **19/20** đều Pending. Bài FLAC **21 Approved / 22 Pending** và tài khoản **23 Approved / 24 Rejected** thuộc hai bộ chạy lại; các bộ không sửa bài có trước. Riêng **FLOWER (18)** được bổ sung bìa từ FLAC người dùng đã gửi theo yêu cầu và giữ Pending; đây là thao tác trên dữ liệu người dùng, tách khỏi fixture của bộ HTTP.

[9/9 ca đăng nhập demo](demo-login-verification.json) đạt lúc 12:28:57–12:28:58 UTC+07: ô text, bốn tên ngắn và bốn email cùng mật khẩu `123456`, đến đúng vai trò/khu vực, không sửa dữ liệu. Tổng bốn bộ là **68/68 HTTP**, tách 59 chức năng và 9 đăng nhập.

Đã quan sát `distrokite / 123456` đăng nhập, các trường/hướng dẫn form mới và hồ sơ FLOWER 18 có bìa thật, vẫn Pending: [ảnh 17](screenshots/17-flower-cover.png). Quyền chọn tệp để thử upload qua trình duyệt bị từ chối trong phiên kiểm thử; vì vậy **chưa chạy upload/% byte/hủy/thử lại bằng UI**, không có ảnh tiến độ upload. Chưa nghe âm thanh qua loa. Không có ca ép thiếu encoder, timeout, ngắt kết nối, giới hạn tối đa hoặc lỗi DB trong báo cáo 17 ca.

## Test case

Các mã UP-xx dưới đây là mã tài liệu, khác **HTTP:PROGRESS-00–16** trong JSON. Chỉ ghi Pass khi đối chiếu đúng nội dung đã thử; một screenshot hoặc một ảnh JPEG nhỏ không thay cho tất cả biến thể.

| ID | Tiền điều kiện và bước | Mong đợi | Thực tế/trạng thái |
|---|---|---|---|
| UP-01 | Khách/Listener gọi Start/Status; POST Start thiếu CSRF | Không cấp phiên hoặc trạng thái trái quyền | Partial: HTTP:PROGRESS-02/03 đạt cho Start trái role/thiếu CSRF; quyền owner đọc Status đạt -05; chưa lặp riêng mọi vai trò trên Status |
| UP-02 | Nguồn A mở lượt, nguồn B/admin khác đọc hoặc gửi UploadId của A | Chỉ đúng user đọc/nhận được; không lộ trạng thái/bài | Pass HTTP:PROGRESS-04/05/09; owner đọc waiting, tài khoản khác không đọc/gửi hộ |
| UP-03 | UploadId đã nhận/xong; gửi lại hoặc dùng ID giả | Không dùng phiên một lần để tạo bài thứ hai | Partial: dùng lại bị chặn -12, Status ID không tồn tại trả 404 -05; chưa thử riêng POST với mọi dạng ID giả |
| UP-04 | Trình duyệt gửi FLAC, theo dõi upload progress | % dựa loaded/total, đạt 100% tải rồi chuyển giai đoạn; nút gửi không tạo lặp | Not run: quyền chọn tệp trên trình duyệt bị từ chối, chưa gửi file hoặc quan sát % byte |
| UP-05 | File đủ dài để quan sát chuyển mã | % encoding do FFmpeg báo, nằm 0–99 và không giảm; đọc/lấy bìa/lưu hiển thị riêng | Partial: HTTP:PROGRESS-10 quan sát % thực 0–97 không giảm và các phase; không bắt được saving/UI trong bộ này |
| UP-06 | FLAC có bìa PNG/JPEG, không chọn ảnh riêng | Tự lấy bìa, JPEG hợp lệ ≤ 2 MB và ≤ 1.600 px; bản gốc/âm thanh hợp lệ | Partial: HTTP:PROGRESS-06/10/11 đạt bìa JPEG 320×240; chưa chạy fixture PNG nhúng mới hoặc biên kích thước trong bộ này |
| UP-07 | FLAC có nhiều ảnh nhúng hoặc ảnh chọn riêng khác bìa nhúng | Ưu tiên bìa trước; ảnh chọn riêng luôn thắng ảnh nhúng | Partial: ảnh chọn riêng thắng -13, ảnh trích thừa được dọn -16; chưa thử nhiều ảnh nhúng để đối chiếu ưu tiên front |
| UP-08 | FLAC không có bìa; bìa nhúng hỏng/khác định dạng/quá giới hạn | Dùng ảnh mặc định khi không có ảnh riêng; không làm hỏng âm thanh, không rác tệp tạm | Partial: -15 chỉ đọc lại bài không ảnh đã có; chưa tải mới mọi biến thể ảnh nhúng hỏng/khác định dạng/quá giới hạn |
| UP-09 | AJAX thiếu trường/FLAC lỗi; rồi sửa và gửi lại | JSON lỗi rõ, hiển thị tại form, giữ file khi không reload, cho thử lại bằng UploadId mới | Partial: HTTP:PROGRESS-07/08 đạt JSON lỗi, failed và không rác tệp; chưa quan sát UI giữ file/sửa/gửi lại |
| UP-10 | Hủy lúc tải/chuyển mã, mất mạng hoặc hết hạn chờ | Dừng chờ, báo trạng thái phù hợp; server dọn tệp của lượt bị hủy, không đụng dữ liệu cũ | Not run: chưa có lượt upload UI vì quyền chọn tệp bị từ chối; HTTP 17 ca không thử Cancel/disconnect/timeout |
| UP-11 | Tắt JS, gửi form thường với FLAC hợp lệ | Vẫn xử lý và tạo Pending/redirect, không có % trực tiếp | Partial: POST thường/redirect đạt HTTP:FLAC-07/08 và HTTP:DIST-14/16; chưa lưu quan sát trình duyệt tắt JavaScript |
| UP-12 | Bài đã xử lý có bìa nhưng Pending; thử user khác/khách | Bìa private theo cùng quyền bài; chỉ Approved công khai | Pass với Pending HTTP:PROGRESS-14 và quyền cover HTTP:DIST-17/19/22; chưa duyệt riêng fixture bìa nhúng để lặp kiểm tra công khai |

Giữ riêng việc xác nhận ảnh hiện đúng cho bài người dùng đã gửi (FLOWER, ID 18) và các fixture mới. Không gửi lại hoặc đổi trạng thái bài thật chỉ để thử. Hủy/timeout/lỗi DB, giới hạn lớn và mọi biến thể ảnh cần có bằng chứng riêng; không suy tất cả đã đạt từ một ảnh PNG nhỏ hoặc một screenshot.
