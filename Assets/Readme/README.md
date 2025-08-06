# Đánh giá project

## Hệ thống
- Hệ thống có vẻ như được thiết kế theo mô hình MVC hoặc không phải vì mới thấy tách biệt phần view (Nhưng view lại gọi thẳng control).
- Tight coupling quá mức:
  - UiManager có dependency là GameManager, nhưng GameManager cũng lại có dependency là UiManager ??
  - Các IMenu đúng ra phải là modul thấp hơn của UIManager nhưng lại có dependency là UIManager.
- God class: UIManager và GameManager có vẻ là god class, không nên làm thế nếu muốn scale project thì 2 thằng này sẽ rất to và rối. Vi phạm single responsibility.
- Ưu điểm: code kiểu này cho prototype được cái nhanh, tiện.
- Nhược điểm: dự án scale to ra sẽ rất khó, muốn sửa hoặc thêm chức năng mới sẽ tốn thời gian do code rối.
- Gợi ý lại tổ chức dự án:
  - Xem lại mô hình MVC hoặc muốn scale to hơn thì dùng MVP.
  - Nên dùng FSM thay vì state thường.
  - Thay vì inject dependency để call method thì chuyển qua observer hoặc hệ thống event bus tránh phụ thuộc.