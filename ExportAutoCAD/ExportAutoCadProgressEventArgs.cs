using System;

namespace VinTed.ExportAutoCAD
{
    /// <summary>
    /// Thông tin tiến trình xuất DWG.
    /// </summary>
    public class ExportAutoCadProgressEventArgs : EventArgs
    {
        private readonly int _currentSheet;
        private readonly int _totalSheets;
        private readonly string _sheetName;
        private readonly string _outputFile;
        private readonly string _message;

        public int CurrentSheet
        {
            get { return _currentSheet; }
        }

        public int TotalSheets
        {
            get { return _totalSheets; }
        }

        public string SheetName
        {
            get { return _sheetName; }
        }

        public string OutputFile
        {
            get { return _outputFile; }
        }

        public string Message
        {
            get { return _message; }
        }

        public ExportAutoCadProgressEventArgs(int currentSheet, int totalSheets, string sheetName, string outputFile, string message)
        {
            _currentSheet = currentSheet;
            _totalSheets = totalSheets;
            _sheetName = sheetName != null ? sheetName : String.Empty;
            _outputFile = outputFile != null ? outputFile : String.Empty;
            _message = message != null ? message : String.Empty;
        }
    }
}
