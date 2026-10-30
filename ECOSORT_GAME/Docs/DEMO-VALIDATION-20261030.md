# Kiểm chứng và phát triển bản demo ngày 30 tháng 10 năm 2026

## Nhận diện ảnh và webcam

Vào **Phân loại bằng ảnh**. Chọn **Mở / chụp lại**, dùng **Đổi camera** nếu có nhiều thiết bị, đặt một vật vào khung rồi **Chụp ảnh**. Chọn dự đoán của mình trước khi phân tích. Cảnh báo sáng và chi tiết chỉ hỗ trợ cách chụp; không phát hiện đầy đủ ảnh mờ, vật lạ hoặc nhiều vật và không bảo đảm AI đúng. Các cảnh báo không tự thay đổi đáp án hay độ tin cậy.

**Lưu ảnh kiểm tra** chỉ ghi ảnh khi người dùng bấm. Ảnh và `manifest.csv` nằm ở thư mục `PhotoReview` bên trong `Application.persistentDataPath`; trạng thái game hiển thị đường dẫn sau khi lưu. Không tự lưu mọi khung hình webcam. Điền nhãn vật liệu thật, mã vật lý và điều kiện chụp vào bảng, ví dụ:

```csv
path,label,object_id,condition
photo-001.jpg,Metal,can-01,daylight
photo-002.jpg,Metal,can-01,dim-light
photo-003.jpg,Paper,sheet-01,daylight
```

Nhãn hợp lệ: `Plastic, Metal, Paper, Organic, Cardboard, GreenGlass, BrownGlass, WhiteGlass, Clothes, Shoes, Batteries, Trash`. Quan sát vật thật để gán nhãn; không sao chép nhãn dự đoán của AI. Với vật khó xác định, không dùng làm đáp án chuẩn. Người lớn chuẩn bị pin và các vật có yêu cầu thu gom riêng; bài kiểm tra không yêu cầu trẻ xử lý chúng.

Để đánh giá ngoài tập huấn luyện, tự chụp các vật mới, giữ nguyên một mã cho nhiều góc chụp của cùng vật. Nên có tối thiểu ba vật khác nhau mỗi nhóm và ba điều kiện chụp, đồng thời ghi nguồn ảnh; đây là kế hoạch thu thập, không phải số ảnh hiện đã kiểm chứng. Tránh dùng lại Dataset/Kaggle hay ảnh đã dùng chọn mô hình. Công cụ kiểm tra trùng tệp trong bộ đánh giá nhưng không tự chứng minh độc lập với mọi ảnh huấn luyện.

Chạy từ `backend` bằng môi trường Python của dự án:

```powershell
python tools/evaluate_photos.py "C:\DuongDan\PhotoReview\manifest.csv" --output data/photo-review --forbid-dir ..\Dataset --forbid-dir ..\Kaggle
```

Kết quả gồm `predictions.csv` và `summary.json`: đúng trên toàn bộ ảnh, tỷ lệ được chấp nhận, đúng trên các ảnh được chấp nhận, nhóm còn thiếu, nhầm lẫn, điều kiện chụp và mã SHA-256 mô hình. Không cộng nhiều góc chụp của một vật thành nhiều vật độc lập. Chưa thu đủ 12 nhóm thì báo cáo phải giữ danh sách nhóm thiếu.

## Theo dõi kết quả học trước và sau

Mở **Kết quả học tập → Trước và sau**. Chọn cùng một bối cảnh cho hai lượt. Thực hiện kiểm tra 10 câu trong 75 giây, quay lại bấm **Ghi lượt đầu**; sau khi học/ôn hoặc chơi nhiệm vụ, kiểm tra lại rồi bấm **Ghi lượt sau**. Lượt sau phải khác mã và có thời điểm muộn hơn lượt đầu. Học có gợi ý không được dùng làm lượt đánh giá.

**Xuất bảng kết quả** ghi CSV UTF-8 tại `Exports` trong thư mục dữ liệu của game. Bảng chứa pha trước/sau, bối cảnh, mã lượt, thời điểm, tổng câu, tỷ lệ, lựa chọn và thời gian từng câu. Câu bỏ trống vẫn nằm trong tổng 10 câu. Chỉ số cho biết thay đổi của cặp lượt đã chọn, không tự xác nhận trò chơi tạo ra thay đổi đó. Bộ câu có thể khác nhau, người chơi có thể quen thao tác và số mẫu có thể nhỏ.

Khi tổ chức buổi chơi thử, dùng mã người tham gia thay cho họ tên; ghi lại khoảng thời gian học, mức hỗ trợ của người lớn, máy và bối cảnh. Giữ hướng dẫn và thời lượng nhất quán. Quan sát thêm việc hiểu lời giải, tự sửa sai và có làm theo gợi ý hay không. Có thể kiểm tra lại sau vài ngày để xem ghi nhớ. Chỉ báo cáo kết quả thu được thật; không điền dữ liệu mẫu vào kết luận hiệu quả giáo dục.

## Cloud và Docker

`backend/tools/verify_live_cloud.py` là phép kiểm tra có ghi dữ liệu thật, chỉ chạy khi được chủ tài khoản cho phép. Nó sao lưu tiến trình vào `backend/data`, giữ nguyên điểm/mở khóa, kiểm tra gửi và đọc lại dự đoán thật cùng một bản ghi điểm có mã nghiệm thu. Không in khóa; không đưa `.env` hoặc bản sao dữ liệu cá nhân vào Git.

`backend/tools/verify_container.py` chỉ chạy với container dùng kho JSON cục bộ riêng. Công cụ đối chiếu mã SHA-256 mô hình, kiểm tra ảnh, ảnh lỗi, tiến trình, lịch sử và gửi lại lượt học không bị nhân đôi. Chạy lại với `--read-only` sau khi khởi động lại container để kiểm tra volume giữ được dữ liệu. Không dùng container cloud đang phục vụ người chơi cho phép thử này.

## Hiệu năng

`DemoBenchmark` chỉ bật khi executable nhận `-ecosort-benchmark`. Phép đo khởi động nhiệm vụ bếp, bỏ qua ba giây đầu rồi lấy mẫu 12 giây. Dùng cùng bản Unity, GPU, độ phân giải và cấu hình cho cả hai lần. Câu lệnh nhận `-ecosort-benchmark-output` để chọn tệp JSON kết quả. Công cụ tự thoát sau khi đo; chơi bình thường không tự bật phép đo.

Phép đo này là cảnh bếp đứng yên, không đại diện toàn bộ chiến dịch. So sánh FPS, trung vị và phân vị 95 thời gian khung hình; không lấy một cảnh nhỏ để bảo đảm mọi máy đều mượt. Bài PlayMode bổ sung đo thời gian 20.000 lần tìm vật phẩm; đó là phép đo một hàm, không phải FPS của executable. Bộ đếm cấp phát theo luồng có thể trả 0 trên Mono, nên không dùng giá trị 0 để kết luận không có cấp phát.

## Kết quả thực hiện trên máy này

| Phạm vi | Kết quả |
|---|---|
| Unity EditMode | 65 đạt, 0 lỗi |
| Unity PlayMode đầy đủ | 31 đạt, 0 lỗi, không bỏ qua; có API Docker/model thật và webcam |
| Kiểm tra lại khung ảnh sau sửa chữ nền | 2 đạt; là kiểm tra lại, không cộng vào 31 bài độc lập |
| Python | 16 đạt, 0 lỗi |
| Webcam vật lý | Chụp được khung 1280 × 720 trong bộ nhớ; không ghi ảnh trong phép thử |
| Adafruit thật | Đọc lại khớp dự đoán model hiện tại, điểm nghiệm thu và tiến trình; giữ nguyên điểm/mở khóa cũ |
| Docker | Image mới có đúng 12 nhãn, đúng SHA-256; API và dữ liệu volume qua restart đã đạt |
| Dịch vụ đang dùng | Container cổng 8000 đã cập nhật sau khi người dùng đồng ý; trạng thái healthy, kho Adafruit |
| Windows | Build thành công, Unity báo 108.611.342 bytes; DLL runtime được cập nhật sau sửa cuối |

Các kết quả và mã kiểm tra nằm trong [verification.json](Evidence/Demo-20261030/verification.json), [cloud](Evidence/Demo-20261030/live-cloud.json), [Docker API](Evidence/Demo-20261030/docker-api.json), [Docker restart](Evidence/Demo-20261030/docker-restart.json), [webcam](Evidence/Demo-20261030/webcam.json). Ảnh [trang trước sau](Evidence/Demo-20261030/learning-before-after-20261030.png) sử dụng dữ liệu kiểm thử, không phải kết quả nghiên cứu trẻ em; ảnh [hướng dẫn chụp](Evidence/Demo-20261030/photo-quality-advice-20261030.png) dùng ảnh tối được tạo để kiểm tra bố cục, không phải ảnh webcam của người dùng.

Đo 20.000 lần tìm vật phẩm trong PlayMode: **62,550 ms trước, 19,888 ms sau**. Đã bỏ truy vấn lọc/sắp xếp mỗi khung hình, dùng vòng lặp tìm khoảng cách bình phương và chỉ tạo lại lời nhắc khi trạng thái thay đổi. Phép đo này có giới hạn môi trường, không dùng để suy ra tỷ lệ tăng tốc toàn game.

Trên RTX 2050 ở 1280 × 720, cảnh bếp đứng yên: trung vị khoảng **7,08 ms/khung** trước và sau. FPS trung bình đo được khoảng **141 trước, 131–132 sau**, có ảnh hưởng lúc kích hoạt cửa sổ; chạy nền có mẫu bị giới hạn 30 FPS. Vì vậy **chưa có bằng chứng FPS tổng thể tăng**. Giữ đầy đủ các mẫu trong [performance.json](Evidence/Demo-20261030/performance.json), không chọn riêng mẫu thuận lợi để kết luận.

Image Docker hiện tại được build từ runtime local đã có đúng phiên bản thư viện để tránh tải lại TensorFlow 620 MB; lệnh `pip check` đạt. `Dockerfile` vẫn mặc định dùng `python:3.12-slim` trên máy mới; có thể dùng `--build-arg BASE_IMAGE=ecosort-api:before-20261030` nếu máy có runtime local tương ứng. Đây là kiểm chứng image hiện tại, không phải đo thời gian cold build trên một máy mới. Bản cũ giữ ở tag `ecosort-api:before-20261030`; dữ liệu container cũ được sao lưu trong `backend/data/docker-backup-20261030` trên máy này, không đưa lên Git.

Chưa hoàn tất đánh giá ảnh mới đủ 12 nhóm và thử nghiệm với trẻ em vì cần thu thập vật/ảnh và người tham gia thực tế. Đã bổ sung quy trình và công cụ để thực hiện hai việc đó; không huấn luyện lại mô hình trong đợt này. AR trên điện thoại được loại khỏi phạm vi theo yêu cầu.
