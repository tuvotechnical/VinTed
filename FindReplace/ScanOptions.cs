namespace VinTed.FindReplace
{
    /// <summary>
    /// Cấu hình bộ lọc cho ScanDocument — xác định loại text nào được quét.
    /// Mặc định tất cả đều bật (true).
    /// </summary>
    public class ScanOptions
    {
        public bool GeneralNotes { get; set; }
        public bool LeaderNotes { get; set; }
        public bool TitleBlocks { get; set; }
        public bool SketchedSymbols { get; set; }
        public bool Dimensions { get; set; }
        public bool HoleThreadNotes { get; set; }
        public bool ChamferNotes { get; set; }
        public bool BendNotes { get; set; }
        public bool PunchNotes { get; set; }
        public bool ViewLabels { get; set; }
        public bool PartsLists { get; set; }
        public bool CustomTables { get; set; }
        public bool RevisionTables { get; set; }
        public bool HoleTables { get; set; }
        public bool Balloons { get; set; }
        public bool FeatureControlFrames { get; set; }
        public bool SurfaceTextureSymbols { get; set; }
        public bool SketchTextBoxes { get; set; }

        public ScanOptions()
        {
            GeneralNotes = true;
            LeaderNotes = true;
            TitleBlocks = true;
            SketchedSymbols = true;
            Dimensions = true;
            HoleThreadNotes = true;
            ChamferNotes = true;
            BendNotes = true;
            PunchNotes = true;
            ViewLabels = true;
            PartsLists = true;
            CustomTables = true;
            RevisionTables = true;
            HoleTables = true;
            Balloons = true;
            FeatureControlFrames = true;
            SurfaceTextureSymbols = true;
            SketchTextBoxes = true;
        }

        /// <summary>
        /// Kiểm tra xem tất cả các option có đang bật hay không.
        /// </summary>
        public bool IsAllSelected()
        {
            return GeneralNotes && LeaderNotes && TitleBlocks && SketchedSymbols
                && Dimensions && HoleThreadNotes && ChamferNotes && BendNotes
                && PunchNotes && ViewLabels && PartsLists && CustomTables
                && RevisionTables && HoleTables && Balloons
                && FeatureControlFrames && SurfaceTextureSymbols
                && SketchTextBoxes;
        }

        /// <summary>
        /// Bật hoặc tắt tất cả.
        /// </summary>
        public void SetAll(bool value)
        {
            GeneralNotes = value;
            LeaderNotes = value;
            TitleBlocks = value;
            SketchedSymbols = value;
            Dimensions = value;
            HoleThreadNotes = value;
            ChamferNotes = value;
            BendNotes = value;
            PunchNotes = value;
            ViewLabels = value;
            PartsLists = value;
            CustomTables = value;
            RevisionTables = value;
            HoleTables = value;
            Balloons = value;
            FeatureControlFrames = value;
            SurfaceTextureSymbols = value;
            SketchTextBoxes = value;
        }
    }
}
