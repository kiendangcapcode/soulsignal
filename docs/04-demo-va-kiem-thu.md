# Test case và trạng thái kiểm chứng

## Phiên bản và cách ghi kết quả

Áp dụng cho **Listener/Distributor/Admin**: nhà phân phối có tài khoản gắn nguồn, tải nhạc lên Pending; admin duyệt/từ chối; người nghe thấy Approved. Không có website hãng đĩa thứ hai hoặc API trao đổi với hãng thật.

Các file [44 HTTP nền tảng](verification-results.json), [24 ca admin nhận/nhập gói](distributor-verification.json), [7 quan sát Edge](admin-browser-verification.json) thuộc bản hai vai trò trước thay đổi này. Giữ làm lịch sử, không dùng để đánh dấu quyền tài khoản nhà phân phối đã đạt.

Mỗi lần chạy ghi ngày/giờ, build/phiên bản, người hoặc công cụ thử, OS/trình duyệt, CSDL/ID dữ liệu, kết quả thực tế, Pass/Fail/Partial/Not run và log/ảnh. Một lần kiểm tra HTTP không thay cho quan sát phát âm thanh trong trình duyệt; quan sát tự động không được ghi thành con người đã nghe qua loa.

## Bản bổ sung tiến độ và ảnh nhúng

Luồng mới có JavaScript riêng trên Distributor/Upload và Admin/Import để báo % byte tải lên, sau đó đọc % chuyển mã thực từ FFmpeg. Tắt JavaScript vẫn POST thường. Thêm ảnh bìa nhúng trong FLAC với thứ tự ảnh chọn riêng → ảnh nhúng hợp lệ → ảnh mặc định. Test case và giới hạn bằng chứng ở [tài liệu 08](08-tien-do-va-anh-bia.md).

## Kết quả hiện tại

Ngày **27/10/2026**, build **0 lỗi/0 cảnh báo**, ba bộ chạy tuần tự trên phiên bản tiến độ/ảnh nhúng đạt **59/59 ca HTTP**:

- [17/17 ca tiến độ/ảnh nhúng](upload-progress-verification.json), 12:26:43–12:26:52 UTC+07, ID **HTTP:PROGRESS-00–16**.
- [16/16 ca xử lý FLAC](flac-processing-verification.json), 12:27:05–12:27:08 UTC+07, ID **HTTP:FLAC-00–15**.
- [26/26 ca tài khoản nguồn](distributor-account-verification.json), 12:27:09–12:27:12 UTC+07, ID **HTTP:DIST-00–25**.
- [9/9 ca đăng nhập demo](demo-login-verification.json), 12:28:57–12:28:58 UTC+07, ID **HTTP:LOGIN-00–08**: ô identifier là text; bốn tên ngắn và bốn email dùng mật khẩu `123456` đến đúng khu vực. Bộ này không tạo/sửa user, bài hoặc media. Tính thêm bộ độc lập này là **68/68 HTTP**; số 59 ở trên chỉ ba bộ chức năng.
- [Ảnh form 15](screenshots/15-upload-flac-tu-dong.png) thuộc bản FLAC-only trước tiến độ. Chưa dùng HTTP để khẳng định % byte/cancel/retry hoạt động trong trình duyệt hoặc âm thanh đã nghe qua loa; cần bằng chứng UI riêng.

Quan sát trình duyệt: `distrokite / 123456` đăng nhập được, form hiện trường/hướng dẫn mới; mở hồ sơ FLOWER (18) thấy bìa thật và Pending, [ảnh 17](screenshots/17-flower-cover.png). Quyền chọn tệp để kiểm thử upload qua trình duyệt bị từ chối; chưa có upload/% byte/hủy/thử lại UI và không tạo ảnh tiến độ upload. Không suy những phần này đã Pass từ số ca HTTP.

FLAC mẫu thực dài **24 giây**. MP3 sinh ra có codec MP3, stereo 44.100 Hz, bitrate **128000** và **320000 bit/s**, thời lượng 24 giây; SHA256 FLAC lưu lại bằng tệp gốc. MP3 đổi đuôi, FLAC chỉ có header và FLAC cắt giữa dữ liệu không tạo bài/tệp thừa.

Bộ tiến độ tạo bài **19/20 Pending**; FLAC tạo **21 Approved / 22 Pending**; tài khoản tạo **23 Approved / 24 Rejected**. Các bộ HTTP không sửa bài có trước. Ngoài các bộ này, ảnh nhúng của **FLOWER (18)** được bổ sung từ chính FLAC người dùng đã gửi, giữ Pending và dữ liệu bài. Bài 10/11 vẫn Pending; QA cũ giữ nguyên. Mã ca tài liệu khác mã trong JSON, cần đọc tiền tố **HTTP:** khi đối chiếu. Phần chưa thử vẫn ghi Not run/Partial.

Bộ tiến độ dùng FLAC thực 180 giây/10.917.124 byte, quan sát encoding không giảm từ 0 đến 97%, cùng các trạng thái waiting/probing/cover/completed. Bìa JPEG nhúng được lưu JPEG 320×240, 673 byte; ảnh PNG chọn riêng 1×1 thay bìa nhúng và phần trích xuất không dùng được dọn. Không coi một bìa này là đã thử mọi loại/giá trị biên ảnh.

## Chuẩn bị

- Chạy Development theo README với SQL Server; dùng khách, Listener, hai Distributor và Admin ở các phiên tách biệt.
- Tài khoản demo Development: `listener`, `distrokite`, `tunekore`, `admin`, mật khẩu `123456`. Email tương ứng tại miền soulsignal.local vẫn dùng được; xem README.
- Tệp mẫu do nhóm tạo trong Media/Audio; dùng FLAC hợp lệ làm nguồn, ảnh PNG/JPG, FLAC hỏng/giả, tệp sai định dạng và dữ liệu biên. FFmpeg/ffprobe sẵn có theo README.
- Có bài Pending/Rejected riêng cho từng nguồn và bài Approved; một ID không tồn tại.
- Chuẩn bị FLAC có bìa PNG/JPEG nhúng, FLAC không có bìa, ảnh chọn riêng khác bìa nhúng; thử hai tài khoản nguồn để kiểm tra owner của phiên tiến độ. Giữ bằng chứng riêng cho HTTP và UI trình duyệt.
- Đối chiếu [chính sách](06-quy-dinh-nen-tang.md), [ERD/quyền](02-du-lieu-va-phan-quyen.md) và [demo nguồn tải lên](07-distribution-simulation.md).

## Đặc tả ca nền tảng và phân quyền

| ID | Tiền điều kiện | Bước thực hiện | Kết quả mong đợi | Trạng thái/bằng chứng mới |
|---|---|---|---|---|
| ENV-01 | SQL Server chạy, CSDL thử sẵn sàng | Restore, chạy Development, mở trang/đối chiếu seed | Migration thành công; bốn tài khoản hoạt động và hai nguồn | Partial: ứng dụng/4 tài khoản đạt HTTP:DIST-00/02/04/05/06; chưa có lần dựng CSDL mới riêng |
| ENV-02 | Đã seed | Ghi số bản ghi, khởi động lại, đối chiếu | Không tạo bản sao tài khoản/nguồn/bài; giữ quyết định xét duyệt | Chờ chạy mới; Not run |
| AUTH-01 | Khách, email mới | Đăng ký hợp lệ, gửi thêm Role=Distributor/Admin và DistributorId | Chỉ Listener; không tự nâng quyền hoặc gắn nguồn | Chờ chạy mới; Not run |
| AUTH-02 | Khách | Thử email trùng, tên rỗng/sau trim quá ngắn, xác nhận mật khẩu sai | Validation, không tạo tài khoản sai hoặc lỗi 500 | Chờ chạy mới; Not run |
| AUTH-03 | Các phiên chưa đăng nhập | Đăng nhập Listener, hai Distributor và Admin; thử sai mật khẩu | Đúng khu vực/nguồn; sai có lỗi, không có phiên mới | Partial: 4 tài khoản đăng nhập đúng đạt HTTP:DIST-02/04/05/06; sai mật khẩu chưa chạy lại |
| AUTH-04 | Đã đăng nhập | POST đăng xuất; thử GET hoặc POST thiếu CSRF | POST hợp lệ kết thúc phiên; GET không đăng xuất; CSRF thiếu bị chặn | Chờ chạy mới; Not run |
| ROLE-01 | Khách/Listener | Gọi trực tiếp /Distributor, /Distributor/Upload và /Admin | Khách chuyển đăng nhập, Listener bị từ chối, không có nội dung riêng | Pass HTTP:DIST-01/03 |
| ROLE-02 | Distributor | Gọi /Admin, /Admin/Import, /Admin/ReviewSong và gửi quyết định | Không vào/duyệt/thay đổi dữ liệu | Partial: GET bị chặn HTTP:DIST-08; POST quyết định trái quyền chưa thử riêng |
| ROLE-03 | Nguồn A có bài Pending/Rejected | Nguồn B gọi danh sách, Details, Stream và Cover của A bằng ID | Không cấp hồ sơ/tệp riêng; danh sách của B không chứa bài A | Partial: Pending cách ly đúng HTTP:DIST-17; chưa lặp chéo sau Rejected |
| ROLE-04 | Distributor | Gửi bài kèm nguồn B, ImportedById khác, Status=Approved | Lấy nguồn/người tải từ phiên, bài luôn Pending | Partial: đúng nguồn/Pending, bỏ ghi chú giả HTTP:DIST-14; chưa đối chiếu ImportedBy bằng SQL |
| SONG-01 | Có Approved mẫu | Xem danh mục/tìm kiếm/chi tiết | Chỉ Approved; tìm đúng; không lộ quyền/mã nguồn/ghi chú | Pass với bài test: HTTP:DIST-19/22/23, gồm tìm theo tên và dữ liệu riêng |
| SONG-02 | Có Pending/Rejected | Khách/Listener gọi trực tiếp Details/Stream/Cover | Không cấp nội dung riêng | Partial: Pending Details/Stream/Cover đạt HTTP:DIST-19; Rejected stream/danh mục đạt DIST-23, cover chưa thử riêng |
| SONG-03 | Admin/đúng nguồn, có Pending | Xem hồ sơ và phát thử MP3 | Có quyền xem đúng bài và trạng thái; chỉ Admin được duyệt | Pass cấp MP3/controls và quyền GET: HTTP:DIST-08/18/20 |
| SONG-04 | ID không tồn tại | Mở Details/Stream | 404, không lộ đường dẫn/lỗi nội bộ | Chờ chạy mới; Not run |
| AUDIO-01 | Approved MP3 | Phát/tạm dừng/tua bằng controls mặc định, không JS ứng dụng | Giải mã/phát được, controls hoạt động | Partial: HTTP range/controls đạt DIST-18/20/22; chưa lưu quan sát phát/tạm dừng/tua qua trình duyệt |
| AUDIO-02 | MP3 đang phát | Chuyển trang trong cùng tab | Nhạc dừng theo phạm vi MVC | Chờ chạy mới; Not run |
| AUDIO-03 | Khách/Listener/Distributor | Gọi FlacPreview và URL tĩnh của tệp private | Không được cấp FLAC qua đường bỏ qua quyền | Partial: Listener bị chặn FlacPreview HTTP:DIST-25; chưa chạy lại toàn bộ vai trò/URL tĩnh |
| AUDIO-04 | Admin, Development | Thử FLAC, HEAD/range và giải mã trên trình duyệt demo | MIME/range đúng; ghi khả năng giải mã thực tế | Partial: MIME/range đúng HTTP:DIST-20; chưa xác nhận giải mã FLAC trên trình duyệt |
| AUDIO-05 | Ngoài Development | Gọi FlacPreview bằng phiên Admin | Không cấp endpoint thử nghiệm | Chờ chạy mới; Not run |
| DATA-01 | Có quyền CSDL thử | Đối chiếu user–source, Song.Distributor/ImportedBy/reviewer/FK/mã | Đúng nguồn/người thao tác; mã nội bộ tách mã nguồn; dữ liệu private không công khai | Partial: nguồn/mã/quyền được kiểm tra qua HTTP:DIST-14/17/22; chưa đối chiếu toàn bộ FK bằng SQL |
| PORT-01 | Máy thành viên thứ hai | Lấy source, cấu hình theo README, đăng nhập/tải/duyệt/nghe | Máy thứ hai chạy được, không suy ra từ một máy | Chưa thử; Not run |

## Ca tải lên và xét duyệt

Xem **DIST-01–11** trong [tài liệu 07](07-distribution-simulation.md): tải hợp lệ; nguồn cố định; cách ly nguồn; trái vai trò; CSRF; thiếu/giả/quá giới hạn tệp; mã trùng/dài; duyệt/công khai; từ chối/lý do; lặp quyết định; đăng ký không tự cấp quyền. Bảng 07 đã đối chiếu từng ca với bộ HTTP mới; các biến thể chưa thử vẫn ghi Partial/Not run.

Cần tách các giá trị biên: FLAC 0 byte/đúng giới hạn/vượt 1 byte; thời lượng thực 0/1/1.800/1.801; mã 64/65 ký tự; dữ liệu trắng sau trim; thiếu FLAC, thiếu xác nhận. Không còn yêu cầu nhập thời lượng/giới thiệu hoặc tải MP3. Không coi một tệp nhỏ hợp lệ là đã kiểm thử đủ giới hạn.

## Kiểm thử FLAC gốc và xử lý âm thanh mới

Các ca tài liệu FLAC-01–09 dưới đây khác mã HTTP:FLAC-00–15 trong JSON. Kết quả được đối chiếu đúng nội dung, không suy toàn bộ biến thể đạt chỉ vì bộ test có 16 ca Pass.

| ID | Tiền điều kiện và bước | Mong đợi | Thực tế/trạng thái |
|---|---|---|---|
| FLAC-01 | Mở form Distributor/Upload và Admin/Import | Chỉ FLAC là tệp nhạc đầu vào; không có Description, DurationSeconds, Mp3File | Pass HTTP:FLAC-02; cả hai form bỏ ba trường nhập tay, không JavaScript |
| FLAC-02 | Upload FLAC mẫu hợp lệ, metadata/quyền đầy đủ | Tự tính đúng thời lượng, sinh MP3 128 và 320 kbps, giữ FLAC, tạo Pending | Pass HTTP:FLAC-03/07/09–12; hai đường nhập tạo Pending, tự đọc 24 giây và sinh bản nhạc |
| FLAC-03 | Gửi thêm Description, DurationSeconds sai, Mp3File và trạng thái/nguồn giả | Không tin dữ liệu tính toán từ client; thời lượng lấy từ FLAC, giới thiệu mới trống, đúng nguồn và Pending | Partial: HTTP:FLAC-08 và HTTP:DIST-14 xác nhận bỏ thời lượng/giới thiệu/tên tệp/trạng thái/nguồn giả; ImportedBy cần đối chiếu SQL riêng |
| FLAC-04 | ffprobe các tệp sinh và so nguồn | Codec MP3, bitrate lần lượt 128000/320000 bit/s, thời lượng tương đương nguồn có dung sai mã hóa; FLAC gốc không đổi | Pass HTTP:FLAC-09–12; 128000/320000 bit/s, stereo 44.100 Hz, 24 giây, SHA256 FLAC giống nguồn |
| FLAC-05 | Đổi đuôi MP3, chỉ có header FLAC, FLAC hỏng hoặc cắt dữ liệu | Không tạo bài; báo lỗi phù hợp; không lưu tệp thừa | Pass HTTP:FLAC-04–06; không tăng bài hoặc để lại FLAC/MP3/ảnh mới |
| FLAC-06 | Thiếu FLAC, quá 50 MB, thời lượng ngoài giới hạn | Bị từ chối, không tạo bài/tệp thừa | Partial: thiếu FLAC đạt HTTP:DIST-10; quá 50 MB và thời lượng giới hạn chưa thử |
| FLAC-07 | Tạm cấu hình sai đường dẫn FFmpeg/ffprobe, hoặc ép timeout trong môi trường thử | Báo lỗi xử lý; không ghi bài; dọn các tệp vừa tạo; không ảnh hưởng tệp cũ | Not run: chưa thử thiếu encoder/ffprobe hoặc ép timeout |
| FLAC-08 | Khách/Listener/nguồn khác gọi tệp Pending và preview 320/FLAC | Giữ quyền nguồn như cũ; preview 320/FLAC chỉ admin Development | Pass trong các vai trò đã thử: HTTP:FLAC-13/15, HTTP:DIST-17–20/25; chưa thử ngoài Development |
| FLAC-09 | Admin nghe thử rồi duyệt hoặc từ chối | Người nghe chỉ nghe MP3 thường khi Approved; không tự cấp VIP/320/FLAC | Pass cấp tệp/trạng thái: HTTP:FLAC-15 và HTTP:DIST-21–24; chưa nghe qua loa |

Lỗi lưu DB và hủy yêu cầu chưa được thử trong hai bộ này; cần ca riêng để xác nhận dọn đúng FLAC/MP3/ảnh vừa tạo. Thử tệp nhỏ không thay cho thử timeout/dung lượng/giá trị biên. Đo bitrate bằng công cụ không thay cho nghe qua loa.

## Phần còn dự kiến

| ID | Tiền điều kiện/bước | Mong đợi | Thực tế/trạng thái |
|---|---|---|---|
| VIP-01 | VIP còn hạn, bài được phép tải; thử FLAC/tải | Đúng người/gói/thời hạn được cấp tệp | Chưa triển khai; Planned |
| VIP-02 | Không VIP/chưa bắt đầu/hết hạn; gọi URL trực tiếp | Không cấp FLAC/tải, vẫn nghe MP3 Approved | Chưa triển khai; Planned |
| PAY-01 | Giao dịch mô phỏng thất bại/thành công/gửi lại | Chỉ thành công cấp một lần, không thu tiền thật | Chưa triển khai; Planned |
| COPY-01 | Phiếu phản ánh mô phỏng thiếu/đủ thông tin | Yêu cầu bổ sung/ghi nhận, không công khai dữ liệu riêng | Quy trình thủ công dự kiến; Planned |
| COPY-02 | Phản ánh và phản hồi mô phỏng | Xem xét/giải thích/khôi phục hoặc giữ ẩn theo căn cứ | Chưa có hệ thống khiếu nại; Planned |

Mỗi lỗi ghi ID, môi trường, vai trò, bước tái hiện, mong đợi, thực tế và bằng chứng. Không ghi mật khẩu/chuỗi kết nối nhạy cảm vào ảnh kiểm thử. Không đánh dấu Pass chỉ vì có schema, chính sách hoặc mã xử lý.
