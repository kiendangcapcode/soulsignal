# Nhà phân phối tải nhạc lên → admin kiểm tra → duyệt

## Demo cùng một website

1. Đăng nhập **distrokite** hoặc **tunekore**, mật khẩu demo **123456**, mở **/Distributor**. Chỉ dùng tài khoản đơn giản này trong Development; email tương ứng vẫn đăng nhập được.
2. Mở **Tải nhạc lên**. Nguồn được xác định từ tài khoản; nhập tên bài/nghệ sĩ/thể loại, thông tin quyền và mã nguồn nếu có; chỉ chọn **FLAC gốc** cùng ảnh tùy chọn. Không nhập giới thiệu/thời lượng hoặc chọn MP3. Dùng `Media/Audio/saigon-dawn.flac` để trình diễn. Server đọc thời lượng rồi tạo MP3 128/320 kbps trước khi lưu bài chờ duyệt.
3. Gửi bài và theo dõi **% tải tệp**. Khi tải xong, form chuyển sang đọc FLAC, **% chuyển mã MP3 thực**, lấy bìa và lưu. Tải tệp đạt 100% chưa phải bài đã lưu. Xử lý thành công mới có bài **Pending**, mã SS-TRACK và xuất hiện trong danh sách của nguồn mình. Nhà phân phối mở hồ sơ để xem trạng thái và kết quả xét duyệt. Tắt JavaScript vẫn POST thường và chờ trang kết quả; không có % trực tiếp.
4. Đăng xuất, đăng nhập **admin / 123456**, mở **/Admin**. Chọn **Kiểm tra bản nhạc**, nghe thử, xem mã nguồn/chủ thể/căn cứ quyền rồi **Duyệt** hoặc **Từ chối**. Từ chối phải có lý do.
5. Dùng Listener/khách để kiểm tra: Approved xuất hiện/nghe MP3 được; Pending/Rejected không công khai. Nhà phân phối thấy trạng thái và lý do từ chối trong khu vực của mình.

DistroKite Demo/DKT-DEMO và TuneKore Demo/TKR-DEMO là tên hư cấu, không kết nối hãng thật. Không cần website ngoài, API, DDEX XML, SFTP/S3, ISRC/UPC thật hoặc tiền bản quyền. Đây là quy trình mô phỏng đơn giản của đồ án.

## Tài khoản và dữ liệu

| Thành phần | Ý nghĩa |
|---|---|
| Distributor | Vai trò tài khoản được cấp sẵn, không tự đăng ký công khai |
| ApplicationUser.DistributorId | Nguồn cố định của tài khoản nhà phân phối |
| Song.DistributorId / ImportedById | Nguồn bài và tài khoản thực hiện tải lên; do server xác định |
| Artist | Nghệ sĩ được ghi trong metadata, không thêm vai trò |
| ReferenceCode | Mã theo dõi nội bộ SS-TRACK-000001, tính từ ID |
| ExternalRecordingCode / ExternalReleaseCode | Mã nguồn mô phỏng tùy chọn, ≤ 64 ký tự mỗi trường |
| RightsHolder / RightsEvidence | Khai báo chủ thể/căn cứ quyền để xét duyệt, không tự chứng minh sở hữu |
| ReviewNote / ReviewedAt / Reviewer | Ghi chú/lý do, thời điểm và tài khoản admin xét duyệt |

Cùng nguồn không nhận trùng mã bản ghi có giá trị; nguồn khác có thể dùng cùng mã. Nhà phân phối không đổi nguồn, người nhập hoặc trạng thái bằng dữ liệu POST. Danh sách/hồ sơ của nguồn khác không được cấp cho tài khoản này, kể cả khi đoán ID.

## Tệp và nhập thủ công

FLAC gốc bắt buộc ≤ 50 MB; PNG/JPG chọn riêng tùy chọn ≤ 2 MB. Thứ tự bìa: ảnh chọn riêng → PNG/JPEG nhúng hợp lệ trong FLAC → ảnh mặc định. Ảnh nhúng ưu tiên bìa trước, ≤ 2 MB, chuyển JPEG giữ tỷ lệ tối đa 1.600 px mỗi chiều. Server đọc thời lượng thực (>0 đến 1.800 giây, lưu làm tròn lên giây), xác nhận FLAC và giải mã để tạo MP3 CBR 128/320 kbps, stereo 44.100 Hz. Không yêu cầu MP3, giới thiệu hoặc thời lượng do người gửi nhập. 1 MB = 1.024 × 1.024 byte. FFmpeg/ffprobe cần có trên máy chạy ứng dụng; [cấu hình trong README](../README.md). Tệp hỏng/giả, thiếu bộ xử lý hoặc timeout không tạo bài; dọn các tệp vừa tạo khi xử lý/lưu thất bại. Chuyển mã không tự xác minh bản quyền.

**/Admin/Import** vẫn cho admin tiếp nhận tệp thủ công khi cần; đây là đường bổ sung. Tệp bài chưa công khai chỉ được cấp cho admin hoặc đúng nguồn sở hữu. FLAC và MP3 320 kbps preview hiện chỉ phục vụ admin kiểm tra kỹ thuật trong Development; FLAC/VIP/tải chính thức cho người nghe còn dự kiến.

## Kiểm chứng hiện tại

Ngày **27/10/2026, 12:26–12:27 UTC+07**, build **0 lỗi/0 cảnh báo**; **59/59 ca HTTP đạt** gồm [17 tiến độ/ảnh nhúng](upload-progress-verification.json), [16 xử lý FLAC](flac-processing-verification.json) và [26 tài khoản nguồn](distributor-account-verification.json). Tiến độ server của FLAC 180 giây đã quan sát từ 0 đến 97%, bìa JPEG nhúng được lấy và ảnh chọn riêng được ưu tiên. [Test case tiến độ/ảnh](08-tien-do-va-anh-bia.md) nêu giới hạn chính xác; HTTP không chứng minh % byte/cancel/retry trong trình duyệt hoặc nghe qua loa. [Ảnh form 15](screenshots/15-upload-flac-tu-dong.png) thuộc bản trước tiến độ.

[9 ca đăng nhập demo](demo-login-verification.json) đạt lúc 12:28:57–12:28:58, gồm bốn tên ngắn/bốn email với `123456` và kiểm tra ô text; tổng độc lập là **68/68 HTTP** (59 chức năng + 9 đăng nhập). Trình duyệt xác nhận DistroKite đăng nhập, form mới và [ảnh bìa FLOWER 18](screenshots/17-flower-cover.png), giữ Pending. Chọn tệp để kiểm thử bằng trình duyệt bị từ chối cấp quyền; upload/% byte/hủy/thử lại UI chưa được kiểm chứng, không có ảnh tiến độ upload.

Bộ tiến độ giữ 19/20 Pending; FLAC giữ 21 Approved / 22 Pending; tài khoản giữ 23 Approved / 24 Rejected. Các bộ HTTP không sửa bài có trước, 10/11 vẫn Pending. Riêng bìa FLOWER (18) được bổ sung từ FLAC đã gửi theo yêu cầu người dùng, giữ Pending. MP3 vẫn đúng 128000/320000 bit/s, 24 giây và SHA256 FLAC không đổi. Đổi đuôi MP3, FLAC header giả hoặc cắt giữa dữ liệu bị từ chối, không có bài/tệp thừa. Các giới hạn tối đa, thiếu bộ xử lý, ép timeout và hủy yêu cầu chưa có bằng chứng trong ba bộ HTTP này.

ID **ca tài liệu DIST-01–11** dưới đây khác ý nghĩa **HTTP:DIST-00–25** trong JSON; chỉ đánh dấu theo đúng nội dung đã thử. Kiểm tra HTTP không chứng minh đã nghe qua loa hoặc mọi biến thể biên đã đạt.

| ID | Tiền điều kiện và bước | Mong đợi | Thực tế/trạng thái/bằng chứng |
|---|---|---|---|
| DIST-01 | Đăng nhập lần lượt hai tài khoản nguồn | Đến khu vực Distributor, đúng tên nguồn, có nút tải lên | Pass HTTP: DIST-05/06/07 |
| DIST-02 | Gửi FLAC gốc hợp lệ, metadata/quyền đầy đủ | Tạo Pending, đúng nguồn, có mã nội bộ, chưa công khai | Pass HTTP: DIST-14/16/19; ImportedBy cần đối chiếu SQL riêng |
| DIST-03 | Gửi thêm DistributorId nguồn khác, ImportedById, Status=Approved | Server dùng tài khoản và Pending; không đổi nguồn/người nhập/trạng thái theo client | Partial: HTTP:DIST-14 xác nhận đúng nguồn/Pending, không lưu ghi chú giả; chưa đối chiếu ImportedBy bằng SQL |
| DIST-04 | Nguồn B đoán ID bài Pending/Rejected của nguồn A | Không đọc được hồ sơ, audio hoặc ảnh tải lên của nguồn A; không thấy trong danh sách của B | Partial: HTTP:DIST-17 đạt với Pending; chưa lặp kiểm tra chéo sau Rejected |
| DIST-05 | Khách/Listener gọi khu vực tải lên; Distributor gọi Admin hoặc xét duyệt | Không được thao tác trái quyền, không thay đổi dữ liệu | Partial: GET route bị chặn, HTTP:DIST-01/03/08; chưa gửi POST xét duyệt trái quyền riêng |
| DIST-06 | POST thiếu CSRF; thiếu tệp/xác nhận; giả định dạng; quá giới hạn | Không tạo bài hoặc lưu tệp lỗi; có thông báo validation phù hợp | Partial: HTTP:DIST-09–13 đạt trên form FLAC-only cho CSRF/thiếu FLAC/giả định dạng/thiếu xác nhận; chưa thử quá dung lượng hoặc toàn bộ ảnh/biên |
| DIST-07 | Gửi mã trùng cùng nguồn/khác nguồn; thử mã >64 ký tự | Trùng cùng nguồn và quá dài bị chặn; khác nguồn có thể dùng cùng mã | Partial: HTTP:DIST-15/16 đạt cho mã trùng/cùng mã khác nguồn; mã 64/65 ký tự chưa thử |
| DIST-08 | Admin duyệt Pending rồi khách/Listener mở danh mục/stream | Approved công khai/nghe MP3; hồ sơ quyền và mã nguồn không công khai | Pass HTTP:DIST-19/20/22/25; HTTP chỉ kiểm tra cấp/range tệp, không nghe qua loa |
| DIST-09 | Admin từ chối thiếu/có lý do; nguồn sở hữu xem lại | Thiếu lý do bị chặn; đủ lý do Rejected, nguồn thấy kết quả, không công khai | Pass HTTP:DIST-21/23 |
| DIST-10 | Gửi lại quyết định cho bài đã xử lý | Không đảo quyết định đã lưu | Pass HTTP:DIST-24 |
| DIST-11 | Đăng ký gửi thêm role/nguồn | Luôn Listener, không tự cấp quyền nguồn hoặc Admin | Not run trong bộ tài khoản nguồn mới; không suy ra từ kiểm thử đăng ký của bản cũ |

Ghi kết quả thực tế, ngày/build, ID bài, môi trường và liên kết log/ảnh cho mỗi ca. Các giá trị biên dung lượng/thời lượng cần ghi riêng; không coi thử một tệp nhỏ là đã thử đủ biên.

## Kiểm thử pipeline mới

Tham chiếu **FLAC-01–09** trong [test case hiện tại](04-demo-va-kiem-thu.md): bỏ ba trường đầu vào, tự đọc thời lượng, không tin thời lượng giả, đo bitrate 128/320, FLAC hỏng/giả, bộ xử lý thiếu/timeout, dọn tệp và giữ phân quyền. Bộ HTTP:FLAC-00–15 đã đạt 16/16; bảng ca tài liệu phân biệt Pass/Partial/Not run theo nội dung thực tế, nhất là thiếu encoder/timeout/hủy và giới hạn tối đa chưa thử.

## Phiên tiến độ và phản hồi lỗi

JavaScript chỉ chạy trên Distributor/Upload và Admin/Import. Form mở UploadId gắn user qua POST chống CSRF, gửi multipart bằng XMLHttpRequest rồi đọc trạng thái của đúng user. UploadId được nhận một lần, trạng thái giữ tạm trong RAM và hết hạn sau 30 phút; không phải hàng đợi nền hoặc lịch sử CSDL. Không thể dùng API tiến độ để đọc nhạc của nguồn khác hay tự duyệt bài.

Nếu validation/xử lý lỗi, form hiển thị thông báo và cho sửa/gửi lại; file đã chọn được giữ khi không tải lại trang. Có nút hủy và giới hạn chờ 4 phút ở trình duyệt. Nếu mất kết nối hoặc chưa rõ kết quả, kiểm tra danh mục đã gửi trước khi thử lại để tránh gửi trùng. Không giả lập % trong các giai đoạn không đo được. [Ca kiểm thử chi tiết](08-tien-do-va-anh-bia.md).

## Bằng chứng lịch sử

[44 HTTP nền tảng](verification-results.json), [24 ca nhận/nhập gói bằng admin](distributor-verification.json) và [7 quan sát Edge](admin-browser-verification.json) ghi lại phiên bản chỉ có Listener/Admin trước thay đổi này. Giữ để đối chiếu lịch sử; không dùng để đánh dấu DIST-01–11 đã Pass. Xem [test case tổng](04-demo-va-kiem-thu.md).
