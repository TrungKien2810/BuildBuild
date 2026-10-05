# Pháo Âm Thanh — SR · CannonSonic · sonic

**Đã chốt form B theo yêu cầu người giao; form A đã xóa.** Bộ đầy đủ gồm43 frame/trạng thái,10 sheet nhóm, identity B và GIF chu kỳ ba thường→một mạnh. Xem HANDOFF.md để ghép lớp đèn và animation. Cơ chế theo yêu cầu người giao: ba sóng thường → phát thứ tư xung kích đẩy lùi, ba đèn báo nhịp. Chi tiết ở mechanics.md. Chỉ asset/tài liệu local, chưa viết gameplay.

## Form

- B: hai loa xếp tầng trên giá giáp, miệng loe vuông với lòng tròn tối; dáng cao và chắc hơn.
- Cùng bệ đá bát giác xám ấm và cổ thép ngắn của bộ pháo cũ. Hướng TRÁI, góc nhìn3/4 hơi cao, ánh sáng trên trái.
- Canvas128×128; sprite cao khoảng70px, rộng không quá76px; đáy bệ y96.
- Sắt tối chính, gỗ nâu phụ, đồng ở vành loa và ổ đèn. Identity ban đầu ba đèn tắt.
- Sóng thường xanh nhạt theo yêu cầu trực tiếp của người giao, khác màu cyan đậm/tinh thể của Pháo Băng. Xung kích vàng trắng là dấu hiệu phát mạnh, không biểu thị buff.
- Không đen thuần, blur, viền AA, bóng nền, người điều khiển, chữ hoặc UI trong sprite.

## Identity và concept tham chiếu

raw/ giữ ảnh nguồn B và VFX tạo bằng built-in image_gen; identity/ chứa form B đã chọn. concept/ chứa sóng thường và xung kích tĩnh, cùng cone footprint. qc/compare.png so form B cạnh bốn pháo cũ cùng3×; qc/concept-board.png mô tả chu kỳ bắn bằng art và nhãn QC.

identity-1.png và sheet-transparent.png dùng form B đã duyệt. Mẫu ba đèn sáng dùng nguồn B. Animation đầy đủ đã xuất trong phiên này. Không có cooldown.

## Bộ animation đã xuất cho form B

- idle/:4frame, cùng bệ cố định.
- attack/:6frame phát thường, kết thúc ở idle.
- attack-shock/:6frame phát thứ4, loa rung mạnh, cùng bệ.
- rhythm/ và lamp-overlay/: mỗi nhóm4trạng thái tương ứng0/1/2/3 nhịp; ghép lớp đèn với thân như HANDOFF.md.
- wave-normal/:4frame cung xanh nhạt mở ra rồi tan.
- wave-shock/:6frame cung vàng trắng cùng vùng đánh.
- hit-normal/ và hit-shock/: mỗi nhóm4frame chớp ngắn trên quái, không tính hit bổ sung.
- icon/icon-cannon-sonic.png:48×48.
- Sheetalpha đúng2×2/3×2 và GIF chu kỳ3thường→1mạnh.
- Không bullet là đạn vật thể, không cooldown hoặc animation đi lại.

## Phạm vi

Chỉ tạo/sửa trong tempart/CannonSonic/. Không sửa Assets, code, .meta, Unity, docs gốc hoặc pháo khác. Nhánh hiện tại asset/phao-thaituan05. art-style.md không có trong checkout, dùng luật asset-phao.md và bốn ảnh pháo mẫu. tempart/ bị Git ignore.
