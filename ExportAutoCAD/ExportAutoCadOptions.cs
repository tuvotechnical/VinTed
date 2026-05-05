using System;

namespace VinTed.ExportAutoCAD
{
    /// <summary>
    /// Chế độ chọn sheet để xuất DWG.
    /// </summary>
    public enum SheetExportMode
    {
        AllSheets = 0,
        CurrentSheet = 1,
        Custom = 2,
        FromTo = 3
    }

    /// <summary>
    /// Tập hợp các tùy chọn cho Export AutoCAD/DWG.
    /// Port 100% logic từ HNB frmDWGExport + ExportToDWG.
    /// </summary>
    public class ExportAutoCadOptions
    {
        private string _outputFolder;
        private string _baseFileName;
        private SheetExportMode _sheetMode;
        private string _customSheets;
        private int _fromSheet;
        private int _toSheet;
        private string _exportSpace;
        private bool _disableScreenUpdating;
        private bool _enableSilentOperation;

        public bool DisableScreenUpdating
        {
            get { return _disableScreenUpdating; }
            set { _disableScreenUpdating = value; }
        }

        public bool EnableSilentOperation
        {
            get { return _enableSilentOperation; }
            set { _enableSilentOperation = value; }
        }

        public string OutputFolder
        {
            get { return _outputFolder; }
            set { _outputFolder = value; }
        }

        public string BaseFileName
        {
            get { return _baseFileName; }
            set { _baseFileName = value; }
        }

        public SheetExportMode SheetMode
        {
            get { return _sheetMode; }
            set { _sheetMode = value; }
        }

        /// <summary>
        /// Danh sách sheet tùy chọn, phân cách bằng dấu phẩy. VD: "1,3,5"
        /// </summary>
        public string CustomSheets
        {
            get { return _customSheets; }
            set { _customSheets = value; }
        }

        public int FromSheet
        {
            get { return _fromSheet; }
            set { _fromSheet = value; }
        }

        public int ToSheet
        {
            get { return _toSheet; }
            set { _toSheet = value; }
        }

        /// <summary>
        /// "Model" hoặc "Layout" — quyết định SPACE và SCALING trong INI.
        /// </summary>
        public string ExportSpace
        {
            get { return _exportSpace; }
            set { _exportSpace = value; }
        }

        public ExportAutoCadOptions()
        {
            _outputFolder = String.Empty;
            _baseFileName = "Drawing";
            _sheetMode = SheetExportMode.AllSheets;
            _customSheets = "";
            _fromSheet = 1;
            _toSheet = 1;
            _exportSpace = "Model";
        }
    }
}
