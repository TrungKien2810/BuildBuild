# Pháo Hắc Tinh — SSR · CannonVoid · void

Trạng thái: người giao đã chốt **form B**. Bộ animation và VFX được xuất trong thư mục này; xem `HANDOFF.md` để lấy file, anchor và chuỗi phát. Chỉ làm asset, chưa triển khai gameplay.

## Form pháo

Thân pháo sắt ngắn và rộng, hướng TRÁI, lõi tròn navy tối nằm trong miệng nòng và được giữ bằng ba ngàm kim loại. Vòng cơ khí phía sau miệng nòng, thân có một mảng gỗ nhỏ, đai đỏ và chi tiết đồng tiết chế. Cổ trụ thép thấp và bệ đá bát giác theo bộ pháo đang có.

- A: vỏ nòng tròn với các vòng sắt lớn, lõi và ngàm giữ lộ rõ.
- B: thân khóa nòng bọc giáp vuông, dáng nặng hơn, có ray giật ngắn.
- Sắt tối chủ đạo, gỗ nâu phụ, accent đỏ/crimson cho sát thương, highlight ivory. Không dùng cyan hoặc tím để tránh lẫn với Băng/Nguyền. Lõi đen dùng navy đậm, không đen thuần.
- Frame thân 144×128, đáy bệ y=96, mục tiêu vùng sprite khoảng 70 px cao. Bệ đứng yên khi làm animation sau duyệt.

## Cơ chế để xem thử

Phóng lõi đến điểm đông quái → hố đen hút 1,5 giây → nén rồi nổ. Trong lúc hút, mỗi quái chết trong vùng thêm một cộng dồn, kể cả do pháo khác giết. Đề xuất +15% sát thương gốc mỗi cộng dồn, tối đa 5: nổ từ 100 lên tối đa 175. Thời gian hút và bán kính giữ nguyên. Chi tiết ở mechanics.md.

## File concept

- raw/identity-a.png, raw/identity-b.png: ảnh nguồn identity.
- identity/identity-variant-a.png, identity/identity-variant-b.png: hai sprite để duyệt.
- identity/identity-1.png, identity/sheet-transparent.png: **form B đã chốt**, dùng làm identity chính thức của bộ asset local.
- concept/vortex-base.png: hố đen 0 cộng dồn.
- concept/vortex-max.png: hố đen tối đa cộng dồn, vành sáng hơn và nhiều hạt năng lượng quanh lõi.
- concept/explosion-max.png: vụ nổ sau khi lõi co lại.
- concept/vortex-sheet-transparent.png: hai trạng thái tĩnh cạnh nhau, không phải clip animation.
- qc/compare.png: A/B cạnh bốn pháo cũ, cùng tỷ lệ 3×.
- qc/concept-board.png: form pháo và các trạng thái hố đen ở tỷ lệ pixel thật.
- qc/validation.md: canvas, đáy bệ, alpha, màu; cần duyệt form và độ rõ VFX.
- prompts.md: prompt nguyên văn, dùng built-in image_gen và nền alpha.

## Bộ asset sau duyệt

Idle 4 frame; attack 6 frame; bullet 4 frame lõi bay; vortex-open 4 frame; vortex-loop 4 frame cho mỗi mức cường độ 0/1/2; absorb 4 frame hạt năng lượng chạy vào lõi; collapse 4 frame; bullet-impact 4–6 frame theo mức cường độ; icon 48×48.

Mức cường độ hiển thị đề xuất: 0 cộng dồn, 1–2 cộng dồn, 3–5 cộng dồn. Phần hiển thị không cần dựng riêng năm phiên bản thân pháo. Khóa diện tích ellipse để người chơi không hiểu nhầm cộng dồn tăng bán kính. Mỗi lần tăng cộng dồn có nhịp hút sáng ngắn và hạt bay vào tâm.

Không tạo asset/animation cooldown. Sau phát đạn thân pháo trở về idle. Chỉ ghi trong tempart/CannonVoid/, không sửa các pháo khác hoặc Assets/code/docs gốc. Art-style.md không có trong checkout này; dùng luật asset-phao.md và mẫu hiện có. tempart/ bị Git ignore nên file bàn giao đang ở local.
