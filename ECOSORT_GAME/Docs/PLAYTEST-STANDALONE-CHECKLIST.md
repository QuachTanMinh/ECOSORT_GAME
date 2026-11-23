# PLAYTEST STANDALONE CHECKLIST — EcoSort Quest Windows Build

> Checklist này dành cho bản **Windows standalone** (`Builds\Windows\`), **không** chạy trong Unity Editor.  
> Mọi mục cần được kiểm tra trực tiếp trên máy nhận; build thành công **không** thay thế playtest thủ công.

---

## Phần 1 — Chuẩn bị môi trường

### 1.1 Yêu cầu máy

| Yêu cầu | Chi tiết |
|---|---|
| Hệ điều hành | Windows 10 (64-bit) hoặc Windows 11 |
| Unity | **Không cần** — bản standalone tự đủ |
| RAM tối thiểu | 4 GB (khuyến nghị 8 GB) |
| GPU | Hỗ trợ DirectX 11 trở lên |
| Dung lượng | ≥ 300 MB trống cho build + data |
| Quyền | Người dùng thường (không cần Administrator; trừ khi webcam bị chặn bởi policy) |

### 1.2 Thư mục build

```
Builds\Windows\
  EcoSort Quest.exe          ← file khởi chạy chính
  EcoSort Quest_Data\
  UnityCrashHandler64.exe
  UnityPlayer.dll
```

> [!IMPORTANT]
> Chạy `EcoSort Quest.exe` **trực tiếp** từ thư mục `Builds\Windows\`. Không di chuyển `.exe` ra ngoài thư mục mà không mang theo toàn bộ `_Data\`.

### 1.3 Chế độ backend

Bản standalone mặc định **ngoại tuyến**. Vào **Kết nối backend** trong game để chọn:

| Chế độ | Điều kiện | Checklist áp dụng |
|---|---|---|
| **Offline** | Không cần mạng, không cần FastAPI | Tất cả, trừ M17 |
| **Online** | FastAPI đang chạy ở URL cấu hình | Tất cả bao gồm M17 và M24 |

> [!NOTE]
> PlayerPrefs (progress, lựa chọn kết nối, điểm cao) được lưu riêng trên máy người chơi. Reinstall sẽ xóa PlayerPrefs — đây là hành vi đã biết và dự kiến.

### 1.4 Danh sách webcam cần kiểm tra (M12)

Đánh dấu các thiết bị có sẵn trước khi chạy:

- [ ] Webcam tích hợp (laptop built-in)
- [ ] Webcam USB ngoài (loại 1)
- [ ] Webcam USB ngoài (loại 2) — nếu có
- [ ] Không có webcam (kiểm tra M12 trả thông báo lỗi phù hợp)

Kiểm tra quyền camera trước: **Windows Settings → Privacy → Camera → cho phép app desktop**.

---

## Phần 2 — Checklist M01–M24 (Mở rộng)

### Hướng dẫn ghi kết quả

- **Pass**: hành vi đúng như kỳ vọng, không có lỗi/crash.
- **Fail**: hành vi sai, crash, hoặc lỗi console không mong đợi.
- **N/A**: không thể kiểm tra (thiếu phần cứng, thiếu backend, v.v.) — ghi rõ lý do.
- Chụp ảnh màn hình khi Fail và lưu vào `Docs/Evidence/`.

---

| ID | Thao tác | Kỳ vọng | Ngày | Máy / GPU | Độ phân giải | Kết quả | Ghi chú |
|---|---|---|---|---|---|---|---|
| **M01** | Mở `EcoSort Quest.exe` → Play Level 1 | Scene 3D tải đủ, HUD hiển thị, ba thùng có nhãn Nhựa / Kim loại / Giấy | | | | ☐ Pass ☐ Fail ☐ N/A | |
| **M02** | Chọn đúng (score = 0 → score sau) và chọn sai | +10 khi đúng, −5 khi sai, điểm **không bao giờ âm** | | | | ☐ Pass ☐ Fail ☐ N/A | |
| **M03** | Click thùng **trước** khi chọn vật phẩm | Hiển thị hướng dẫn chọn vật phẩm trước; không tính điểm | | | | ☐ Pass ☐ Fail ☐ N/A | |
| **M04** | Click liên tiếp nhanh khi vật phẩm đang chuyển | Chỉ tính điểm **một lần** duy nhất | | | | ☐ Pass ☐ Fail ☐ N/A | |
| **M05** | Hoàn thành level với 6/10 đúng; thử lại với 7/10 đúng | 6/10 → không qua; 7/10 → qua và mở level tiếp | | | | ☐ Pass ☐ Fail ☐ N/A | |
| **M06** | Nhấn ESC (Pause) → chờ ≥ 5 giây → ESC lần nữa (tiếp tục) | Timer **dừng** khi pause, **tiếp tục** đúng chỗ; không crash | | | | ☐ Pass ☐ Fail ☐ N/A | |
| **M07** | Để hết giờ ở Level 2 hoặc Level 3 | Hiển thị màn kết quả; câu chưa trả lời **không** được tính | | | | ☐ Pass ☐ Fail ☐ N/A | |
| **M08** | Chơi lại hoặc về menu từ màn Pause | Lượt mới bắt đầu sạch; `timeScale` trở về 1 | | | | ☐ Pass ☐ Fail ☐ N/A | |
| **M09** | Thoát game → mở lại | Điểm cao và level đã mở **vẫn còn** (PlayerPrefs) | | | | ☐ Pass ☐ Fail ☐ N/A | |
| **M10** | Thay đổi kích thước cửa sổ: 1280×720, 1920×1080, 1024×768 | Nội dung đọc được, nút không bị chồng; 16:9 là mục tiêu chính | | | | ☐ Pass ☐ Fail ☐ N/A | |
| **M11** | Scan mock từng loại Plastic / Metal / Paper | Kết quả khớp nhãn fixture; có cảnh báo "[MOCK]" rõ ràng | | | | ☐ Pass ☐ Fail ☐ N/A | |
| **M12** | Mở webcam → chụp ảnh → chuyển scene | Có preview webcam, chụp được; camera được **giải phóng** sau khi chuyển scene | | | | ☐ Pass ☐ Fail ☐ N/A | Cần webcam thực |
| **M13** | Nạp JPG/PNG hợp lệ; thử đường dẫn sai; thử file không phải ảnh | Hiển thị ảnh hoặc thông báo lỗi rõ ràng; **không crash** | | | | ☐ Pass ☐ Fail ☐ N/A | |
| **M14** | AR simulation: click sàn, chọn sai rồi chọn đúng | Sai không cộng điểm; đúng hoàn thành +20 **một lần** trong nhiệm vụ | | | | ☐ Pass ☐ Fail ☐ N/A | |
| **M15** | Progress mock upload / download | Đọc lại đúng tiến trình; ghi rõ "[MOCK]" / "[LOCAL]" | | | | ☐ Pass ☐ Fail ☐ N/A | |
| **M16** | Nhập REST URL sai hoặc tắt server | Có thông báo timeout / lỗi; progress cục bộ **còn nguyên** | | | | ☐ Pass ☐ Fail ☐ N/A | |
| **M17** | Backend thật: `health` / `predict` / `score` / `progress` / `ar-results` | Chạy đúng contract API; kiểm tra dữ liệu phía server | | | | ☐ Pass ☐ Fail ☐ N/A | Cần FastAPI + Docker |
| **M18** | Android AR và cảm ứng | Chưa nằm trong phạm vi PC; để N/A nếu không có thiết bị | | | | ☐ N/A | |
| **M19** | Chạy `"EcoSort Quest.exe" -ecosort-benchmark` | File JSON kết quả được tạo ra; `median` < 20 ms ở 1280×720 | | | | ☐ Pass ☐ Fail ☐ N/A | Xem tham số `-ecosort-benchmark-output` |
| **M20** | Vào toàn màn hình (Alt+Enter) → nhấn ESC → trở về cửa sổ | Chuyển đổi suôn sẻ; **không crash**; game tiếp tục đúng | | | | ☐ Pass ☐ Fail ☐ N/A | |
| **M21** | Kéo cửa sổ từ 1600×900 → 1920×1080 trong khi game đang chạy | UI tự điều chỉnh; **không bị vỡ layout**, không mất nút | | | | ☐ Pass ☐ Fail ☐ N/A | |
| **M22** | Trigger đủ 8 nhóm AI nhận diện trong một phiên | Panel hướng dẫn 8-nhóm hiển thị đúng; **không crash** | | | | ☐ Pass ☐ Fail ☐ N/A | Cần backend 12-class |
| **M23** | Settings → Timer → chọn Ngắn / Mặc định / Dài lần lượt | Thời gian mỗi level thay đổi đúng theo lựa chọn | | | | ☐ Pass ☐ Fail ☐ N/A | |
| **M24** | Chơi xong khi **không có backend** → kết nối lại backend → kiểm tra score | Score được đưa vào OfflineQueue; sau khi kết nối, score được gửi thành công | | | | ☐ Pass ☐ Fail ☐ N/A | Cần FastAPI |

---

## Phần 3 — Hướng dẫn ghi kết quả

Điền đầy đủ trước khi bắt đầu mỗi phiên kiểm tra:

```
╔══════════════════════════════════════════════════════╗
║           PLAYTEST SESSION LOG                       ║
╠══════════════════════════════════════════════════════╣
║ Ngày kiểm tra  : ___/___/______                      ║
║ Người kiểm tra : ___________________________         ║
║ Máy            : ___________________________         ║
║ GPU            : ___________________________         ║
║ RAM            : _______ GB                          ║
║ Build version  : Builds/Windows/ (ngày build: ______ )║
║ Backend        : ☐ Offline  ☐ Online (URL: _________ )║
║ Độ phân giải   : ___________________________         ║
║ Webcam         : ☐ Có (tên: ____________) ☐ Không    ║
╚══════════════════════════════════════════════════════╝
```

### Quy trình sau mỗi phiên

1. Chụp ảnh màn hình các mục **Fail** → lưu vào `Docs/Evidence/<ngày>/`.
2. Ghi số lượng: `Pass ___  /  Fail ___  /  N/A ___  / Tổng 24`.
3. Với mỗi **Fail**, mở issue hoặc ghi vào `Docs/BUG-LOG.md` với định dạng: `[ID] [Mô tả ngắn] [Bước tái hiện] [Ảnh đính kèm]`.
4. Ký tên / ghi nhận người kiểm tra ở cuối file log.

---

## Phần 4 — Danh sách lỗi phổ biến cần chú ý

| Triệu chứng | Nguyên nhân đã biết | Cách xử lý |
|---|---|---|
| Webcam không hiện preview | Quyền camera bị tắt trong Windows Settings | **Windows Settings → Privacy → Camera** → bật cho "Desktop apps" |
| Toàn màn hình bị giới hạn 30 FPS | Focus / background throttle của Windows | Nhấp vào cửa sổ game để lấy lại focus; kiểm tra Power Plan không ở "Battery saver" |
| PlayerPrefs bị xóa sau khi reinstall | Hành vi chuẩn của Unity PlayerPrefs trên Windows | Dự kiến; nhắc người dùng **không** uninstall nếu muốn giữ tiến trình |
| Score không gửi khi backend offline | Hàng đợi OfflineQueue chưa có cơ chế tự gửi lại bền vững | Kết nối lại backend rồi dùng tính năng "Gửi lại" nếu có; kiểm tra M24 |
| Crash khi mở rộng cửa sổ nhanh | Hiếm gặp; thường do GPU driver cũ | Cập nhật driver; nếu lặp lại thì ghi bug với GPU model |
| AI nhận diện sai nhóm | Bình thường; model có giới hạn | Nhắc người dùng chọn đáp án của **mình** trước khi xem kết quả AI |
| File JSON benchmark không tạo ra | Thiếu quyền ghi vào thư mục hiện tại | Chạy từ thư mục `Builds\Windows\`; hoặc dùng `-ecosort-benchmark-output "C:\path\result.json"` |

> [!WARNING]
> Không đánh dấu M12/M17/M18/M22 là **Pass** khi chưa có phần cứng/backend tương ứng. Ghi **N/A** và lý do.

> [!CAUTION]
> Không dùng kết quả playtest mock/Editor để thay thế checklist này. Bản standalone có thể có hành vi khác do thiếu Unity runtime debug tools.

---

*Cập nhật lần cuối: 23/11/2026 — dựa trên TESTING.md (M01–M18) và DEMO-VALIDATION-20261030.md*
