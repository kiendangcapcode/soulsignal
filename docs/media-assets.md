# Nhạc và ảnh mẫu của Soul Signal

Các bản thu dưới đây là nhạc tổng hợp được tạo riêng cho bản demo Soul Signal bằng bộ tạo sóng, hợp âm, giai điệu và nhịp điệu có thể tái tạo trong `scripts/generate-demo-audio.py`. Không sử dụng bản thu, giọng hát hoặc sample nhạc từ bên thứ ba. Tên nghệ sĩ dùng trong demo: **Soul Signal Studio**.

| Bài hát | Thời lượng nội dung | Tệp nghe thường | Tệp FLAC thử nghiệm | Ảnh bìa |
|---|---:|---|---|---|
| Saigon Dawn | 24 giây | `Media/Audio/saigon-dawn.mp3` | `Media/Audio/saigon-dawn.flac` | `wwwroot/images/covers/saigon-dawn.svg` |
| Afterglow | 28 giây | `Media/Audio/afterglow.mp3` | Chưa có trong tuần 1 | `wwwroot/images/covers/afterglow.svg` |
| Quiet Orbit | 32 giây | `Media/Audio/quiet-orbit.mp3` | Chưa có trong tuần 1 | `wwwroot/images/covers/quiet-orbit.svg` |

- **MP3:** stereo, 44.100 Hz, 128 kbit/s. Thời lượng container có thể dài hơn nội dung vài phần trăm giây do khung mã hóa MP3.
- **FLAC:** stereo, 44.100 Hz, nguồn PCM 16-bit; mã hóa trực tiếp từ cùng nguồn WAV tổng hợp. Không đổi đuôi hoặc chuyển ngược từ MP3 thành FLAC.
- **Ảnh bìa:** SVG tự thiết kế bằng hình học và gradient; không tải ảnh bên ngoài. Văn bản dùng font hệ thống dự phòng, không có phụ thuộc font mạng.
- Âm thanh nằm ngoài `wwwroot` để ứng dụng chủ động kiểm tra quyền trước khi cấp tệp. Chỉ ảnh bìa được phục vụ như tài nguyên công khai.
- Ở tuần 1, FLAC phục vụ kiểm tra kỹ thuật trên trình duyệt demo. Luồng mua VIP và quyền truy cập FLAC hoàn chỉnh thuộc mốc tuần 3.

## Tái tạo tệp âm thanh

Các tệp thành phẩm đã có trong dự án; **chạy website không cần Python**. Luồng upload FLAC mới cần FFmpeg/ffprobe để đọc thời lượng và tạo MP3 128/320 kbps. Chỉ khi muốn tạo lại nguồn nhạc mẫu bằng script mới cần Python 3, thư viện `numpy` và `ffmpeg` có trên PATH. Chạy từ thư mục dự án:

```powershell
python scripts/generate-demo-audio.py
```

Script sử dụng seed cố định, tạo nguồn PCM trong tệp WAV tạm, mã hóa ra MP3/FLAC rồi xóa đúng tệp WAV tạm vừa tạo. Chạy lại sẽ ghi đè bốn tệp âm thanh demo được liệt kê ở trên.

## Kiểm tra tệp đã thực hiện

- `ffprobe` nhận đúng ba MP3 stereo 44.100 Hz / 128 kbit/s, có thời lượng lần lượt 24, 28, 32 giây; FLAC stereo 44.100 Hz / 16-bit có thời lượng 24 giây.
- Cả ba MP3 giải mã bằng ffmpeg không có lỗi.
- Giải mã FLAC về PCM rồi so sánh với nguồn tạo bằng script: toàn bộ **1.058.400 mẫu stereo** khớp chính xác.
- Bản MP3 Saigon Dawn có mức đỉnh -2,1 dBFS qua phép đo `volumedetect`, không bị clipping trong kiểm tra này.
- Ba ảnh SVG đọc được dưới dạng XML hợp lệ. Kiểm tra phát âm thanh và bố cục trực tiếp trên trình duyệt được thực hiện trong bước tích hợp của ứng dụng.
