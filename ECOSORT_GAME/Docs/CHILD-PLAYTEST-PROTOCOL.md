# Giao thức thử nghiệm trẻ em — EcoSort Quest

> **Tính năng "Trước và sau"** (Before & After Learning)  
> Mục tiêu: Đo khả năng nhận biết phân loại rác **trước và sau** khi trẻ chơi EcoSort Quest.

---

## Mục tiêu nghiên cứu

| Mục | Chi tiết |
|---|---|
| **Câu hỏi nghiên cứu** | Chơi EcoSort Quest có cải thiện khả năng phân loại rác của trẻ không? |
| **Nhóm tuổi mục tiêu** | 8–12 tuổi |
| **Thời gian mỗi phiên** | 45–60 phút |
| **Môi trường** | Phòng học, gia đình, hoặc không gian yên tĩnh có máy tính |
| **Người giám sát** | Phụ huynh hoặc giáo viên (bắt buộc có mặt suốt buổi) |

---

## Chuẩn bị trước phiên

### Thiết bị và phần mềm

- [ ] Máy tính Windows 10/11 cài sẵn **EcoSort Quest standalone** (`Builds\Windows\`)
- [ ] Chế độ **Offline** (không cần mạng); kiểm tra game chạy được trước khi trẻ đến
- [ ] Màn hình đủ sáng, âm thanh hoạt động (tai nghe hoặc loa nhỏ)
- [ ] Đóng toàn bộ ứng dụng khác để tránh gián đoạn

### Tài liệu in sẵn

- [ ] **Phiếu đồng ý của phụ huynh** (ký trước khi bắt đầu)
- [ ] **Bảng ghi kết quả quan sát** (Observer Sheet — xem Phần 5)
- [ ] **Thẻ mã người tham gia** (P001, P002, … — không dùng họ tên thật)
- [ ] Bút và đồng hồ bấm giờ (hoặc điện thoại dùng làm đồng hồ)

### Mã người tham gia

Phân công mã tuần tự trước buổi:

```
P001, P002, P003, ...
```

Không ghi tên thật vào bất kỳ tài liệu nào. Bảng đối chiếu mã–tên (nếu cần liên lạc sau) phải được lưu riêng, **offline**, và không đưa vào file nghiên cứu.

---

## Lịch trình một phiên (45–60 phút)

| Thời gian | Hoạt động | Người thực hiện |
|---|---|---|
| 0–5 phút | Giới thiệu: giải thích mục đích (không nhắc đến "bài kiểm tra"), hướng dẫn thao tác chuột cơ bản | Người giám sát |
| 5–15 phút | **Bài kiểm tra "Trước"**: 10 câu phân loại rác trong 75 giây, cùng bối cảnh đã chọn | Trẻ tự làm |
| 15–35 phút | **Chơi tự do EcoSort Quest**: Campaign Level 1–2, có thể khám phá Học tập | Trẻ tự chơi |
| 35–45 phút | **Bài kiểm tra "Sau"**: 10 câu, **cùng bối cảnh** với bài "Trước" | Trẻ tự làm |
| 45–50 phút | **Xuất CSV** (`Kết quả học tập → Trước và sau → Xuất bảng kết quả`) → ghi quan sát | Người giám sát |
| 50–60 phút | (Tùy chọn) Trả lời câu hỏi mở, cảm ơn trẻ | Người giám sát |

> [!IMPORTANT]
> Chọn **một bối cảnh duy nhất** (Nhà ở / Trường học / Công viên) **trước khi** bài "Trước" bắt đầu và **giữ nguyên** cho bài "Sau". Bối cảnh khác nhau sẽ làm mất tính so sánh.

> [!NOTE]
> Trong 20 phút chơi tự do, người giám sát **không** gợi ý đáp án, chỉ hỗ trợ kỹ thuật (chuột bị lag, game không phản hồi, v.v.).

---

## Quy trình ghi lượt "Trước" và "Sau" trong game

1. Mở **Học tập → Kết quả học tập → Trước và sau**.
2. Chọn bối cảnh đã định (Nhà ở / Trường học / Công viên).
3. Làm bài kiểm tra 10 câu (75 giây) → nhấn **Ghi lượt đầu**.
4. Trẻ chơi Campaign Level 1–2.
5. Làm lại bài kiểm tra (cùng bối cảnh) → nhấn **Ghi lượt sau**.
6. Nhấn **Xuất bảng kết quả** → CSV được lưu tại `Exports\` trong thư mục dữ liệu game.

> [!WARNING]
> Lượt "Sau" phải có **mã khác** và **thời điểm muộn hơn** lượt "Trước". Nếu dùng chế độ Học có gợi ý thay vì Kiểm tra, kết quả **không được dùng** làm dữ liệu nghiên cứu.

---

## Bảng ghi quan sát (Observer Sheet)

In phiếu này, điền tay trong phiên:

```
╔══════════════════════════════════════════════════════════════╗
║              OBSERVER SHEET — EcoSort Quest                  ║
╠══════════════════════════════════════════════════════════════╣
║ Mã người tham gia : _______                                  ║
║ Ngày              : ___/___/______                           ║
║ Người giám sát    : ___________________________              ║
║ Bối cảnh được chọn: ☐ Nhà ở  ☐ Trường học  ☐ Công viên     ║
╠══════════════════════════════════════════════════════════════╣
║ KẾT QUẢ BÀI KIỂM TRA                                        ║
║ Điểm Trước  : _____ / 10  | TG trung bình/câu: _____ giây   ║
║ Điểm Sau    : _____ / 10  | TG trung bình/câu: _____ giây   ║
║ Chênh lệch  : _____ điểm (Sau − Trước)                      ║
╠══════════════════════════════════════════════════════════════╣
║ QUAN SÁT TRONG GIỜ CHƠI                                     ║
║ Có đọc hướng dẫn không  : ☐ Có   ☐ Không                   ║
║ Số lần hỏi trợ giúp     : _____ lần                         ║
║ Có sử dụng gợi ý không  : ☐ Có   ☐ Không                   ║
║ Tự sửa sai khi nhận ra  : ☐ Có   ☐ Không  ☐ Không quan sát ║
║ Thái độ chung           : ☐ Hứng thú  ☐ Bình thường  ☐ Chán║
╠══════════════════════════════════════════════════════════════╣
║ GHI CHÚ THÊM                                                 ║
║ ___________________________________________________________  ║
║ ___________________________________________________________  ║
║ ___________________________________________________________  ║
╠══════════════════════════════════════════════════════════════╣
║ File CSV đã xuất : ☐ Có (tên file: ___________________)      ║
║                    ☐ Không (lý do: __________________)       ║
╚══════════════════════════════════════════════════════════════╝
```

---

## Yêu cầu dữ liệu tối thiểu để báo cáo

| Yêu cầu | Chi tiết |
|---|---|
| Số người tham gia | **Tối thiểu 5** người cùng bối cảnh |
| Tính nhất quán | Cùng bối cảnh, cùng thời lượng chơi (20 phút) |
| Dữ liệu gốc | Giữ nguyên file CSV, **không chỉnh sửa thủ công** |
| Nguồn dữ liệu | Chỉ dùng dữ liệu thu từ người tham gia thực tế; **không** dùng dữ liệu kiểm thử (`test data`) |
| Báo cáo nhóm thiếu | Nếu chưa đủ 12 nhóm AI hoặc chưa đủ 5 người, giữ danh sách nhóm/người còn thiếu |

> [!CAUTION]
> Không điền dữ liệu mẫu vào kết luận hiệu quả giáo dục. Chỉ báo cáo kết quả thu được thật. Ảnh trang "Trước và Sau" trong `Evidence/Demo-20261030/` là dữ liệu kiểm thử — **không phải** kết quả nghiên cứu trẻ em.

---

## Lưu trữ và quản lý dữ liệu

```
Session-Data/
  PXXX/
    observer-sheet-PXXX.pdf   ← scan phiếu giấy hoặc điền PDF
    before-after-PXXX.csv     ← file CSV xuất từ game (giữ nguyên)
    notes-PXXX.txt            ← ghi chú bổ sung nếu có
```

- Đặt tên thư mục theo mã người tham gia, không theo tên thật.
- Không đưa thư mục `Session-Data/` lên Git public.
- Sao lưu ít nhất một bản vào USB hoặc cloud riêng của nhóm nghiên cứu.

---

## Cảnh báo bảo mật và đạo đức

> [!CAUTION]
> **Không thu thập ảnh trẻ em** dưới bất kỳ hình thức nào trong nghiên cứu này.

| Quy tắc | Lý do |
|---|---|
| Không lưu tên thật trong game | Mã P001, P002... là đủ; tên thật tăng rủi ro dữ liệu cá nhân |
| Phụ huynh ký đồng ý trước | Bắt buộc theo chuẩn nghiên cứu trẻ em |
| Không thu thập ảnh webcam của trẻ | Tính năng "Lưu ảnh kiểm tra" phải **tắt** trong phiên nghiên cứu |
| Dừng ngay nếu trẻ có dấu hiệu lo lắng hoặc mệt mỏi | Sức khỏe và sự thoải mái của trẻ ưu tiên hơn dữ liệu |
| Người lớn chuẩn bị pin và vật thu gom đặc biệt | Trẻ không tự xử lý pin, kính vỡ, hoặc vật liệu nguy hiểm |
| Không ép trẻ hoàn thành nếu không muốn | Tham gia là tự nguyện; có thể dừng bất cứ lúc nào |

---

## Mẫu phiếu đồng ý phụ huynh (rút gọn)

```
PHIẾU ĐỒNG Ý THAM GIA NGHIÊN CỨU

Tên nghiên cứu: Đánh giá hiệu quả giáo dục phân loại rác qua trò chơi EcoSort Quest
Nhóm thực hiện: ___________________________
Liên hệ: ___________________________

Nghiên cứu này cho trẻ chơi một trò chơi máy tính giáo dục về phân loại rác (khoảng
45–60 phút) và đo kiến thức trước/sau qua bài kiểm tra ngắn. Dữ liệu được lưu
bằng mã vô danh, không có ảnh hay thông tin cá nhân.

Tôi đồng ý cho con tôi (mã: _______) tham gia và hiểu rằng:
- Con có thể dừng bất cứ lúc nào mà không cần giải thích.
- Dữ liệu chỉ được dùng cho mục đích nghiên cứu giáo dục, không thương mại.
- Không có ảnh hay tên thật được thu thập.

Chữ ký phụ huynh: _____________________ Ngày: ___/___/______
```

---

## Checklist trước khi bắt đầu phiên

- [ ] Phiếu đồng ý phụ huynh đã ký
- [ ] Mã người tham gia đã phân công (P___)
- [ ] Game chạy ổn định ở chế độ Offline
- [ ] Bối cảnh đã chọn và cố định
- [ ] Bảng ghi kết quả in sẵn, bút sẵn sàng
- [ ] Tắt thông báo và ứng dụng nền trên máy tính
- [ ] Tính năng "Lưu ảnh kiểm tra" đã tắt (không dùng webcam trong phiên này)
- [ ] Người giám sát biết cách xuất CSV

---

*Cập nhật lần cuối: 23/11/2026 — dựa trên DEMO-VALIDATION-20261030.md và hướng dẫn tính năng "Trước và sau"*
