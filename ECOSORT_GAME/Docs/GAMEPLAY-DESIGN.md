# Thiết kế gameplay để thuyết trình

## Mục tiêu học tập

Người chơi nhận biết vật liệu chính của đồ dùng quen thuộc, thực hành phân loại ba nhóm và biết chuẩn bị vật phẩm trước thu gom. Game dùng vật phẩm sạch, đơn vật liệu trong bài học; không khẳng định mọi loại nhựa/giấy đều được địa phương chấp nhận tái chế.

## Vòng chơi

Quan sát vật phẩm trên băng chuyền → giữ/kéo/thả vào thùng → nhận điểm và giải thích → vật phẩm kế tiếp → tổng kết → mở level tiếp theo. Mỗi lượt có 10 câu. Chưa nhặt thì vật phẩm chạy vòng lại, không bị trừ điểm vì đi hết băng chuyền. Thả ngoài không tính là đáp án. Tạm dừng và mất focus hủy kéo để tránh kẹt thao tác.

Level 1 là học có hướng dẫn: không giới hạn thời gian, gợi ý nhóm vật liệu, ba mẫu dễ mỗi nhóm. Level 2 dùng bốn mẫu mỗi nhóm và timer 75 giây. Level 3 dùng toàn bộ sáu mẫu mỗi nhóm và timer 50 giây. Băng chuyền chạy lần lượt 0.65 / 0.85 / 1.05 đơn vị Unity mỗi giây. Timer dừng trong pause và khi đọc phản hồi (1.5 giây/câu) để không phạt thời gian học.

Điểm đúng +10, sai −5 và không âm; đúng ít nhất 7/10 để qua. Mỗi đáp án có tên vật liệu và bài học riêng. Sổ tay 18 vật phẩm mở từ menu để học lại mà không cần bắt đầu một lượt. Danh mục được chọn ngẫu nhiên không lặp sớm trong từng nhóm; không hứa đủ 18 mẫu sẽ xuất hiện trong một lượt 10 câu.

## Danh mục

| Nhựa | Kim loại | Giấy |
|---|---|---|
| Chai nước | Lon nước nhôm | Báo |
| Chai nước giặt | Hộp thiếc thực phẩm | Thùng carton |
| Cốc nhựa | Nắp kim loại | Túi giấy |
| Hộp nhựa | Khay nhôm sạch | Vở giấy |
| Nắp nhựa | Cốc inox | Phong bì giấy |
| Chai dầu gội | Hộp bánh kim loại | Lõi cuộn giấy |

Mô hình được dựng bằng primitive, có một pickup collider rộng hơn thân một chút để vật phẩm nhỏ dễ kéo. Màu vật phẩm đa dạng, không luôn trùng màu thùng, tránh việc người chơi chỉ ghép màu. Các thùng vẫn có nhãn chữ để hỗ trợ phân loại.

## Kỹ thuật và cấu hình

`WasteCatalog.cs` chứa dữ liệu học tập và chọn mẫu. `WorldFactory.Catalog.cs` dựng mô hình. `ConveyorMotion.cs` cập nhật vạch băng chuyền; `EcoSortApp` dùng cùng quãng đường để mang rác. Phần luật điểm ở `GameRules.cs` không phụ thuộc dựng hình hoặc transport.

Đồ họa hiện là mô hình runtime. Không cần chạy lại Setup để thấy vật phẩm mới; giữ scene/prefab/template gốc. Tốc độ hiện nằm ở Update của EcoSortApp, timer/số câu/ngưỡng qua màn nằm trong ba LevelConfig asset. Bài học và mẫu vật phẩm nằm trong WasteCatalog, thuận tiện cho thành viên sửa nội dung.

## Tích hợp và phạm vi báo cáo

Chế độ ngoại tuyến dùng mock chỉ để luyện tập và kiểm thử UI. Chế độ Online dùng FastAPI/model thật và Adafruit theo cấu hình backend. Thêm mẫu 3D không có nghĩa model AI nhận biết tên của cả 18 vật phẩm: đầu ra AI vẫn chỉ Plastic/Metal/Paper.

Thử thách sau nhận diện hiện là 3D mô phỏng trên PC. Phần này không cung cấp bằng chứng AR tracking trên điện thoại. Backend cloud lưu snapshot mới nhất cho một demo player; chưa có xác thực hoặc multi-user. Ghi rõ những giới hạn này trong báo cáo thay vì trình bày phiên bản Windows như một ứng dụng AR mobile đã nghiệm thu.

## Minh chứng

Xem `TESTING.md`, XML trong `TestResults` và ảnh trong `Evidence`. Ảnh `02-gameplay.png` là UI gameplay; `07-field-guide.png` là sổ tay; `08-catalog-*.png` là fixture đồ họa trưng bày sáu mẫu một nhóm, không phải gameplay có sáu vật phẩm đồng thời. Không dùng ảnh fixture để mô tả cơ chế hàng đợi vật phẩm mà game chưa triển khai.
