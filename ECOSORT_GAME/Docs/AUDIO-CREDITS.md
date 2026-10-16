# Âm thanh và cách kiểm tra

## Menu bánh răng và âm thao tác

Bấm **icon bánh răng** ở góc trên bên phải trên các màn hình để mở Cài đặt. Tab Âm thanh có ba thanh volume, bật/tắt, Nghe thử và hai nút thử âm đúng/sai. Tab Hình ảnh có cửa sổ/toàn màn hình và bật/tắt bóng đổ. Tab Lối chơi có tốc độ luyện tập. Cài đặt tự lưu; cửa sổ/toàn màn hình áp dụng bản Windows, không ép kích thước Game view của Editor. Đóng hoặc ESC khôi phục đúng trạng thái trước khi mở, kể cả đang pause sẵn.

Âm kéo/thả dùng thêm hai file cùng gói Kenney CC0:

- select_003.ogg → Resources/Audio/pickup.ogg: phát một lần khi nhấc rác.
- drop_003.ogg → Resources/Audio/drop.ogg: phát khi thả vào một thùng hợp lệ.

Sau tiếng thả khoảng 0,14 giây, phát confirmation_002 nếu đúng hoặc error_003 nếu sai. AR mô phỏng cũng có phản hồi đúng/sai. Thả ngoài hoặc hủy kéo không phát tiếng “vào thùng”. Chế độ kiểm tra chỉ phát pickup/drop, giữ ẩn đúng/sai đến cuối lượt. Tất cả dùng mức Hiệu ứng và âm lượng tổng; không phát tiếng lặp liên tục theo từng frame kéo.

**Điều chỉnh độ lớn:** Bánh răng → Âm thanh có ba thanh 0–100%: Âm lượng tổng, Nhạc nền, Hiệu ứng. Kéo để nghe thay đổi ngay, tự lưu qua scene và lần mở game. Mặc định tổng 100%, nhạc 65%, hiệu ứng 100%; nhạc nền lớn hơn mức 30% của bản trước. Âm từng kênh = mức tổng × mức kênh; đặt tổng 0% thì Nghe thử vẫn im vì giữ lựa chọn của người chơi. Nút bật/tắt âm độc lập với ba mức này.

Ba hiệu ứng từ **Kenney Interface Sounds 1.0**, giấy phép **CC0**, cho phép sử dụng trong dự án giáo dục và phân phối source:

Nguồn: https://kenney.nl/assets/interface-sounds

- confirmation_002.ogg → Resources/Audio/correct.ogg
- error_003.ogg → Resources/Audio/retry.ogg
- click_003.ogg → Resources/Audio/click.ogg

Giữ bản License-Kenney.txt nguyên gốc cạnh các clip. Không cần tải lại khi clone. Nhạc nền nhẹ được tổng hợp trong EcoSortAudio, không dùng bản nhạc thương mại. Các hiệu ứng có fallback tổng hợp nếu thiếu asset, nhưng source chuẩn có đủ ba clip.

Âm dùng AudioSource 2D, không bị giảm theo khoảng cách camera. Có listener trên camera, hai source riêng nhạc/hiệu ứng. Nhạc nền chạy khi vào gameplay, kể cả kiểm tra; âm phản hồi đúng/sai chỉ ở chế độ học/ôn/chiến dịch. Nút UI có click; chế độ kiểm tra không tiết lộ đáp án qua âm thanh.

Bánh răng → Âm thanh → **Nghe thử**. Nút bật âm game, khôi phục volume listener và bỏ **Mute Audio** của Editor rồi phát hiệu ứng đúng. Nếu vẫn không nghe, kiểm tra Windows Volume Mixer/thiết bị đầu ra. Source không tự thay đổi cài đặt Windows. Test tự động kiểm tra clip, sample và routing; không chứng minh loa/tai nghe đang phát âm.

Bối cảnh mới có sân rộng phủ khung hình, đường đi, vườn cây, nhà/cửa sổ, trường/ghế và công viên/hồ nước. Camera gameplay rộng hơn. Tất cả trang trí không có collider để không cản kéo thả. Không cần chạy lại Setup scene.
