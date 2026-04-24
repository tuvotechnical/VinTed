using Inventor;

namespace VinTed.FindReplace
{
    /// <summary>
    /// Đại diện cho một kết quả tìm kiếm trong bản vẽ.
    /// Mở rộng: hỗ trợ thêm thông tin vị trí trong bảng (Row/Column index),
    /// và giá trị gốc của cell để hiển thị/đối chiếu.
    /// </summary>
    public class FoundTextItem
    {
        public Sheet SheetObj { get; set; }
        public object ParentObj { get; set; }
        public TextBox TBox { get; set; }
        public string ObjType { get; set; }

        /// <summary>
        /// Chỉ số dòng (1-indexed) — dùng cho Table cells.
        /// </summary>
        public int RowIndex { get; set; }

        /// <summary>
        /// Chỉ số cột (1-indexed) — dùng cho Table cells.
        /// </summary>
        public int ColIndex { get; set; }

        /// <summary>
        /// Tham chiếu đến đối tượng Table gốc (PartsListRow, CustomTableRow, v.v.)
        /// </summary>
        public object RowObj { get; set; }
    }
}
