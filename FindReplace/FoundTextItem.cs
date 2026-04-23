using Inventor;

namespace VinTed.FindReplace
{
    public class FoundTextItem
    {
        public Sheet SheetObj { get; set; }
        public object ParentObj { get; set; }
        public TextBox TBox { get; set; }
        public string ObjType { get; set; }
    }
}
