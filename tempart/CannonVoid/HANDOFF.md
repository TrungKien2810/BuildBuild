# Pháo Hắc Tinh — form B

Form B được người giao chọn. Toàn bộ asset nằm trong tempart/CannonVoid/. Các bản A/B trong identity-variant-* và concept/ giữ làm tư liệu; bộ sử dụng lấy identity-1.png và các clip ở bảng dưới.

| Thư mục | File frame | Số frame | Canvas |
| --- | --- | ---: | --- |
| identity | identity-1.png | 1 | 144×128 |
| idle | idle-1.png … idle-4.png | 4 | 144×128 |
| attack | attack-1.png … attack-6.png | 6 | 144×128 |
| bullet | projectile-1.png … projectile-4.png | 4 | 128×128 |
| vortex-open | vortex-open-1.png … vortex-open-4.png | 4 | 128×128 |
| vortex-loop-0 | vortex-1.png … vortex-4.png | 4 | 128×128 |
| vortex-loop-1 | vortex-1.png … vortex-4.png | 4 | 128×128 |
| vortex-loop-2 | vortex-1.png … vortex-4.png | 4 | 128×128 |
| absorb | absorb-1.png … absorb-4.png | 4 | 128×128 |
| collapse | collapse-1.png … collapse-4.png | 4 | 128×128 |
| bullet-impact | impact-1.png … impact-6.png | 6 | 128×128 |
| bullet-impact-1 | impact-1.png … impact-6.png | 6 | 128×128 |
| bullet-impact-2 | impact-1.png … impact-6.png | 6 | 128×128 |
| icon | icon-cannon-void.png | 1 | 48×48 |

57 frame riêng ngoài identity. Mỗi thư mục có sheet-transparent.png: 4 frame xếp 2×2; 6 frame xếp 3×2; identity/icon một ô. Thứ tự đọc trái→phải, trên→dưới.

## Chuỗi phát và cộng dồn

Thân pháo: idle → attack → idle. Không có clip hoặc asset cooldown.

Tại vị trí mục tiêu: lõi bay → vortex-open → vortex-loop-0/1/2 trong thời gian hút → collapse → bullet-impact tương ứng mức cộng dồn → kết thúc.

- Mức 0: 0 cộng dồn, vortex-loop-0 và bullet-impact.
- Mức 1: 1–2 cộng dồn, vortex-loop-1 và bullet-impact-1.
- Mức 2: 3–5 cộng dồn, vortex-loop-2 và bullet-impact-2.
- Khi quái chết trong vùng: phát absorb một lần, đổi mức loop nếu vượt ngưỡng. Mỗi absorb vẽ hạt đi từ phía trên bên phải vào tâm; có thể xoay hiệu ứng quanh tâm theo phía quái chết.
- Vòng hút giữ cùng vùng 58×32 px ở cả ba mức; tăng độ sáng và mật độ năng lượng. Cộng dồn tăng sát thương nổ cuối, không tăng bán kính.
- Vụ nổ cùng giới hạn union 64×48 px cho cả ba mức, khác mật độ shard và sắc độ. Mức damage cụ thể nằm trong mechanics.md.
- Collapse dùng một chuỗi chung để nén về điểm sáng trước vụ nổ.

Gợi ý nhịp để xem art: idle 4 frame/640ms lặp; attack 6 frame/~780ms; bullet 4 frame/~360ms; open 4 frame/~240ms; vòng hút lặp tổng 1,5s; absorb 4 frame/~200ms chồng lên vortex; collapse 4 frame/200ms; impact 6 frame/~490ms. Đây là nhịp duyệt hình, không phải code timing.

## Anchor và scale

Thân quay TRÁI, camera hơi nhìn xuống. Identity/idle/attack 144×128, chân bệ ở y=96. Bệ/cổ từ y=71 trở xuống lấy nguyên pixel identity ở mọi frame; recoil phía trên +2/+1 px sang PHẢI. Điểm lõi/miệng nòng đề xuất (43,49). Anchor chuẩn các VFX là (64,64) trên canvas128×128. Icon48×48.

Sprite và sheet có alpha0/255, màu được gom vào palette nhỏ, không dùng đen thuần. Export dùng nearest-neighbor. Không đặt blur hoặc filtering làm mềm khi thử.

Các chuỗi VFX dùng cùng scale trong mỗi chuỗi để giữ kích thước mở/co/nổ; không kéo từng frame về cùng kích thước. Sheet nguồn co lõi và nổ có bố trí ô không đều, được cắt theo vùng riêng rồi đăng ký tâm; sheet bàn giao vẫn đúng grid2×2/3×2. Absorb dùng vị trí mote cuối làm anchor để đường hạt kết thúc đúng tâm. Các patch năng lượng ở thân được lấy từ ảnh nguồn image_gen; silhouette kim loại và bệ khóa theo identity B.

## Xem thử và tái xuất

- qc/overview.png: các thành phần ở tỉ lệ3×; icon6×.
- qc/all-assets.png: toàn bộ frame theo từng hàng.
- qc/preview.gif: phóng lõi → hút/tăng mức0→1→2 → co → nổ mạnh.
- qc/preview-zero.gif và qc/preview-medium.gif: hai mức còn lại.
- qc/clips/: từng clip riêng để kiểm tra vòng lặp.
- qc/validation-production.md: kết quả kiểm tra kích thước/alpha/bệ/rest-pose và giải mã GIF.
- qc/export-production.ps1: tái xuất bộ animation từ raw/ và identity đã chọn.
- qc/export.ps1: tái xuất identity/concept, mặc định chọn B.
- production-prompts.md: toàn bộ prompt sản xuất; prompts.md: prompt concept ban đầu.
- raw/: ảnh nguồn alpha tạo bằng built-in image_gen.

Chỉ asset local, không có Unity import/meta, gameplay hoặc dữ liệu pháo. tempart/ bị Git ignore.
