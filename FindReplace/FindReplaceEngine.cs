using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Inventor;

namespace VinTed.FindReplace
{
    /// <summary>
    /// Engine tìm kiếm và thay thế Text trong Drawing.
    /// Logic bảo toàn 100% từ iLogic gốc: ScanDocument, PerformReplace, ZoomToTarget.
    /// </summary>
    public class FindReplaceEngine
    {
        private readonly Application _invApp;
        private readonly DrawingDocument _drawDoc;
        private readonly List<FoundTextItem> _results;
        private int _currentIndex;
        private string _lastSearchTerm;

        public FindReplaceEngine(Application invApp, DrawingDocument drawDoc)
        {
            _invApp = invApp;
            _drawDoc = drawDoc;
            _results = new List<FoundTextItem>();
            _currentIndex = -1;
            _lastSearchTerm = "";
        }

        public int ResultCount
        {
            get { return _results.Count; }
        }

        public int CurrentIndex
        {
            get { return _currentIndex; }
        }

        /// <summary>
        /// Chuẩn hóa chuỗi: thay * thành space (bảo toàn từ iLogic gốc)
        /// </summary>
        public static string CleanStr(string s)
        {
            if (String.IsNullOrEmpty(s)) return "";
            return s.Replace("*", " ");
        }

        /// <summary>
        /// Kiểm tra tồn tại không phân biệt hoa thường (bảo toàn từ iLogic gốc)
        /// </summary>
        private bool IsMatch(string source, string target)
        {
            if (String.IsNullOrEmpty(source) || String.IsNullOrEmpty(target)) return false;
            return source.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Thuật toán quét — bảo toàn 100% logic iLogic gốc.
        /// Quét 4 loại: GeneralNote, LeaderNote, TitleBlock (Prompt), SketchedSymbol (Prompt).
        /// </summary>
        public void ScanDocument(string findText)
        {
            _results.Clear();
            string findStr = CleanStr(findText);
            if (String.IsNullOrEmpty(findStr)) return;

            foreach (Sheet oSheet in _drawDoc.Sheets)
            {
                // 1. General Notes
                foreach (GeneralNote oNote in oSheet.DrawingNotes.GeneralNotes)
                {
                    if (IsMatch(oNote.Text, findStr))
                    {
                        FoundTextItem item = new FoundTextItem();
                        item.SheetObj = oSheet;
                        item.ParentObj = oNote;
                        item.ObjType = "GeneralNote";
                        _results.Add(item);
                    }
                    else
                    {
                        try { Marshal.ReleaseComObject(oNote); } catch { }
                    }
                }

                // 2. Leader Notes
                foreach (LeaderNote oLeader in oSheet.DrawingNotes.LeaderNotes)
                {
                    if (IsMatch(oLeader.Text, findStr))
                    {
                        FoundTextItem item = new FoundTextItem();
                        item.SheetObj = oSheet;
                        item.ParentObj = oLeader;
                        item.ObjType = "LeaderNote";
                        _results.Add(item);
                    }
                    else
                    {
                        try { Marshal.ReleaseComObject(oLeader); } catch { }
                    }
                }

                // 3. Title Block
                if (oSheet.TitleBlock != null)
                {
                    try
                    {
                        TitleBlock oTB = oSheet.TitleBlock;
                        foreach (TextBox t in oTB.Definition.Sketch.TextBoxes)
                        {
                            if (t.FormattedText.Contains("<Prompt"))
                            {
                                if (IsMatch(oTB.GetResultText(t), findStr))
                                {
                                    FoundTextItem item = new FoundTextItem();
                                    item.SheetObj = oSheet;
                                    item.ParentObj = oTB;
                                    item.TBox = t;
                                    item.ObjType = "TitleBlock";
                                    _results.Add(item);
                                }
                            }
                        }
                    }
                    catch (Exception) { }
                }

                // 4. Sketched Symbols
                foreach (SketchedSymbol oSym in oSheet.SketchedSymbols)
                {
                    try
                    {
                        foreach (TextBox t in oSym.Definition.Sketch.TextBoxes)
                        {
                            if (t.FormattedText.Contains("<Prompt"))
                            {
                                if (IsMatch(oSym.GetResultText(t), findStr))
                                {
                                    FoundTextItem item = new FoundTextItem();
                                    item.SheetObj = oSheet;
                                    item.ParentObj = oSym;
                                    item.TBox = t;
                                    item.ObjType = "Symbol";
                                    _results.Add(item);
                                }
                            }
                        }
                    }
                    catch (Exception) { }
                }
            }
        }

        /// <summary>
        /// Find Next — bảo toàn logic iLogic: duyệt tuần tự, quay vòng khi hết.
        /// Trả về status message.
        /// </summary>
        public string FindNext(string findText)
        {
            string currentInput = CleanStr(findText);
            if (String.IsNullOrEmpty(currentInput)) return "";

            if (_lastSearchTerm != currentInput || _results.Count == 0)
            {
                ScanDocument(findText);
                _lastSearchTerm = currentInput;
                _currentIndex = -1;
            }

            if (_results.Count == 0)
            {
                return "Không tìm thấy kết quả nào.";
            }

            _currentIndex++;
            string status;
            if (_currentIndex >= _results.Count)
            {
                _currentIndex = 0;
                status = "Hết bản vẽ. Quay lại mục đầu.";
            }
            else
            {
                status = String.Format("Tìm thấy: {0} / {1}", _currentIndex + 1, _results.Count);
            }

            ZoomToCurrentTarget();
            return status;
        }

        /// <summary>
        /// Zoom đến vị trí target — bảo toàn logic iLogic gốc.
        /// </summary>
        private void ZoomToCurrentTarget()
        {
            if (_currentIndex < 0 || _currentIndex >= _results.Count) return;

            FoundTextItem item = _results[_currentIndex];
            if (item.SheetObj != _drawDoc.ActiveSheet)
            {
                item.SheetObj.Activate();
            }
            _drawDoc.SelectSet.Clear();
            _drawDoc.SelectSet.Select(item.ParentObj);

            try
            {
                _invApp.CommandManager.ControlDefinitions["AppZoomSelectCmd"].Execute();
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Thay thế text bằng Regex (case-insensitive) — bảo toàn từ iLogic gốc.
        /// </summary>
        private string PerformReplace(string source, string find, string replace)
        {
            return Regex.Replace(source, Regex.Escape(find), replace, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Replace mục hiện tại — bảo toàn logic iLogic gốc + Transaction.
        /// </summary>
        public string ReplaceCurrent(string findText, string replaceText)
        {
            if (_currentIndex < 0 || _currentIndex >= _results.Count)
                return "Chưa có mục nào được chọn. Hãy bấm Find trước.";

            FoundTextItem item = _results[_currentIndex];
            string f = CleanStr(findText);
            string r = CleanStr(replaceText);

            Transaction txn = _invApp.TransactionManager.StartTransaction(
                _invApp.ActiveDocument, "VinTed Find Replace");
            try
            {
                ReplaceItem(item, f, r);
                txn.End();
                _drawDoc.Update();
                _results.RemoveAt(_currentIndex);
                _currentIndex--;
                return "Đã thay thế mục hiện tại.";
            }
            catch (Exception ex)
            {
                try { txn.Abort(); } catch { }
                return "Lỗi thay thế: " + ex.Message;
            }
        }

        /// <summary>
        /// Replace All — bảo toàn logic iLogic gốc + Transaction.
        /// </summary>
        public string ReplaceAll(string findText, string replaceText)
        {
            string f = CleanStr(findText);
            string r = CleanStr(replaceText);
            if (String.IsNullOrEmpty(f)) return "";

            ScanDocument(findText);
            int count = 0;

            Transaction txn = _invApp.TransactionManager.StartTransaction(
                _invApp.ActiveDocument, "VinTed Replace All");
            try
            {
                foreach (FoundTextItem item in _results)
                {
                    try
                    {
                        ReplaceItem(item, f, r);
                        count++;
                    }
                    catch (Exception) { }
                }
                txn.End();
                _drawDoc.Update();
                _results.Clear();
                _currentIndex = -1;
                return String.Format("Đã thay thế tất cả {0} vị trí.", count);
            }
            catch (Exception ex)
            {
                try { txn.Abort(); } catch { }
                return "Lỗi: " + ex.Message;
            }
        }

        /// <summary>
        /// Thay thế nội dung theo loại — bảo toàn 100% Select Case từ iLogic gốc.
        /// </summary>
        private void ReplaceItem(FoundTextItem item, string find, string replace)
        {
            if (item.ObjType == "GeneralNote")
            {
                GeneralNote obj = (GeneralNote)item.ParentObj;
                obj.FormattedText = PerformReplace(obj.FormattedText, find, replace);
            }
            else if (item.ObjType == "LeaderNote")
            {
                LeaderNote obj = (LeaderNote)item.ParentObj;
                obj.FormattedText = PerformReplace(obj.FormattedText, find, replace);
            }
            else if (item.ObjType == "TitleBlock")
            {
                TitleBlock obj = (TitleBlock)item.ParentObj;
                obj.SetPromptResultText(item.TBox,
                    PerformReplace(obj.GetResultText(item.TBox), find, replace));
            }
            else if (item.ObjType == "Symbol")
            {
                SketchedSymbol obj = (SketchedSymbol)item.ParentObj;
                obj.SetPromptResultText(item.TBox,
                    PerformReplace(obj.GetResultText(item.TBox), find, replace));
            }
        }
    }
}
