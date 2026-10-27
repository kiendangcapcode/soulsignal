# Chính sách nền tảng — bản dự thảo cho đồ án

Tài liệu giúp nhóm/thầy thống nhất cách Soul Signal nhận, phát và xử lý nội dung. Mô hình hiện tại có **Listener/Distributor/Admin**. Nhà phân phối được cấp tài khoản gắn với nguồn để tải nhạc lên; admin xét duyệt. Đăng ký công khai chỉ tạo Listener. [Phạm vi kỹ thuật](01-pham-vi.md) và [luồng nhà phân phối–admin](07-distribution-simulation.md) được mô tả riêng.

Trang /Home/Policy trình bày bản tóm tắt. Có trang chính sách không đồng nghĩa mọi quy trình phản ánh/VIP đã được tự động hóa. Bản này cần đối chiếu yêu cầu thầy; không tuyên bố là bộ quy định pháp lý hoàn chỉnh.

## BR-01 — Tài khoản và trách nhiệm thao tác

Người đăng ký mới là Listener. Distributor được cấp sẵn, tải nhạc và xem hồ sơ của nguồn gắn với tài khoản; chỉ Admin xét duyệt hoặc nhập thủ công. Không cấp quyền theo tên nghệ sĩ, tên nhà phân phối hoặc trường role người gửi tự khai. Người dùng không mạo danh; admin ghi nhận nguồn và căn cứ quyết định, không coi quyền quản trị là quyền sở hữu âm nhạc.

## BR-02 — Phạm vi quyền đối với nhạc

Một bài có thể liên quan quyền đối với **tác phẩm/lời, phần biểu diễn và bản ghi âm**. Cần rà quyền tương ứng, không chỉ xem ai có tệp. [Tham khảo WIPO](https://www.wipo.int/en/web/music/).

Chỉ nhận nội dung do bên cung cấp sở hữu hoặc có sự cho phép phù hợp cho cách Soul Signal sử dụng: lưu trữ tệp/ảnh, tạo các bản MP3 từ FLAC để phát trực tuyến, cung cấp FLAC và cho tải khi gói VIP được triển khai. Quyền cho phát trực tuyến không tự được hiểu là quyền cho tải. Để giữ MVP đơn giản, gói được nhập cần xác nhận đủ phạm vi dự kiến; chưa có cơ chế quyền tải riêng từng bài.

Mua/tải miễn phí một bài không tự cho phép đưa nó lên nền tảng. [Hướng dẫn SoundCloud](https://help.soundcloud.com/hc/en-us/articles/115003446067-Best-practices-for-uploading-a-bought-or-free-track). Ghi công/tên nghệ sĩ cũng không thay thế sự cho phép cần thiết. [Giải thích của YouTube](https://support.google.com/youtube/answer/2797449?hl=en).

## BR-03 — Nguồn gói và thông tin quyền

Nhà phân phối tải nhạc với nguồn cố định từ tài khoản, khai đúng nghệ sĩ/chủ thể quyền và căn cứ sử dụng, không giả nhận tác giả/chủ bản ghi. Admin có thể nhập thủ công và chọn nguồn khi cần. RightsHolder/RightsEvidence là nội dung khai báo bằng văn bản để xem xét; có thể nêu bằng chứng/phạm vi/thời hạn liên quan. Đây chưa phải hệ thống thẩm định giấy phép hoặc upload hồ sơ pháp lý riêng.

Chỉ admin và tài khoản đúng nguồn sở hữu thấy thông tin quyền/mã nguồn/ghi chú xét duyệt riêng. Nhà phân phối không xem hồ sơ hoặc tệp bài chưa công khai của nguồn khác; server không tin nguồn/người nhập/trạng thái do client tự gửi.

## BR-04 — Xét duyệt và công khai

Bài tải lên hoặc nhập mới luôn là Pending. Admin xem metadata, mã nội bộ/mã nguồn, tệp và thông tin quyền trước khi duyệt hoặc từ chối. Chỉ Approved xuất hiện công khai/nghe MP3; Pending/Rejected chỉ admin và nguồn sở hữu kiểm tra. Lưu người/thời điểm/ghi chú; từ chối có lý do.

**Duyệt không phải chứng nhận sở hữu.** Ẩn/gỡ một bài đã công khai để xử lý phản ánh là quy trình dự kiến riêng, không gộp với thao tác xét Pending đã có.

## BR-05 — Nội dung không chấp nhận

Không nhận nội dung không có quyền sử dụng, mạo danh, quấy rối/đe dọa, xâm phạm riêng tư, thông tin đánh lừa hoặc tệp độc hại. Áp dụng cho âm thanh, ảnh, tên bài, thông tin nghệ sĩ/quyền và mô tả lịch sử nếu có. Việc xem xét là công việc vận hành; chưa hứa tự phát hiện bản quyền hay nội dung vi phạm bằng máy.

## BR-06 — Nghe thường và VIP

Nghe thường dùng MP3 128 kbps của Approved trong luồng tải FLAC mới. Hệ thống tạo thêm MP3 320 kbps từ cùng bản gốc để chuẩn bị cho chất lượng cao; tạo tệp không đồng nghĩa đã hoàn thành quyền VIP. Chức năng tải chính thức và FLAC cho người nghe dành cho VIP còn hiệu lực khi giai đoạn đó hoàn thành. VIP là quyền dùng dịch vụ theo thời hạn, không chuyển sở hữu tác phẩm/bản ghi hay cho phép phát hành lại.

Đề xuất điều kiện thời gian StartsAt ≤ thời điểm server kiểm tra < EndsAt. Thanh toán đồ án là mô phỏng, không thu tiền/thẻ; chỉ giao dịch thành công cấp/gia hạn đúng một lần. Quy tắc gia hạn/giá cần chốt trước triển khai.

FLAC/MP3 320 kbps preview Development chỉ cho admin kiểm tra kỹ thuật; không phải VIP đã hoàn thành. Server kiểm tra quyền cấp tệp; chính sách không tuyên bố chặn tuyệt đối mọi cách sao chép âm thanh đã phát.

## BR-07 — Phản ánh về quyền

Hồ sơ đề xuất gồm người/chủ thể phản ánh và liên hệ, tác phẩm/bản ghi/quyền họ đại diện, URL/ID bài, mô tả căn cứ và bằng chứng. Không yêu cầu số giấy tờ định danh hay công khai hồ sơ.

Kênh tiếp nhận: **[nhóm chốt kênh hoặc phiếu mô phỏng trước báo cáo]**. Chưa có địa chỉ email hay biểu mẫu vận hành thật được công bố. Có thể thử bằng phiếu thủ công, chưa cần xây thêm hệ thống khiếu nại.

## BR-08 — Xem xét và phản hồi

Quy trình do nhóm đề xuất, không gán là thủ tục pháp lý nước ngoài:

1. Ghi nhận, xác định bài; yêu cầu bổ sung nếu chưa đủ thông tin.
2. Tạm ẩn bài cần làm rõ, ghi lý do và liên hệ bên cung cấp theo kênh ngoài hệ thống đã thống nhất.
3. Nhận phản hồi/căn cứ; chỉ chia sẻ thông tin cần thiết, không công khai hồ sơ/liên hệ riêng.
4. Khôi phục khi quyền sử dụng được làm rõ; giữ ẩn hoặc gỡ nếu không đáp ứng, ghi căn cứ quyết định.
5. Tiếp nhận đề nghị xem xét lại có thông tin bổ sung.

Chưa ấn định thời hạn pháp lý hay tự khôi phục sau một số ngày. Luồng phản ánh/ẩn/gỡ này chưa được coi là chức năng đã hoàn thành.

## BR-09 — Nguồn hoặc hành vi vi phạm lặp lại

Có thể nhắc nhở, yêu cầu sửa/bổ sung và ngừng tiếp nhận gói từ nguồn nhiều lần không đáp ứng chính sách, theo mức độ/căn cứ và có cách phản hồi. Đây là biện pháp vận hành dự kiến; chưa có giao diện đình chỉ tài khoản nguồn, bộ đếm vi phạm hoặc tự động khóa do vi phạm đang hoạt động.

## BR-10 — Dữ liệu và riêng tư

Chỉ thu dữ liệu cần cho tài khoản/gói, metadata và căn cứ xử lý. Identity quản lý mật khẩu băm, không lưu bản rõ để admin đọc. Email, thông tin quyền, mã nguồn và hồ sơ phản ánh không mặc định công khai cho người nghe.

Âm thanh/ảnh upload ở vùng riêng, cấp qua endpoint kiểm tra trạng thái/quyền. Không đưa URL FLAC tĩnh để bỏ qua quyền. Phiên tiến độ tạm chỉ cho user sở hữu đọc, không công khai thông tin lượt tải và không thay thế quyền truy cập bài. Không thu thẻ/ngân hàng cho thanh toán mô phỏng. Nếu thành dịch vụ công khai cần chốt thêm thời hạn lưu và quy trình yêu cầu xóa dữ liệu.

## BR-11 — Dữ liệu mẫu, nhãn nguồn và mã

Nhạc/ảnh demo do nhóm tạo, có [nguồn và cách tái tạo](media-assets.md); không phải giấy phép cho nhạc của người khác.

DistroKite Demo/DKT-DEMO và TuneKore Demo/TKR-DEMO là tên giả. Không tuyên bố liên kết hãng thật, không tích hợp phát hành bên ngoài. SS-TRACK-000001 là mã nội bộ. ExternalRecordingCode/ExternalReleaseCode là mã nguồn mô phỏng tùy chọn, không phải ISRC/UPC được cấp/xác minh thật. Không trả royalty/chia doanh thu.

## BR-12 — Tệp nhập và dữ liệu biên

| Dữ liệu | Quy định |
|---|---|
| FLAC gốc | Bắt buộc, có nội dung, ≤ 50 MB; phải giải mã được, không đổi đuôi MP3 để giả FLAC |
| MP3 128/320 kbps | Server sinh từ FLAC gốc; không yêu cầu người gửi tự chuẩn bị |
| Ảnh | Ảnh PNG/JPG chọn riêng ≤ 2 MB được ưu tiên; nếu không có, dùng PNG/JPEG nhúng hợp lệ trong FLAC (ưu tiên bìa trước), chuyển JPEG tối đa 1.600 px mỗi chiều; cuối cùng dùng ảnh mặc định |
| Thời lượng | Server đọc từ tệp, lớn hơn 0 và ≤ 1.800 giây; không nhập thủ công |
| Nội dung | Tên bài, nghệ sĩ, thể loại hợp lệ, chủ thể/căn cứ quyền và xác nhận; không yêu cầu giới thiệu bài |
| Hai mã nguồn | Tùy chọn, ≤ 64 ký tự mỗi trường, ghi rõ mô phỏng |

1 MB trong giới hạn mã là 1.024 × 1.024 byte. Server kiểm tra đầu vào, codec/thời lượng và giải mã/chuyển FLAC thành MP3 128/320 kbps; sinh tên tệp riêng. Thiếu bộ xử lý, timeout hoặc lỗi giải mã không tạo bài và cần dọn tệp vừa tạo. Kiểm tra tệp/duyệt không tự xác minh quyền sở hữu.

Ảnh nhúng và ảnh chọn riêng đều thuộc nội dung người gửi cần có quyền sử dụng. Việc trích xuất, đổi kích thước và định dạng ảnh không làm thay đổi yêu cầu về quyền. Phần tiến độ chỉ phản ánh vận hành: tải đạt 100% chưa được coi là bài đã lưu hoặc được admin duyệt. Không thay đổi yêu cầu khai báo quyền hiện tại chỉ từ việc giải thích ý nghĩa các trường.

## Tình trạng áp dụng và test

Tài khoản, nhận mẫu/nhập gói bằng admin và xét Pending có bằng chứng lịch sử **44 HTTP + 24 ca gói + 7 quan sát Edge** của bản hai vai trò trước đây; không dùng các kết quả đó để khẳng định luồng Distributor mới đã đạt. Xem [trạng thái nền tảng](04-demo-va-kiem-thu.md) và [DIST-01–11](07-distribution-simulation.md); các giá trị biên chưa thử đầy đủ vẫn ghi Partial/Not run. VIP/thanh toán, royalty và quy trình phản ánh bản quyền còn Planned; có chính sách không đồng nghĩa tính năng đã hoàn thành.
