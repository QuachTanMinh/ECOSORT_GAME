# Dataset và cách chia

Dữ liệu đầu vào hiện có trong workspace, không tạo ảnh giả để báo cáo chất lượng:

```text
../Dataset/dataset-plastic-metal-paper/
  plastic/  482 ảnh
  metal/    410 ảnh
  paper/    594 ảnh
```

Tổng 1.486 ảnh đọc được, kích thước gốc 512×384; không thấy ảnh trùng pixel chính xác. Nhãn lấy từ thư mục, chưa kiểm định thủ công từng nhãn. Tên file/số lượng phù hợp với ba lớp tương ứng của [TrashNet](https://github.com/garythung/trashnet), nhưng chưa đối chiếu checksum với upstream: đây là suy luận về nguồn gốc, không xác nhận provenance hoặc quyền phân phối.

Nếu cần tái tạo dữ liệu khi chuyển máy, dùng bản dataset cục bộ đã bàn giao riêng. Có thể lấy `dataset-resized.zip` từ TrashNet upstream và giữ ba thư mục trên để làm một dataset ứng viên, kiểm tra điều khoản nguồn; phải audit/chia/train lại nếu ảnh hoặc checksum khác manifest hiện tại. Không mặc định model/report hiện tại mô tả bản tải mới.

`prepare_dataset.py` xác thực ảnh và gộp cùng nhóm các ảnh trùng pixel hoặc dHash Hamming ≤4 trong cùng lớp. Cùng nhóm không xuất hiện ở nhiều split. Có 1.418 nhóm, nhóm lớn nhất 32 ảnh. Đây là heuristic, có thể gộp cả vật phẩm khác nhau hoặc bỏ sót vật phẩm giống nhau; không có object/session ID gốc nên chưa chứng minh object-disjoint. Có thể cung cấp `--groups-csv` với cột `path,group_id`, đầy đủ mọi ảnh, để gộp theo vật phẩm/phiên chụp có nguồn gốc xác thực.

| Split | Plastic | Metal | Paper | Tổng |
|---|---:|---:|---:|---:|
| Train | 336 | 286 | 416 | 1.038 |
| Validation | 73 | 62 | 89 | 224 |
| Test | 73 | 62 | 89 | 224 |

Tỉ lệ mục tiêu 70/15/15, seed 42; chấp nhận sai lệch nhỏ để giữ nhóm nguyên vẹn. Manifest lưu đường dẫn tương đối, hash pixel, nhãn và nhóm trong `reports/splits.csv`; audit trong `reports/dataset_audit.json`. Không sửa/xóa ảnh gốc. Augmentation chỉ dùng train, gồm lật ngang, xoay nhẹ, zoom và contrast. Validation/test không augmentation.

Phân bố nền/ánh sáng hẹp và thiếu unknown/OOD là giới hạn chính. Cần ảnh điện thoại độc lập theo `PHONE-TEST.md` trước khi nghiệm thu thực tế.
