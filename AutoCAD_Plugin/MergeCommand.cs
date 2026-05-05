using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace VinTed.AutoCAD
{
    public class MergeCommand
    {
        private ObjectId Standard_TextStyleID { get; set; }
        public double SaveTargetInsertPoint { get; set; }
        public double minPoint { get; set; }
        public double maxPoint { get; set; }
        public double Gap { get; set; }

        private HashSet<string> processedFiles;
        private static string fileToSave;
        private static string pathContainingDWG;
        private static string SpecifyModel;

        [CommandMethod("VINTED_MERGE")]
        public void ExecuteMerge()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            ed.WriteMessage("\n--- VinTed Core Console Merge Started ---");

            string argsFile = Path.Combine(Path.GetTempPath(), "VinTed_MergeArgs.txt");
            if (!File.Exists(argsFile))
            {
                ed.WriteMessage("\nError: Cannot find args file: " + argsFile);
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(argsFile);
                if (lines.Length < 4) return;

                pathContainingDWG = lines[0];
                fileToSave = lines[1];
                double gap;
                double.TryParse(lines[2], out gap);
                bool deleteFileAfterMerging;
                bool.TryParse(lines[3], out deleteFileAfterMerging);
                this.Gap = gap;

                if (!Directory.Exists(pathContainingDWG))
                {
                    ed.WriteMessage("\nError: Thư mục chứa file .dwg không tồn tại.");
                    return;
                }

                if (!CreateNewDrawingFile(fileToSave))
                {
                    ed.WriteMessage("\nError: Tệp đang được sử dụng " + fileToSave);
                    return;
                }

                ConfigurationTargetDrawing();

                string str = string.Empty;
                string[] files = new string[0];

                using (Database targetDb = new Database(true, false))
                {
                    targetDb.ReadDwgFile(fileToSave, FileShare.ReadWrite, false, null);
                    
                    this.processedFiles = new HashSet<string>();
                    bool firstFile = true;
                    
                    string baseName = Path.GetFileNameWithoutExtension(fileToSave);
                    string searchPattern = baseName + "_*.dwg";
                    files = Directory.GetFiles(pathContainingDWG, searchPattern)
                                     .OrderBy(f => SortedToGetFileNumber(f))
                                     .ToArray();

                    if (files.Length == 0)
                    {
                        files = Directory.GetFiles(pathContainingDWG, "Sheet_*.dwg")
                                         .OrderBy(f => SortedToGetFileNumber(f))
                                         .ToArray();
                    }

                    if (files.Length == 0)
                    {
                        ed.WriteMessage("\nError: Không tìm thấy các file " + searchPattern);
                        return;
                    }

                    foreach (string text in files)
                    {
                        if (text != fileToSave && !this.processedFiles.Contains(text))
                        {
                            str = text;
                            if (!dwgFileProcessing(text, targetDb, firstFile))
                            {
                                ed.WriteMessage("\nError: Dừng xử lý.");
                                return;
                            }
                            this.processedFiles.Add(text);
                            firstFile = false;
                        }
                    }
                    
                    targetDb.SaveAs(fileToSave, DwgVersion.Current);
                    
                    if (deleteFileAfterMerging)
                    {
                        DeleteDWGfile(files);
                    }
                }

                ed.WriteMessage("\nMerge hoàn tất thành công: " + fileToSave);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\nLỗi xử lý: " + ex.Message);
            }
            finally
            {
                SpecifyModel = string.Empty;
                pathContainingDWG = string.Empty;
                fileToSave = string.Empty;
                SaveTargetInsertPoint = 0.0;
            }
        }

        private bool ConfigurationTargetDrawing()
        {
            try
            {
                using (Database database = new Database(true, false))
                {
                    database.ReadDwgFile(fileToSave, FileShare.ReadWrite, false, null);
                    CreateOrUpdateTextStyle(database, "Standard", "Arial.ttf");
                    database.Lunits = 2;
                    database.Luprec = 0;
                    database.Insunits = UnitsValue.Millimeters;
                    database.Aunits = 0;
                    database.Auprec = 0;
                    database.SaveAs(fileToSave, DwgVersion.Current);
                }
                return true;
            }
            catch (System.Exception ex)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nLỗi khởi tạo style: " + ex.Message);
                return false;
            }
        }

        private void DeleteDWGfile(string[] files)
        {
            foreach (string path in files)
            {
                try { File.Delete(path); } catch { }
            }
        }

        private static int SortedToGetFileNumber(string fileName)
        {
            string name = Path.GetFileNameWithoutExtension(fileName);
            int num = name.LastIndexOf('_');
            if (num >= 0 && num < name.Length - 1)
            {
                string text = name.Substring(num + 1);
                int result;
                if (int.TryParse(text, out result))
                {
                    return result;
                }
            }
            return 0;
        }

        private bool CreateNewDrawingFile(string fileName)
        {
            try
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
                if (!string.IsNullOrEmpty(fileName))
                {
                    using (Database database = new Database())
                    {
                        database.SaveAs(fileName, DwgVersion.Current);
                    }
                    return true;
                }
            }
            catch { }
            return false;
        }

        private bool dwgFileProcessing(string sourceFilename, Database targetDb, bool firstFile)
        {
            int num = 0;
            bool flag = false;
            using (Database database2 = new Database(true, false))
            {
                database2.ReadDwgFile(sourceFilename, FileShare.ReadWrite, false, null);
                if (CheckObjectExistsInDatabase(database2, "MODEL"))
                {
                    SpecifyModel = "MODEL";
                    FindBlockToGetSize(database2, out num, sourceFilename);
                    if (this.maxPoint > 0.0)
                    {
                        if (num <= 0) num = 1;
                        ProcessingObjectsInModelSpace_SourceDatabase(database2, num, out flag);
                        Point3d point3d;
                        if (firstFile)
                        {
                            double saveTargetInsertPoint = this.maxPoint - this.minPoint;
                            point3d = new Point3d(-this.minPoint, -this.minPoint, 0.0);
                            this.SaveTargetInsertPoint = saveTargetInsertPoint;
                        }
                        else
                        {
                            double num2 = this.SaveTargetInsertPoint + this.Gap - this.minPoint;
                            point3d = new Point3d(num2, -this.minPoint, 0.0);
                            this.SaveTargetInsertPoint = num2 + this.maxPoint;
                        }
                        Matrix3d matrix3d = Matrix3d.Displacement(point3d - Point3d.Origin);
                        targetDb.Insert(matrix3d, database2, true);
                    }
                    if (flag)
                    {
                        CreateNewDimStyle(targetDb, num);
                    }
                    string[] layerName = new string[] { "Hidden 1", "Hidden 2" };
                    SetLayerLinetype(targetDb, layerName, "HIDDEN");
                    if ((int)targetDb.Ltscale < num)
                    {
                        targetDb.Ltscale = (double)num;
                    }
                }
                else if (CheckObjectExistsInDatabase(database2, "LAYOUT"))
                {
                    SpecifyModel = "LAYOUT";
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceFilename);
                    if (fileNameWithoutExtension.Contains("_"))
                    {
                        int num3 = fileNameWithoutExtension.LastIndexOf('_');
                        if (num3 != -1)
                        {
                            string text = fileNameWithoutExtension.Substring(num3 + 1);
                            string sourceLayoutName = text + "_" + text;
                            ProcessingObjectsInPaperSpace_SourceDatabase(database2, targetDb, sourceLayoutName);
                        }
                    }
                }
                else
                {
                    return true; // Ignore empty file
                }
            }
            return true;
        }

        private void FindBlockToGetSize(Database db, out int scaleX, string sourceFilename)
        {
            scaleX = 0;
            using (Transaction transaction = db.TransactionManager.StartTransaction())
            {
                BlockTable blockTable = transaction.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                foreach (ObjectId objectId in (transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord))
                {
                    DBObject obj = transaction.GetObject(objectId, OpenMode.ForRead);
                    if (obj is BlockReference)
                    {
                        BlockReference blockReference = (BlockReference)obj;
                        if (blockReference.Name.Contains("Borders"))
                        {
                            scaleX = (int)blockReference.ScaleFactors.X;
                            Extents3d extents3d = new Extents3d();
                            Entity entity = transaction.GetObject(objectId, OpenMode.ForRead) as Entity;
                            extents3d.AddExtents(entity.GeometricExtents);
                            string text = extents3d.MinPoint.X.ToString();
                            string text2 = extents3d.MaxPoint.X.ToString();
                            if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(text2))
                            {
                                this.minPoint = ConvertStringToDouble(text);
                                this.maxPoint = ConvertStringToDouble(text2);
                            }
                            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceFilename);
                            ((BlockTableRecord)transaction.GetObject(blockTable[blockReference.Name], OpenMode.ForWrite)).Name = fileNameWithoutExtension;
                        }
                        else if (blockReference.Name.Contains("Title Blocks"))
                        {
                            string fileNameWithoutExtension2 = Path.GetFileNameWithoutExtension(sourceFilename);
                            try
                            {
                                int num = fileNameWithoutExtension2.LastIndexOf('_');
                                if (num != -1)
                                {
                                    string name = blockReference.Name + " " + fileNameWithoutExtension2.Substring(num + 1);
                                    ((BlockTableRecord)transaction.GetObject(blockTable[blockReference.Name], OpenMode.ForWrite)).Name = name;
                                }
                            }
                            catch { }
                        }
                    }
                }
                transaction.Commit();
            }
        }

        private double ConvertStringToDouble(string input)
        {
            double result = 0.0;
            try { double.TryParse(input, out result); } catch { }
            return result;
        }

        private static void SetLayerLinetype(Database db, string[] layerName, string linetypeName)
        {
            using (Transaction transaction = db.TransactionManager.StartTransaction())
            {
                LayerTable layerTable = transaction.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                foreach (string text in layerName)
                {
                    if (layerTable.Has(text))
                    {
                        LayerTableRecord layerTableRecord = transaction.GetObject(layerTable[text], OpenMode.ForWrite) as LayerTableRecord;
                        LinetypeTable linetypeTable = transaction.GetObject(db.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;
                        try
                        {
                            if (!linetypeTable.Has(linetypeName))
                            {
                                db.LoadLineTypeFile(linetypeName, "acad.lin");
                            }
                        }
                        catch { }
                        
                        if (linetypeTable.Has(linetypeName))
                        {
                            layerTableRecord.LinetypeObjectId = linetypeTable[linetypeName];
                        }
                    }
                }
                transaction.Commit();
            }
        }

        private static bool CheckObjectExistsInDatabase(Database db, string SpecifyModel)
        {
            try
            {
                using (Transaction transaction = db.TransactionManager.StartTransaction())
                {
                    ObjectId objectId = ObjectId.Null;
                    if (SpecifyModel == "MODEL")
                    {
                        objectId = SymbolUtilityServices.GetBlockModelSpaceId(db);
                    }
                    else if (SpecifyModel == "LAYOUT")
                    {
                        objectId = SymbolUtilityServices.GetBlockPaperSpaceId(db);
                    }
                    if (objectId == ObjectId.Null) return false;
                    
                    if ((transaction.GetObject(objectId, OpenMode.ForRead) as BlockTableRecord).Cast<ObjectId>().Count() == 0)
                    {
                        return false;
                    }
                    transaction.Commit();
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        private void ProcessingObjectsInModelSpace_SourceDatabase(Database db, int value, out bool dimExists)
        {
            dimExists = false;
            using (Transaction transaction = db.TransactionManager.StartTransaction())
            {
                BlockTable blockTable = (BlockTable)transaction.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord blockTableRecord = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForRead);
                if (blockTableRecord != null)
                {
                    foreach (ObjectId objectId in blockTableRecord)
                    {
                        Entity entity = transaction.GetObject(objectId, OpenMode.ForRead) as Entity;
                        if (entity != null && entity is Dimension)
                        {
                            dimExists = true;
                            Dimension dimension = (Dimension)entity;
                            entity.UpgradeOpen();
                            dimension.Dimscale = (double)value;
                            string styleName = string.Format("1-{0}", value);
                            CreateNewDimStyle(db, value);
                            ObjectId objectID_DimStyle = GetObjectID_DimStyle(db, styleName);
                            if (dimension.Dimalt)
                            {
                                int dimaltu = dimension.Dimaltu;
                                int dimaltd = dimension.Dimaltd;
                                double dimaltrnd = dimension.Dimaltrnd;
                                double dimaltf = dimension.Dimaltf;
                                if (objectID_DimStyle != ObjectId.Null)
                                {
                                    dimension.DimensionStyle = objectID_DimStyle;
                                }
                                dimension.Dimalt = true;
                                dimension.Dimaltu = dimaltu;
                                dimension.Dimaltd = dimaltd;
                                dimension.Dimaltrnd = dimaltrnd;
                                dimension.Dimaltf = dimaltf;
                                if (!string.IsNullOrEmpty(dimension.Dimapost))
                                {
                                    dimension.Dimapost = "\"";
                                }
                            }
                            else
                            {
                                double dimlfac = dimension.Dimlfac;
                                if (objectID_DimStyle != ObjectId.Null)
                                {
                                    dimension.DimensionStyle = objectID_DimStyle;
                                }
                                dimension.Dimlfac = dimlfac;
                            }
                            if (entity is DiametricDimension || entity is RadialDimension)
                            {
                                ObjectId objectID_Arrow = GetObjectID_Arrow(db, "DIMBLK2", "_CLOSED");
                                if (objectID_Arrow != ObjectId.Null)
                                {
                                    dimension.Dimblk2 = objectID_Arrow;
                                    dimension.Dimasz = 2.0;
                                }
                            }
                        }
                    }
                    transaction.Commit();
                }
            }
        }

        private void CreateNewDimStyle(Database db, int DimscaleValue)
        {
            string text = string.Format("1-{0}", DimscaleValue);
            using (Transaction transaction = db.TransactionManager.StartTransaction())
            {
                DimStyleTable dimStyleTable = (DimStyleTable)transaction.GetObject(db.DimStyleTableId, OpenMode.ForRead);
                ObjectId objectId = ObjectId.Null;
                if (!dimStyleTable.Has(text))
                {
                    dimStyleTable.UpgradeOpen();
                    DimStyleTableRecord dimStyleTableRecord = new DimStyleTableRecord();
                    SetDimStyleVariable(transaction, db, dimStyleTableRecord, text, DimscaleValue);
                    objectId = dimStyleTable.Add(dimStyleTableRecord);
                    transaction.AddNewlyCreatedDBObject(dimStyleTableRecord, true);
                }
                else
                {
                    objectId = dimStyleTable[text];
                }
                DimStyleTableRecord dimStyleTableRecord2 = (DimStyleTableRecord)transaction.GetObject(objectId, OpenMode.ForRead);
                if (dimStyleTableRecord2.ObjectId != db.Dimstyle)
                {
                    db.Dimstyle = dimStyleTableRecord2.ObjectId;
                    db.SetDimstyleData(dimStyleTableRecord2);
                }
                transaction.Commit();
            }
        }

        private void SetDimStyleVariable(Transaction tr, Database db, DimStyleTableRecord dim, string dimStyleName, int DimscaleValue)
        {
            dim.Dimclrd = Color.FromColorIndex(ColorMethod.ByAci, 195);
            ObjectId ojectID_LineStyle = GetOjectID_LineStyle(db, "BYLAYER");
            if (ojectID_LineStyle != ObjectId.Null)
            {
                dim.Dimltype = ojectID_LineStyle;
            }
            dim.Dimlwd = LineWeight.ByLineWeightDefault;
            dim.Dimdle = 0.5;
            dim.Dimdli = 0.5;
            dim.Dimsd1 = false;
            dim.Dimsd2 = false;
            dim.Dimclre = Color.FromColorIndex(ColorMethod.ByAci, 195);
            dim.Dimltex1 = dim.Dimltype;
            dim.Dimltex2 = dim.Dimltype;
            dim.Dimlwe = LineWeight.ByLineWeightDefault;
            dim.Dimse1 = false;
            dim.Dimse2 = false;
            dim.Dimexe = 0.5;
            dim.Dimexo = 1.0;
            dim.Dimsah = true;
            ObjectId objectID_Arrow = GetObjectID_Arrow(db, "DIMBLK1", "_OBLIQUE");
            if (objectID_Arrow != ObjectId.Null)
            {
                dim.Dimblk1 = objectID_Arrow;
                dim.Dimblk2 = objectID_Arrow;
            }
            dim.Dimldrblk = ObjectId.Null;
            dim.Dimasz = 0.5;
            dim.Dimcen = 0.0;
            dim.Dimgap = 3.75;
            dim.Dimarcsym = 0;
            dim.Dimjogang = 45.0;
            this.Standard_TextStyleID = GetObjectID_TextStyle(tr, db, "Standard");
            if (this.Standard_TextStyleID != ObjectId.Null)
            {
                dim.Dimtxsty = this.Standard_TextStyleID;
            }
            dim.Dimclrt = Color.FromColorIndex(ColorMethod.ByAci, 7);
            dim.Dimtfill = 0;
            dim.Dimtxt = 2.0;
            dim.Dimgap = 0.3;
            dim.Dimtad = 2;
            dim.Dimjust = 0;
            dim.Dimtxtdirection = false;
            dim.Dimtih = false;
            dim.Dimtoh = false;
            dim.Dimatfit = 3;
            dim.Dimsoxd = false;
            dim.Dimtmove = 1;
            dim.Dimscale = (double)DimscaleValue;
            dim.Dimupt = false;
            dim.Dimtofl = false;
            dim.Dimlunit = 2;
            dim.Dimdec = 1;
            dim.Dimfrac = 2;
            dim.Dimdsep = Convert.ToChar(".");
            dim.Dimrnd = 0.25;
            dim.Dimpost = "";
            dim.Dimlfac = 1.0;
            dim.Dimzin = 9;
            dim.Dimaunit = 0;
            dim.Dimadec = 1;
            dim.Dimazin = 3;
            dim.Dimalt = false;
            dim.Dimaltu = 7;
            dim.Dimaltd = 2;
            dim.Dimaltf = 25.4;
            dim.Dimaltrnd = 0.5;
            dim.Dimapost = "\"";
            dim.Dimaltz = 12;
            dim.Dimtol = false;
            dim.Dimtdec = 0;
            dim.Dimtp = 0.0;
            dim.Dimtm = 0.0;
            dim.Dimtfac = 2.0;
            dim.Dimtolj = 1;
            dim.Dimalttd = 0;
            dim.Dimalttz = 12;
            dim.Name = dimStyleName;
            dim.Dimblk = ObjectId.Null;
            dim.Dimfxlen = 0.18;
            dim.DimfxlenOn = false;
            dim.Dimlim = false;
            dim.Dimtfillclr = Color.FromColorIndex(ColorMethod.ByAci, 0);
            dim.Dimtix = false;
            dim.Dimtsz = 0.0;
            dim.Dimtvp = 0.0;
            dim.Dimtzin = 0;
        }

        private static ObjectId GetObjectID_DimStyle(Database db, string styleName)
        {
            ObjectId result = ObjectId.Null;
            using (Transaction transaction = db.TransactionManager.StartTransaction())
            {
                SymbolTable symbolTable = (SymbolTable)transaction.GetObject(db.DimStyleTableId, OpenMode.ForRead);
                if (symbolTable.Has(styleName))
                {
                    result = symbolTable[styleName];
                }
                transaction.Commit();
            }
            return result;
        }

        private static ObjectId GetOjectID_LineStyle(Database db, string LineStyleName)
        {
            ObjectId result = ObjectId.Null;
            using (Transaction transaction = db.TransactionManager.StartTransaction())
            {
                LinetypeTable linetypeTable = (LinetypeTable)transaction.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                if (linetypeTable.Has(LineStyleName))
                {
                    result = linetypeTable[LineStyleName];
                }
                transaction.Commit();
            }
            return result;
        }

        private static ObjectId GetObjectID_Arrow(Database db, string arrow, string newArrowName)
        {
            ObjectId result = ObjectId.Null;
            try
            {
                // Core console may not support Application.GetSystemVariable for UI variables, fallback gracefully
                using (Transaction transaction = db.TransactionManager.StartTransaction())
                {
                    BlockTable blockTable = (BlockTable)transaction.GetObject(db.BlockTableId, OpenMode.ForRead);
                    if (blockTable.Has(newArrowName))
                    {
                        result = blockTable[newArrowName];
                    }
                    transaction.Commit();
                }
            }
            catch { }
            return result;
        }

        private static ObjectId GetObjectID_TextStyle(Transaction tr, Database db, string TextStyleName)
        {
            ObjectId result = ObjectId.Null;
            TextStyleTable textStyleTable = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
            if (textStyleTable.Has(TextStyleName))
            {
                result = textStyleTable[TextStyleName];
            }
            return result;
        }

        private void CreateOrUpdateTextStyle(Database db, string textStyleName, string fontName)
        {
            using (Transaction transaction = db.TransactionManager.StartTransaction())
            {
                TextStyleTable textStyleTable = transaction.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
                if (textStyleTable.Has(textStyleName))
                {
                    (transaction.GetObject(textStyleTable[textStyleName], OpenMode.ForWrite) as TextStyleTableRecord).FileName = fontName;
                }
                else
                {
                    textStyleTable.UpgradeOpen();
                    TextStyleTableRecord textStyleTableRecord = new TextStyleTableRecord();
                    textStyleTableRecord.Name = textStyleName;
                    textStyleTableRecord.FileName = fontName;
                    textStyleTable.Add(textStyleTableRecord);
                    transaction.AddNewlyCreatedDBObject(textStyleTableRecord, true);
                }
                transaction.Commit();
            }
        }

        private void ProcessingObjectsInPaperSpace_SourceDatabase(Database sourceDb, Database targetDb, string sourceLayoutName)
        {
            using (Transaction transaction = targetDb.TransactionManager.StartTransaction())
            {
                try
                {
                    // Copy Layout Logic
                    LayoutManager layoutManager = LayoutManager.Current;
                    ObjectId objectId = layoutManager.CreateLayout(sourceLayoutName);
                    Layout layout = (Layout)transaction.GetObject(objectId, OpenMode.ForWrite);
                    
                    using (Transaction transaction2 = sourceDb.TransactionManager.StartTransaction())
                    {
                        if ((transaction2.GetObject(sourceDb.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary).Contains(sourceLayoutName))
                        {
                            ObjectId layoutId = (transaction2.GetObject(sourceDb.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary).GetAt(sourceLayoutName);
                            Layout layout2 = (Layout)transaction2.GetObject(layoutId, OpenMode.ForRead);
                            layout.CopyFrom(layout2);
                            ObjectIdCollection objectIdCollection = new ObjectIdCollection((transaction2.GetObject(layout2.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord).Cast<ObjectId>().ToArray<ObjectId>());
                            IdMapping idMapping = new IdMapping();
                            targetDb.WblockCloneObjects(objectIdCollection, layout.BlockTableRecordId, idMapping, DuplicateRecordCloning.Ignore, false);
                        }
                        transaction2.Commit();
                    }
                    transaction.Commit();
                }
                catch (System.Exception ex)
                {
                    Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nLỗi copy layout: " + ex.Message);
                    transaction.Abort();
                }
            }
        }
    }
}
