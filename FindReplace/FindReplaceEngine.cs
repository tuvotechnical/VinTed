using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Inventor;

namespace VinTed.FindReplace
{
    public class FindReplaceEngine
    {
        private readonly Application _invApp;
        private readonly DrawingDocument _drawDoc;
        private readonly List<FoundTextItem> _results;
        private int _currentIndex;
        private string _lastSearchTerm;
        private ScanOptions _options;

        public FindReplaceEngine(Application invApp, DrawingDocument drawDoc)
        {
            _invApp = invApp;
            _drawDoc = drawDoc;
            _results = new List<FoundTextItem>();
            _currentIndex = -1;
            _lastSearchTerm = "";
            _options = new ScanOptions();
        }

        public int ResultCount { get { return _results.Count; } }
        public int CurrentIndex { get { return _currentIndex; } }
        public ScanOptions Options { get { return _options; } set { _options = value; } }

        public static string CleanStr(string s)
        {
            if (String.IsNullOrEmpty(s)) return "";
            return s.Replace("*", " ");
        }

        private bool IsMatch(string source, string target)
        {
            if (String.IsNullOrEmpty(source) || String.IsNullOrEmpty(target)) return false;
            return source.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private FoundTextItem MakeItem(Sheet sh, object parent, string type)
        {
            FoundTextItem it = new FoundTextItem();
            it.SheetObj = sh;
            it.ParentObj = parent;
            it.ObjType = type;
            return it;
        }

        public void ScanDocument(string findText)
        {
            _results.Clear();
            string f = CleanStr(findText);
            if (String.IsNullOrEmpty(f)) return;

            foreach (Sheet oSheet in _drawDoc.Sheets)
            {
                if (_options.GeneralNotes) ScanGeneralNotes(oSheet, f);
                if (_options.LeaderNotes) ScanLeaderNotes(oSheet, f);
                if (_options.TitleBlocks) ScanTitleBlock(oSheet, f);
                if (_options.SketchedSymbols) ScanSketchedSymbols(oSheet, f);
                if (_options.Dimensions) ScanDimensions(oSheet, f);
                if (_options.HoleThreadNotes) ScanHoleThreadNotes(oSheet, f);
                if (_options.ChamferNotes) ScanChamferNotes(oSheet, f);
                if (_options.BendNotes) ScanBendNotes(oSheet, f);
                if (_options.PunchNotes) ScanPunchNotes(oSheet, f);
                if (_options.ViewLabels) ScanViewLabels(oSheet, f);
                if (_options.PartsLists) ScanPartsLists(oSheet, f);
                if (_options.CustomTables) ScanCustomTables(oSheet, f);
                if (_options.RevisionTables) ScanRevisionTables(oSheet, f);
                if (_options.HoleTables) ScanHoleTables(oSheet, f);
                if (_options.Balloons) ScanBalloons(oSheet, f);
                if (_options.FeatureControlFrames) ScanFeatureControlFrames(oSheet, f);
                if (_options.SurfaceTextureSymbols) ScanSurfaceTextures(oSheet, f);
                if (_options.SketchTextBoxes) ScanSketchTextBoxes(oSheet, f);
            }
        }

        #region Scan Methods

        private void ScanGeneralNotes(Sheet s, string f)
        {
            foreach (GeneralNote o in s.DrawingNotes.GeneralNotes)
            {
                if (IsMatch(o.Text, f))
                    _results.Add(MakeItem(s, o, "GeneralNote"));
                else
                    try { Marshal.ReleaseComObject(o); } catch { }
            }
        }

        private void ScanLeaderNotes(Sheet s, string f)
        {
            foreach (LeaderNote o in s.DrawingNotes.LeaderNotes)
            {
                if (IsMatch(o.Text, f))
                    _results.Add(MakeItem(s, o, "LeaderNote"));
                else
                    try { Marshal.ReleaseComObject(o); } catch { }
            }
        }

        private void ScanTitleBlock(Sheet s, string f)
        {
            if (s.TitleBlock == null) return;
            try
            {
                TitleBlock tb = s.TitleBlock;
                foreach (TextBox t in tb.Definition.Sketch.TextBoxes)
                {
                    if (t.FormattedText.Contains("<Prompt"))
                    {
                        if (IsMatch(tb.GetResultText(t), f))
                        {
                            FoundTextItem it = MakeItem(s, tb, "TitleBlock");
                            it.TBox = t;
                            _results.Add(it);
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        private void ScanSketchedSymbols(Sheet s, string f)
        {
            foreach (SketchedSymbol sym in s.SketchedSymbols)
            {
                try
                {
                    foreach (TextBox t in sym.Definition.Sketch.TextBoxes)
                    {
                        if (t.FormattedText.Contains("<Prompt"))
                        {
                            if (IsMatch(sym.GetResultText(t), f))
                            {
                                FoundTextItem it = MakeItem(s, sym, "Symbol");
                                it.TBox = t;
                                _results.Add(it);
                            }
                        }
                    }
                }
                catch (Exception) { }
            }
        }

        private void ScanDimensions(Sheet s, string f)
        {
            try
            {
                foreach (DrawingDimension dim in s.DrawingDimensions)
                {
                    try
                    {
                        string txt = dim.Text.Text;
                        if (IsMatch(txt, f))
                            _results.Add(MakeItem(s, dim, "Dimension"));
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }

        private void ScanHoleThreadNotes(Sheet s, string f)
        {
            try
            {
                foreach (HoleThreadNote o in s.DrawingNotes.HoleThreadNotes)
                {
                    try
                    {
                        if (IsMatch(o.FormattedHoleThreadNote, f))
                            _results.Add(MakeItem(s, o, "HoleThreadNote"));
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }

        private void ScanChamferNotes(Sheet s, string f)
        {
            try
            {
                foreach (ChamferNote o in s.DrawingNotes.ChamferNotes)
                {
                    try
                    {
                        if (IsMatch(o.FormattedText, f))
                            _results.Add(MakeItem(s, o, "ChamferNote"));
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }

        private void ScanBendNotes(Sheet s, string f)
        {
            try
            {
                foreach (BendNote o in s.DrawingNotes.BendNotes)
                {
                    try
                    {
                        if (IsMatch(o.FormattedText, f))
                            _results.Add(MakeItem(s, o, "BendNote"));
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }

        private void ScanPunchNotes(Sheet s, string f)
        {
            try
            {
                foreach (PunchNote o in s.DrawingNotes.PunchNotes)
                {
                    try
                    {
                        if (IsMatch(o.FormattedText, f))
                            _results.Add(MakeItem(s, o, "PunchNote"));
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }

        private void ScanViewLabels(Sheet s, string f)
        {
            try
            {
                foreach (DrawingView v in s.DrawingViews)
                {
                    try
                    {
                        if (v.ShowLabel)
                        {
                            string lbl = v.Label.FormattedText;
                            if (IsMatch(lbl, f))
                                _results.Add(MakeItem(s, v, "ViewLabel"));
                        }
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }

        private void ScanPartsLists(Sheet s, string f)
        {
            try
            {
                foreach (PartsList pl in s.PartsLists)
                {
                    try
                    {
                        for (int r = 1; r <= pl.PartsListRows.Count; r++)
                        {
                            PartsListRow row = pl.PartsListRows[r];
                            for (int c = 1; c <= pl.PartsListColumns.Count; c++)
                            {
                                try
                                {
                                    PartsListCell cell = row[c];
                                    string val = cell.Value;
                                    if (IsMatch(val, f))
                                    {
                                        FoundTextItem it = MakeItem(s, pl, "PartsList");
                                        it.RowIndex = r;
                                        it.ColIndex = c;
                                        it.RowObj = row;
                                        _results.Add(it);
                                    }
                                }
                                catch (Exception) { }
                            }
                        }
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }

        private void ScanCustomTables(Sheet s, string f)
        {
            try
            {
                foreach (CustomTable tbl in s.CustomTables)
                {
                    try
                    {
                        for (int r = 1; r <= tbl.Rows.Count; r++)
                        {
                            Row row = tbl.Rows[r];
                            for (int c = 1; c <= tbl.Columns.Count; c++)
                            {
                                try
                                {
                                    string val = row[c].Value;
                                    if (IsMatch(val, f))
                                    {
                                        FoundTextItem it = MakeItem(s, tbl, "CustomTable");
                                        it.RowIndex = r;
                                        it.ColIndex = c;
                                        it.RowObj = row;
                                        _results.Add(it);
                                    }
                                }
                                catch (Exception) { }
                            }
                        }
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }

        private void ScanRevisionTables(Sheet s, string f)
        {
            try
            {
                foreach (RevisionTable tbl in s.RevisionTables)
                {
                    try
                    {
                        for (int r = 1; r <= tbl.RevisionTableRows.Count; r++)
                        {
                            RevisionTableRow row = tbl.RevisionTableRows[r];
                            for (int c = 1; c <= tbl.RevisionTableColumns.Count; c++)
                            {
                                try
                                {
                                    RevisionTableCell cell = row[c];
                                    string val = cell.Text;
                                    if (IsMatch(val, f))
                                    {
                                        FoundTextItem it = MakeItem(s, tbl, "RevisionTable");
                                        it.RowIndex = r;
                                        it.ColIndex = c;
                                        it.RowObj = row;
                                        _results.Add(it);
                                    }
                                }
                                catch (Exception) { }
                            }
                        }
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }

        private void ScanHoleTables(Sheet s, string f)
        {
            try
            {
                foreach (HoleTable tbl in s.HoleTables)
                {
                    try
                    {
                        for (int r = 1; r <= tbl.HoleTableRows.Count; r++)
                        {
                            HoleTableRow row = tbl.HoleTableRows[r];
                            for (int c = 1; c <= tbl.HoleTableColumns.Count; c++)
                            {
                                try
                                {
                                    HoleTableCell cell = row[c];
                                    string val = cell.Text;
                                    if (IsMatch(val, f))
                                    {
                                        FoundTextItem it = MakeItem(s, tbl, "HoleTable");
                                        it.RowIndex = r;
                                        it.ColIndex = c;
                                        it.RowObj = row;
                                        _results.Add(it);
                                    }
                                }
                                catch (Exception) { }
                            }
                        }
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }

        private void ScanBalloons(Sheet s, string f)
        {
            try
            {
                foreach (Balloon b in s.Balloons)
                {
                    try
                    {
                        foreach (BalloonValueSet vs in b.BalloonValueSets)
                        {
                            try
                            {
                                string val = vs.OverrideValue;
                                if (!String.IsNullOrEmpty(val) && IsMatch(val, f))
                                    _results.Add(MakeItem(s, vs, "Balloon"));
                            }
                            catch (Exception) { }
                        }
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }

        private void ScanFeatureControlFrames(Sheet s, string f)
        {
            try
            {
                foreach (FeatureControlFrame fcf in s.FeatureControlFrames)
                {
                    try
                    {
                        // FCF: quét DatumIdentifier, InlineNote, Tolerance, DatumOne/Two/Three
                        bool matched = false;
                        try { if (IsMatch(fcf.DatumIdentifier, f)) matched = true; } catch { }
                        if (!matched)
                        {
                            try
                            {
                                foreach (FeatureControlFrameRow row in fcf.FeatureControlFrameRows)
                                {
                                    try { if (IsMatch(row.Tolerance, f)) { matched = true; break; } } catch { }
                                    if (!matched) { try { if (IsMatch(row.InlineNote, f)) { matched = true; break; } } catch { } }
                                    if (!matched) { try { if (IsMatch(row.DatumOne, f)) { matched = true; break; } } catch { } }
                                    if (!matched) { try { if (IsMatch(row.DatumTwo, f)) { matched = true; break; } } catch { } }
                                    if (!matched) { try { if (IsMatch(row.DatumThree, f)) { matched = true; break; } } catch { } }
                                }
                            }
                            catch { }
                        }
                        if (matched)
                            _results.Add(MakeItem(s, fcf, "FeatureControlFrame"));
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }

        private void ScanSurfaceTextures(Sheet s, string f)
        {
            try
            {
                foreach (SurfaceTextureSymbol sts in s.SurfaceTextureSymbols)
                {
                    try
                    {
                        // SurfaceTexture: quét MaximumRoughness, ProductionMethod, MachiningAllowance
                        bool matched = false;
                        try { if (IsMatch(sts.MaximumRoughness, f)) matched = true; } catch { }
                        if (!matched) { try { if (IsMatch(sts.MinimumRoughness, f)) matched = true; } catch { } }
                        if (!matched) { try { if (IsMatch(sts.ProductionMethod, f)) matched = true; } catch { } }
                        if (!matched) { try { if (IsMatch(sts.MachiningAllowance, f)) matched = true; } catch { } }
                        if (matched)
                            _results.Add(MakeItem(s, sts, "SurfaceTexture"));
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }

        private void ScanSketchTextBoxes(Sheet s, string f)
        {
            try
            {
                foreach (DrawingSketch sk in s.Sketches)
                {
                    try
                    {
                        foreach (TextBox tb in sk.TextBoxes)
                        {
                            try
                            {
                                if (IsMatch(tb.Text, f))
                                {
                                    FoundTextItem it = MakeItem(s, sk, "SketchText");
                                    it.TBox = tb;
                                    _results.Add(it);
                                }
                            }
                            catch (Exception) { }
                        }
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }

        #endregion

        public string FindNext(string findText)
        {
            string ci = CleanStr(findText);
            if (String.IsNullOrEmpty(ci)) return "";

            if (_lastSearchTerm != ci || _results.Count == 0)
            {
                ScanDocument(findText);
                _lastSearchTerm = ci;
                _currentIndex = -1;
            }

            if (_results.Count == 0)
                return "Không tìm thấy kết quả nào.";

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
        /// Buộc scan lại khi options thay đổi.
        /// </summary>
        public void InvalidateCache()
        {
            _lastSearchTerm = "";
            _results.Clear();
            _currentIndex = -1;
        }

        private void ZoomToCurrentTarget()
        {
            if (_currentIndex < 0 || _currentIndex >= _results.Count) return;

            FoundTextItem item = _results[_currentIndex];

            // Chuyển sheet nếu cần
            try
            {
                if (item.SheetObj != _drawDoc.ActiveSheet)
                    item.SheetObj.Activate();
            }
            catch (Exception) { }

            // Xác định đối tượng có thể Select được trong Inventor
            object selectTarget = ResolveSelectableObject(item);

            // Clear selection trước
            try { _drawDoc.SelectSet.Clear(); } catch { }

            // Select đối tượng
            if (selectTarget != null)
            {
                try
                {
                    _drawDoc.SelectSet.Select(selectTarget);
                }
                catch (Exception) { }
            }

            // Đưa cửa sổ Inventor lên foreground để zoom hoạt động
            try
            {
                IntPtr hwnd = (IntPtr)_invApp.MainFrameHWND;
                SetForegroundWindow(hwnd);
            }
            catch (Exception) { }

            // Thực thi Zoom to Selection
            try
            {
                _invApp.CommandManager.ControlDefinitions["AppZoomSelectCmd"].Execute();
            }
            catch (Exception)
            {
                // Fallback: Zoom All nếu ZoomSelect thất bại
                try
                {
                    _invApp.CommandManager.ControlDefinitions["AppZoomAllCmd"].Execute();
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// Chuyển đổi ParentObj thành object có thể Select() trong Inventor.
        /// Một số type (BalloonValueSet, table cells...) không phải là đối tượng drawing trực tiếp.
        /// </summary>
        private object ResolveSelectableObject(FoundTextItem item)
        {
            string t = item.ObjType;

            // Các đối tượng có thể select trực tiếp
            if (t == "GeneralNote" || t == "LeaderNote" || t == "Dimension"
                || t == "HoleThreadNote" || t == "ChamferNote" || t == "BendNote"
                || t == "PunchNote" || t == "FeatureControlFrame" || t == "SurfaceTexture"
                || t == "PartsList" || t == "CustomTable" || t == "RevisionTable"
                || t == "HoleTable" || t == "TitleBlock" || t == "Symbol")
            {
                return item.ParentObj;
            }

            // Balloon: ParentObj là BalloonValueSet, cần lấy Balloon cha
            if (t == "Balloon")
            {
                try
                {
                    BalloonValueSet vs = (BalloonValueSet)item.ParentObj;
                    return vs.Parent;
                }
                catch (Exception) { return item.ParentObj; }
            }

            // ViewLabel: ParentObj là DrawingView — select được
            if (t == "ViewLabel")
            {
                return item.ParentObj;
            }

            // SketchText: ParentObj là DrawingSketch, TBox là TextBox — select TBox
            if (t == "SketchText")
            {
                if (item.TBox != null) return item.TBox;
                return item.ParentObj;
            }

            return item.ParentObj;
        }

        // P/Invoke: đưa cửa sổ Inventor lên foreground
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private string PerformReplace(string source, string find, string replace)
        {
            return Regex.Replace(source, Regex.Escape(find), replace, RegexOptions.IgnoreCase);
        }

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
                    try { ReplaceItem(item, f, r); count++; }
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

        private void ReplaceItem(FoundTextItem item, string find, string replace)
        {
            string t = item.ObjType;

            if (t == "GeneralNote")
            {
                GeneralNote o = (GeneralNote)item.ParentObj;
                o.FormattedText = PerformReplace(o.FormattedText, find, replace);
            }
            else if (t == "LeaderNote")
            {
                LeaderNote o = (LeaderNote)item.ParentObj;
                o.FormattedText = PerformReplace(o.FormattedText, find, replace);
            }
            else if (t == "TitleBlock")
            {
                TitleBlock o = (TitleBlock)item.ParentObj;
                o.SetPromptResultText(item.TBox,
                    PerformReplace(o.GetResultText(item.TBox), find, replace));
            }
            else if (t == "Symbol")
            {
                SketchedSymbol o = (SketchedSymbol)item.ParentObj;
                o.SetPromptResultText(item.TBox,
                    PerformReplace(o.GetResultText(item.TBox), find, replace));
            }
            else if (t == "Dimension")
            {
                DrawingDimension o = (DrawingDimension)item.ParentObj;
                o.Text.FormattedText = PerformReplace(o.Text.FormattedText, find, replace);
            }
            else if (t == "HoleThreadNote")
            {
                HoleThreadNote o = (HoleThreadNote)item.ParentObj;
                o.FormattedHoleThreadNote = PerformReplace(o.FormattedHoleThreadNote, find, replace);
            }
            else if (t == "ChamferNote")
            {
                ChamferNote o = (ChamferNote)item.ParentObj;
                o.FormattedText = PerformReplace(o.FormattedText, find, replace);
            }
            else if (t == "BendNote")
            {
                BendNote o = (BendNote)item.ParentObj;
                o.FormattedText = PerformReplace(o.FormattedText, find, replace);
            }
            else if (t == "PunchNote")
            {
                PunchNote o = (PunchNote)item.ParentObj;
                o.FormattedText = PerformReplace(o.FormattedText, find, replace);
            }
            else if (t == "ViewLabel")
            {
                DrawingView o = (DrawingView)item.ParentObj;
                o.Label.FormattedText = PerformReplace(o.Label.FormattedText, find, replace);
            }
            else if (t == "PartsList")
            {
                PartsList pl = (PartsList)item.ParentObj;
                PartsListRow row = pl.PartsListRows[item.RowIndex];
                PartsListCell cell = row[item.ColIndex];
                cell.Value = PerformReplace(cell.Value, find, replace);
            }
            else if (t == "CustomTable")
            {
                CustomTable tbl = (CustomTable)item.ParentObj;
                Row row = tbl.Rows[item.RowIndex];
                row[item.ColIndex].Value = PerformReplace(row[item.ColIndex].Value, find, replace);
            }
            else if (t == "RevisionTable")
            {
                RevisionTable tbl = (RevisionTable)item.ParentObj;
                RevisionTableRow row = tbl.RevisionTableRows[item.RowIndex];
                RevisionTableCell cell = row[item.ColIndex];
                cell.Text = PerformReplace(cell.Text, find, replace);
            }
            else if (t == "HoleTable")
            {
                HoleTable tbl = (HoleTable)item.ParentObj;
                HoleTableRow row = tbl.HoleTableRows[item.RowIndex];
                HoleTableCell cell = row[item.ColIndex];
                cell.FormattedText = PerformReplace(cell.FormattedText, find, replace);
            }
            else if (t == "Balloon")
            {
                BalloonValueSet vs = (BalloonValueSet)item.ParentObj;
                vs.OverrideValue = PerformReplace(vs.OverrideValue, find, replace);
            }
            else if (t == "FeatureControlFrame")
            {
                // FCF: thay thế DatumIdentifier và các field trong từng row
                FeatureControlFrame o = (FeatureControlFrame)item.ParentObj;
                try { o.DatumIdentifier = PerformReplace(o.DatumIdentifier, find, replace); } catch { }
                try
                {
                    foreach (FeatureControlFrameRow row in o.FeatureControlFrameRows)
                    {
                        try { row.Tolerance = PerformReplace(row.Tolerance, find, replace); } catch { }
                        try { row.InlineNote = PerformReplace(row.InlineNote, find, replace); } catch { }
                        try { row.DatumOne = PerformReplace(row.DatumOne, find, replace); } catch { }
                        try { row.DatumTwo = PerformReplace(row.DatumTwo, find, replace); } catch { }
                        try { row.DatumThree = PerformReplace(row.DatumThree, find, replace); } catch { }
                    }
                }
                catch { }
            }
            else if (t == "SurfaceTexture")
            {
                // SurfaceTexture: thay thế từng field riêng biệt
                SurfaceTextureSymbol o = (SurfaceTextureSymbol)item.ParentObj;
                try { o.MaximumRoughness = PerformReplace(o.MaximumRoughness, find, replace); } catch { }
                try { o.MinimumRoughness = PerformReplace(o.MinimumRoughness, find, replace); } catch { }
                try { o.ProductionMethod = PerformReplace(o.ProductionMethod, find, replace); } catch { }
                try { o.MachiningAllowance = PerformReplace(o.MachiningAllowance, find, replace); } catch { }
            }
            else if (t == "SketchText")
            {
                item.TBox.FormattedText = PerformReplace(item.TBox.FormattedText, find, replace);
            }
        }
    }
}
