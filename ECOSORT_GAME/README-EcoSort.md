# EcoSort Quest — Unity client

Bản Windows / Unity Editor 6000.6.2f1. Xem [README repo](../README.md) và [hướng dẫn chạy đầy đủ](../HUONG-DAN-CHAY-PROJECT.md) để cài backend, Docker và Adafruit IO trên máy mới.

## Chạy game

Mở `Assets/_EcoSortQuest/Scenes/MainMenu.unity`, Play. Vào **Kết nối backend** để chọn ngoại tuyến hoặc Online. Ngoại tuyến chạy được campaign/sổ tay; Scan chỉ dùng nhãn luyện tập. Online dùng URL FastAPI, không nhập khóa Adafruit vào client.

Giữ chuột trái trên rác đang chạy, kéo vào thùng rồi thả. Ba level có 10 câu/lượt, đúng +10, sai −5 và đạt 7/10 để qua. Băng chuyền mang vật phẩm chạy vòng lại nếu chưa nhặt; thả ngoài trả vật phẩm về vị trí trước khi kéo và không chấm điểm. Pause hoặc mất focus hủy thao tác kéo. Timer dừng trong pause và khi xem phản hồi đáp án.

18 vật phẩm, sáu mỗi nhóm Nhựa/Kim loại/Giấy. Level 1 dùng ba mẫu đầu mỗi nhóm và có gợi ý; Level 2 dùng bốn mẫu; Level 3 có toàn bộ sáu mẫu. Mẫu được xáo trộn và tránh lặp sớm trong từng nhóm. Sổ tay ở MainMenu giải thích từng vật phẩm, cách làm rỗng/giữ khô và lưu ý địa phương.

## Source chính

- `Gameplay/WasteCatalog.cs`: tên, nhóm, bài học và bộ chọn vật phẩm.
- `Gameplay/WorldFactory.Catalog.cs`: 18 mô hình primitive với collider dễ nhặt.
- `Gameplay/ConveyorMotion.cs`: vạch băng chuyền chuyển động/vòng lại.
- `Core/EcoSortApp.cs`: gameplay, kéo thả, timer, điểm và progress.
- `Core/EcoSortApp.Library.cs`: sổ tay và UI cấu hình backend.
- `Core/EcoSortApp.Scan.cs`: webcam, ảnh, nhận diện và thử thách mô phỏng.
- `Networking`: service Mock/REST, DTO, capture và ApiSettings.

Đường dẫn trên tính từ `Assets/_EcoSortQuest/Scripts`. Không cần plugin đồ họa hoặc asset trả phí. Ba prefab mẫu cũ được giữ để setup/tương thích; gameplay campaign dựng mô hình theo catalog khi chạy. Geometry và UI runtime không xuất hiện trong Hierarchy sau khi thoát Play, đây là hành vi có chủ đích.

## Build và test

`EcoSort → Build Windows` build x64 tại `Builds/Windows`. Bản standalone mặc định ngoại tuyến, cửa sổ 1600×900 có resize; lựa chọn kết nối lưu riêng trên máy người chơi. Editor dùng asset Rest/Mock cho đầu phiên; lựa chọn từ menu áp dụng xuyên scene trong phiên đó.

`Window → General → Test Runner`: chạy EditMode/PlayMode. Kết quả mới: **20 EditMode + 8 PlayMode passed**, kèm một lần chạy fixture đồ họa 1/1. Xem [TESTING](Docs/TESTING.md), XML tại `Docs/TestResults` và ảnh tại `Docs/Evidence`.

Test PlayMode dùng mock được cô lập, không gửi lên tài khoản cloud. Backend REST/AI/Adafruit cần chạy và kiểm tra theo hướng dẫn repo. AR vẫn là thử thách 3D mô phỏng PC, chưa phải AR Foundation/ARCore. Không dùng kết quả mock để chứng minh tích hợp thật. Tiến trình lưu cục bộ; chưa có hàng đợi bền vững gửi lại lịch sử score/AR khi lỗi.

