using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.AutoCAD.Interop;
using HNB_MyTools_Inventor.Properties;
using Inventor;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.WindowsAPICodePack.Dialogs;
using MyToolsExt;

namespace HNB_MyTools_Inventor
{
    // Token: 0x0200005D RID: 93
    public class frmDWGExport : Form
    {
        // Token: 0x06000902 RID: 2306 RVA: 0x00073B54 File Offset: 0x00072B54
        public frmDWGExport(Inventor.Application application)
        {
            this.invApp = application;
            this.InitializeComponent();
            if (!this.GetTheDWGTranslatorAddIn())
            {
                mBox.Error(this, "DWG translator not found\nKhông tìm thấy trình bổ trợ DWG");
                return;
            }
            this.oOptions = this.invApp.TransientObjects.CreateNameValueMap();
            this.oContext = this.invApp.TransientObjects.CreateTranslationContext();
            this.oContext.Type = IOMechanismEnum.kFileBrowseIOMechanism;
            this.oDataMedium = this.invApp.TransientObjects.CreateDataMedium();
            this.oDocument = this.invApp.ActiveDocument;
        }

        // Token: 0x06000903 RID: 2307 RVA: 0x00073BEC File Offset: 0x00072BEC
        private bool GetTheDWGTranslatorAddIn()
        {
            try
            {
                this.oDWGAddIn = (this.invApp.ApplicationAddIns.get_ItemById("{C24E3AC2-122E-11D5-8E91-0010B541CD80}") as TranslatorAddIn);
            }
            catch (Exception)
            {
                this.oDWGAddInLoadOK = false;
                return false;
            }
            if (!this.oDWGAddIn.Activated)
            {
                this.oDWGAddIn.Activate();
            }
            this.oDWGAddInLoadOK = true;
            return true;
        }

        // Token: 0x06000904 RID: 2308 RVA: 0x00073C5C File Offset: 0x00072C5C
        private void frmDWGExport_Load(object sender, EventArgs e)
        {
            if (this.invApp.ActiveDocumentType == Inventor.DocumentTypeEnum.kDrawingDocumentObject)
            {
                this.idwDoc = (Inventor.DrawingDocument)this.invApp.ActiveDocument;
                this.cbSpecifyExportStyle.SelectedIndex = 0;
                try
                {
                    Control control = this.txtSaveFileName;
                    if (frmDWGExport.<>o__10.<>p__0 == null)
                    {
                        frmDWGExport.<>o__10.<>p__0 = CallSite<Func<CallSite, object, string>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(frmDWGExport)));
                    }
                    control.Text = frmDWGExport.<>o__10.<>p__0.Target(frmDWGExport.<>o__10.<>p__0, this.idwDoc.PropertySets[4]["PTC code"].Value);
                    goto IL_10B;
                }
                catch
                {
                    goto IL_10B;
                }
            }
            if (this.invApp.ActiveDocumentType == Inventor.DocumentTypeEnum.kPartDocumentObject || this.invApp.ActiveDocumentType == Inventor.DocumentTypeEnum.kAssemblyDocumentObject)
            {
                this.gbOptions.Enabled = false;
                this.txtFileNameAfterExport.Enabled = false;
                this.txtGap.Enabled = false;
                this.cbSpecifyExportStyle.Enabled = false;
                this.chkDeleteFileAfterMerging.Enabled = false;
            }
            IL_10B:
            this.txtPathToSaveTheFile.Text = this.invApp.DesignProjectManager.ActiveDesignProject.WorkspacePath.ToString() + "\\CAD";
            this.lbStatus.Text = "Version: " + Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        // Token: 0x06000905 RID: 2309 RVA: 0x00073DDC File Offset: 0x00072DDC
        private void btn_Browser_Click(object sender, EventArgs e)
        {
            base.WindowState = FormWindowState.Minimized;
            string text = this.invApp.DesignProjectManager.ActiveDesignProject.WorkspacePath.ToString();
            CommonOpenFileDialog commonOpenFileDialog = new CommonOpenFileDialog();
            commonOpenFileDialog.IsFolderPicker = true;
            commonOpenFileDialog.InitialDirectory = text;
            if (commonOpenFileDialog.ShowDialog().ToString() == "Ok")
            {
                string fileName = commonOpenFileDialog.FileName;
                if (fileName.Contains("OldVersions"))
                {
                    return;
                }
                if (fileName == text)
                {
                    return;
                }
                this.txtPathToSaveTheFile.Text = commonOpenFileDialog.FileName;
            }
            base.WindowState = FormWindowState.Normal;
        }

        // Token: 0x06000906 RID: 2310 RVA: 0x00073E78 File Offset: 0x00072E78
        private void btnProceed_ButtonClick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.txtSaveFileName.Text))
            {
                base.ActiveControl = this.txtSaveFileName;
                return;
            }
            char[] specialCharacters = new char[]
            {
                '*',
                '\\',
                '/',
                ':',
                '?',
                '"',
                '<',
                '>',
                '|'
            };
            if (this.CheckSpecialCharacters(this.txtSaveFileName, "Item Code", specialCharacters))
            {
                return;
            }
            if (string.IsNullOrEmpty(this.txtPathToSaveTheFile.Text))
            {
                base.ActiveControl = this.txtPathToSaveTheFile;
                return;
            }
            char[] specialCharacters2 = new char[]
            {
                '*',
                '/',
                '?',
                '"',
                '<',
                '>',
                '|'
            };
            if (this.CheckSpecialCharacters(this.txtPathToSaveTheFile, "Save File", specialCharacters2))
            {
                return;
            }
            if (!Directory.Exists(this.txtPathToSaveTheFile.Text))
            {
                Directory.CreateDirectory(this.txtPathToSaveTheFile.Text);
            }
            else
            {
                int num = Directory.EnumerateFiles(this.txtPathToSaveTheFile.Text, "*.dwg").Count<string>();
                if (num > 0)
                {
                    if (mBox.Question(this, string.Format("{0} bản vẽ \".dwg\" được tìm thấy trong {1}.\n\nXoá các tệp này trước khi thực hiện lệnh?", num, this.txtPathToSaveTheFile.Text), false) != DialogResult.Yes)
                    {
                        return;
                    }
                    string[] files = (from f in Directory.GetFiles(this.txtPathToSaveTheFile.Text, "*.dwg")
                    orderby f
                    select f).ToArray<string>();
                    if (!this.DeleteDWGfile(files))
                    {
                        return;
                    }
                }
            }
            string text = Path.Combine(this.txtPathToSaveTheFile.Text, this.txtSaveFileName.Text + ".dwg");
            if (string.IsNullOrEmpty(text))
            {
                mBox.Warning(this, "Không có đường dẫn lưu file.");
                return;
            }
            if (System.IO.File.Exists(text))
            {
                if (mBox.Question(this, text + "\nTệp đã tồn tại, bạn có muốn thay thế?", false) != DialogResult.Yes)
                {
                    return;
                }
                try
                {
                    System.IO.File.Delete(text);
                }
                catch (Exception ex)
                {
                    mBox.Error(this, ex.Message);
                    return;
                }
            }
            if (this.oDWGAddInLoadOK)
            {
                this.PublishDWG(text);
            }
        }

        // Token: 0x06000907 RID: 2311 RVA: 0x0007405C File Offset: 0x0007305C
        private bool DeleteDWGfile(string[] files)
        {
            try
            {
                for (int i = 0; i < files.Length; i++)
                {
                    System.IO.File.Delete(files[i]);
                }
                return true;
            }
            catch (Exception ex)
            {
                mBox.Error(this, "Lỗi xoá tệp: " + ex.Message);
            }
            return false;
        }

        // Token: 0x06000908 RID: 2312 RVA: 0x000740B4 File Offset: 0x000730B4
        private bool RenameSheet(Inventor.Sheets oSheets)
        {
            int num = 0;
            try
            {
                for (int i = 1; i <= oSheets.Count; i++)
                {
                    num++;
                    oSheets[i].Name = num.ToString();
                }
                return true;
            }
            catch (Exception ex)
            {
                mBox.Error(this, ex.Message);
            }
            return false;
        }

        // Token: 0x06000909 RID: 2313 RVA: 0x00074118 File Offset: 0x00073118
        private bool CheckSpecialCharacters(System.Windows.Forms.TextBox textBox, string caption, char[] specialCharacters)
        {
            List<char> list = new List<char>();
            foreach (char c in textBox.Text)
            {
                if (specialCharacters.Contains(c))
                {
                    list.Add(c);
                }
            }
            if (list.Count > 0)
            {
                string str = string.Join<char>(", ", list);
                mBox.Warning(this, "Phát hiện ký tự đặc biệt: " + str + "\nKý này không được hỗ trợ trong việc đặt tên file");
                base.ActiveControl = textBox;
                for (int j = 0; j < textBox.Text.Length; j++)
                {
                    char item = textBox.Text[j];
                    if (list.Contains(item))
                    {
                        textBox.Select(j, 1);
                        break;
                    }
                }
                return true;
            }
            return false;
        }

        // Token: 0x0600090A RID: 2314 RVA: 0x000741D0 File Offset: 0x000731D0
        public void PublishDWG(string pathToSaveFile)
        {
            if (this.oDWGAddIn.get_HasSaveCopyAsOptions(this.invApp.ActiveDocument, this.oContext, this.oOptions))
            {
                if (this.oDocument.DocumentType == Inventor.DocumentTypeEnum.kPartDocumentObject || this.oDocument.DocumentType == Inventor.DocumentTypeEnum.kAssemblyDocumentObject)
                {
                    this.oOptions.set_Value("Solid", true);
                    this.oOptions.set_Value("Surface", true);
                    this.oOptions.set_Value("Sketch", true);
                    this.oOptions.set_Value("DwgVersion", 27);
                }
                else if (this.oDocument.DocumentType == Inventor.DocumentTypeEnum.kDrawingDocumentObject)
                {
                    string text = this.WriteToExportToDWGIniFile();
                    if (!System.IO.File.Exists(text))
                    {
                        mBox.Warning(this, "Không thể truy cập tệp cấu hình \"ExportToDWG.ini\"");
                        return;
                    }
                    this.oOptions.set_Value("Export_Acad_IniFile", text);
                }
            }
            Inventor.Transaction transaction = null;
            try
            {
                base.Enabled = false;
                transaction = this.invApp.TransactionManager.StartTransaction((Inventor._Document)this.oDocument, this.Text);
                if (this.invApp.ActiveDocumentType == Inventor.DocumentTypeEnum.kDrawingDocumentObject)
                {
                    if (this.idwDoc == null)
                    {
                        this.idwDoc = (this.invApp.ActiveDocument as Inventor.DrawingDocument);
                    }
                    if (this.CheckDrawingViewIsRasterView(this.idwDoc))
                    {
                        if (this.rbAllSheets.Checked)
                        {
                            if (!this.RenameSheet(this.idwDoc.Sheets))
                            {
                                transaction.Abort();
                                return;
                            }
                            this.oDataMedium.FileName = Path.Combine(this.txtPathToSaveTheFile.Text, this.txtFileNameAfterExport.Text + ".dwg");
                            this.oDWGAddIn.SaveCopyAs(this.oDocument, this.oContext, this.oOptions, this.oDataMedium);
                            if (Directory.EnumerateFiles(this.txtPathToSaveTheFile.Text, "*.dwg").Count<string>() > 1)
                            {
                                this.MergeAllFileFromFolder(this.txtPathToSaveTheFile.Text, pathToSaveFile);
                            }
                        }
                        else if (this.rbCurrrentSheet.Checked)
                        {
                            string fileToSave = Path.Combine(this.txtPathToSaveTheFile.Text, this.txtSaveFileName.Text + "_" + this.idwDoc.ActiveSheet.Name + ".dwg");
                            this.ExportCustomSheet(this.idwDoc.ActiveSheet, 1, fileToSave);
                        }
                        else if (this.rbCustomed.Checked)
                        {
                            if (string.IsNullOrEmpty(this.txtCustomed.Text))
                            {
                                base.ActiveControl = this.txtCustomed;
                                return;
                            }
                            List<int> list = new List<int>();
                            foreach (string text2 in this.txtCustomed.Text.Split(new char[]
                            {
                                ','
                            }))
                            {
                                if (!string.IsNullOrEmpty(text2))
                                {
                                    int num = ConvertHelper.ToInt(text2);
                                    if (num < 1 || num > this.idwDoc.Sheets.Count)
                                    {
                                        this.CheckCharacters(this.txtCustomed, text2);
                                        transaction.Abort();
                                        return;
                                    }
                                    if (!list.Contains(num))
                                    {
                                        list.Add(num);
                                    }
                                }
                            }
                            if (list.Count > 0)
                            {
                                foreach (int num2 in list)
                                {
                                    Inventor.Sheet sheet = this.idwDoc.Sheets[num2];
                                    if (sheet != null)
                                    {
                                        this.ExportCustomSheet(sheet, num2, "");
                                    }
                                }
                                if (Directory.EnumerateFiles(this.txtPathToSaveTheFile.Text, "*.dwg").Count<string>() > 1)
                                {
                                    this.MergeAllFileFromFolder(this.txtPathToSaveTheFile.Text, pathToSaveFile);
                                }
                            }
                        }
                        else if (this.rbFrom.Checked)
                        {
                            int num3 = ConvertHelper.ToInt(this.txtFrom.Text);
                            if (num3 < 1)
                            {
                                base.Enabled = true;
                                base.ActiveControl = this.txtFrom;
                                transaction.Abort();
                                return;
                            }
                            if (num3 > this.idwDoc.Sheets.Count)
                            {
                                transaction.Abort();
                                base.Enabled = true;
                                base.ActiveControl = this.txtFrom;
                                return;
                            }
                            int num4 = ConvertHelper.ToInt(this.txtTo.Text);
                            if (num4 < 1)
                            {
                                transaction.Abort();
                                base.Enabled = true;
                                base.ActiveControl = this.txtTo;
                                return;
                            }
                            if (num4 > this.idwDoc.Sheets.Count)
                            {
                                transaction.Abort();
                                base.Enabled = true;
                                base.ActiveControl = this.txtTo;
                                return;
                            }
                            if (num3 == num4 || num3 > num4)
                            {
                                transaction.Abort();
                                base.Enabled = true;
                                return;
                            }
                            for (int j = num3; j <= num4; j++)
                            {
                                string fileToSave2 = Path.Combine(this.txtPathToSaveTheFile.Text, string.Format("{0}_{1}.dwg", this.txtFileNameAfterExport.Text, j));
                                Inventor.Sheet sheet2 = this.idwDoc.Sheets[j];
                                if (sheet2 != null)
                                {
                                    this.ExportCustomSheet(sheet2, j, fileToSave2);
                                }
                            }
                            if (Directory.EnumerateFiles(this.txtPathToSaveTheFile.Text, "*.dwg").Count<string>() > 1)
                            {
                                this.MergeAllFileFromFolder(this.txtPathToSaveTheFile.Text, pathToSaveFile);
                            }
                        }
                    }
                }
                else if (this.invApp.ActiveDocumentType == Inventor.DocumentTypeEnum.kPartDocumentObject || this.invApp.ActiveDocumentType == Inventor.DocumentTypeEnum.kAssemblyDocumentObject)
                {
                    string text3 = Path.Combine(this.txtPathToSaveTheFile.Text, this.txtSaveFileName.Text + ".dwg");
                    this.oDataMedium.FileName = text3;
                    this.oDWGAddIn.SaveCopyAs(this.oDocument, this.oContext, this.oOptions, this.oDataMedium);
                    if (System.IO.File.Exists(text3))
                    {
                        Process.Start("explorer.exe", string.Format("/select,\"{0}\"", text3));
                    }
                }
            }
            catch (Exception ex)
            {
                mBox.Error(this, ex.Message);
            }
            finally
            {
                if (transaction != null)
                {
                    transaction.End();
                    Marshal.ReleaseComObject(transaction);
                    transaction = null;
                }
            }
            base.Enabled = true;
        }

        // Token: 0x0600090B RID: 2315 RVA: 0x00074864 File Offset: 0x00073864
        private bool CheckDrawingViewIsRasterView(Inventor.DrawingDocument _idwDoc)
        {
            try
            {
                ConcurrentBag<ValueTuple<Inventor.Sheet, Inventor.DrawingView>> rasterViews = new ConcurrentBag<ValueTuple<Inventor.Sheet, Inventor.DrawingView>>();
                Parallel.ForEach<Inventor.Sheet>(_idwDoc.Sheets.Cast<Inventor.Sheet>(), delegate(Inventor.Sheet sheet)
                {
                    if (sheet.DrawingViews == null)
                    {
                        return;
                    }
                    foreach (ValueTuple<Inventor.Sheet, Inventor.DrawingView> item in from Inventor.DrawingView v in sheet.DrawingViews
                    where v.IsRasterView
                    select new ValueTuple<Inventor.Sheet, Inventor.DrawingView>(sheet, v))
                    {
                        rasterViews.Add(item);
                    }
                });
                if (!rasterViews.Any<ValueTuple<Inventor.Sheet, Inventor.DrawingView>>())
                {
                    return true;
                }
                ValueTuple<Inventor.Sheet, Inventor.DrawingView> valueTuple = rasterViews.First<ValueTuple<Inventor.Sheet, Inventor.DrawingView>>();
                mBox.Information(this, "Định dạng được chỉ định trên chế độ xem \"" + valueTuple.Item2.Label.Text + "\"\nkhông được hỗ trợ khi đang ở trạng thái \"Raster View\".\nVui lòng chuyển đổi tất cả chế độ xem raster thành chế độ xem precise trước khi xuất.");
            }
            catch (Exception ex)
            {
                mBox.Error(this, "Lỗi kiểm tra Drawing View có đang ở chế độ \"Raster\".\nError message:\n" + ex.Message);
            }
            return false;
        }

        // Token: 0x0600090C RID: 2316 RVA: 0x00074914 File Offset: 0x00073914
        private string WriteToExportToDWGIniFile()
        {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(directoryName))
            {
                return string.Empty;
            }
            string text = this.invApp.SoftwareVersion.DisplayVersion.Substring(0, 4);
            string text2;
            if (this.rbAllSheets.Checked)
            {
                text2 = "Yes";
            }
            else
            {
                text2 = "No";
            }
            string text3 = "";
            if (this.cbSpecifyExportStyle.Text == "Model")
            {
                text3 = "Geometry";
            }
            else if (this.cbSpecifyExportStyle.Text == "Layout")
            {
                text3 = "Text";
            }
            string text4 = directoryName + "\\ExportToDWG.ini";
            string contents = string.Concat(new string[]
            {
                "\r\n[EXPORT SELECT OPTIONS]\r\nAUTOCAD VERSION=AutoCAD 2007\r\nCREATE AUTOCAD MECHANICAL=No\r\nUSE TRANSMITTAL=No\r\nUSE CUSTOMIZE=No\r\nCUSTOMIZE FILE=C:\\Users\\Public\\Documents\\Autodesk\\Inventor ",
                text,
                "\\Design Data\\DWG-DXF\\FlatPattern.xml\r\nCREATE LAYER GROUP=No\r\nPARTS ONLY=No\r\nREPLACE SPLINE=No\r\nCHORD TOLERANCE=0.001000\r\n[EXPORT PROPERTIES]\r\nSELECTED PROPERTIES=\r\n[EXPORT DESTINATION]\r\nSPACE=",
                this.cbSpecifyExportStyle.Text,
                "\r\nSCALING=",
                text3,
                "\r\nALL SHEETS=",
                text2,
                "\r\nMAPPING=MapsBest\r\nMODEL GEOMETRY ONLY=No\r\nEXPLODE DIMENSIONS=No\r\nSYMBOLS ARE BLOCKED=Yes\r\nAUTOCAD TEMPLATE=\r\nDESTINATION DXF=No\r\nUSE ACI FOR ENTITIES AND LAYERS=No\r\nALLOW RASTER VIEWS=No\r\nSHOW DESTINATION PAGE=Yes\r\nENABLE POSTPROCESS=Yes\r\n[EXPORT LINE TYPE & LINE SCALE]\r\nLINE TYPE FILE=C:\\Users\\Public\\Documents\\Autodesk\\Inventor ",
                text,
                "\\COMPATIBILITY\\Support\\invISO.lin\r\nContinuous=Continuous;0.\r\nDashed=DASHED;0.\r\nDashed Space=DASHED_SPACE;0.\r\nLong Dash Dotted=LONG_DASH_DOTTED;0.\r\nLong Dash Double Dot=LONG_DASH_DOUBLE_DOT;0.\r\nLong Dash Triple Dot=LONG_DASH_TRIPLE_DOT;0.\r\nDotted=DOTTED;0.\r\nChain=CHAIN;0.\r\nDouble Dash Chain=DOUBLE_DASH_CHAIN;0.\r\nDash Double Dot=DASH_DOUBLE_DOT;0.\r\nDash Dot=DASH_DOT;0.\r\nDouble Dash Dot=DOUBLE_DASH_DOT;0.\r\nDouble Dash Double Dot=DOUBLE_DASH_DOUBLE_DOT;0.\r\nDash Triple Dot=DASH_TRIPLE_DOT;0.\r\nDouble Dash Triple Dot=DOUBLE_DASH_TRIPLE_DOT;0.\r\n"
            });
            System.IO.File.WriteAllText(text4, contents);
            return text4;
        }

        // Token: 0x0600090D RID: 2317 RVA: 0x00074A30 File Offset: 0x00073A30
        private void ExportCustomSheet(Inventor.Sheet sheet, int sheetIndex, string fileToSave = "")
        {
            if (this.idwDoc.ActiveSheet != sheet)
            {
                sheet.Activate();
            }
            if (string.IsNullOrEmpty(fileToSave))
            {
                string str;
                if (sheet.Name.Contains(":"))
                {
                    str = sheet.Name.Replace(":", "_");
                }
                else
                {
                    str = string.Format("{0}_{1}", sheet.Name, sheetIndex);
                }
                fileToSave = Path.Combine(this.txtPathToSaveTheFile.Text, str + ".dwg");
            }
            this.oDataMedium.FileName = fileToSave;
            this.oDWGAddIn.SaveCopyAs(this.oDocument, this.oContext, this.oOptions, this.oDataMedium);
        }

        // Token: 0x0600090E RID: 2318 RVA: 0x00074AEC File Offset: 0x00073AEC
        private void CheckCharacters(System.Windows.Forms.TextBox textBox, string pattern)
        {
            base.ActiveControl = textBox;
            int num = textBox.Text.IndexOf(pattern);
            if (num != -1)
            {
                textBox.Select(num, pattern.Length);
            }
        }

        // Token: 0x0600090F RID: 2319 RVA: 0x00074B20 File Offset: 0x00073B20
        private void MergeAllFileFromFolder(string folderContainingDWG, string fileNameToSave)
        {
            if (!this.IsAutoCADRunning())
            {
                try
                {
                    Type typeFromProgID = Type.GetTypeFromProgID(frmDWGExport._autocadClassId, true);
                    this.cadApp = (AcadApplication)Activator.CreateInstance(typeFromProgID, true);
                }
                catch (Exception ex)
                {
                    mBox.Error(this, "Lỗi kết nối tới AutoCAD:\n" + ex.Message);
                    return;
                }
            }
            AcadDocument acadDocument = this.CreateDWGFile(fileNameToSave);
            if (acadDocument == null)
            {
                return;
            }
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.fileMergeDWGFiles = directoryName + "\\CombineDWGFiles.dll";
            if (!System.IO.File.Exists(this.fileMergeDWGFiles))
            {
                mBox.Warning(this, "Lỗi kết nối tới tệp lệnh \n\"CombineDWGFiles.dll\"");
                return;
            }
            string str = this.fileMergeDWGFiles.Replace("\\", "\\\\");
            string command = "(command \"NETLOAD\" \"" + str + "\") ";
            acadDocument.SendCommand(command);
            string text = this.txtPathToSaveTheFile.Text.Replace("\\", "\\\\");
            string text2 = text + "\\\\" + this.txtSaveFileName.Text + ".dwg";
            string text3 = this.txtGap.Text;
            string text4 = this.chkDeleteFileAfterMerging.Checked.ToString();
            string command2 = string.Concat(new string[]
            {
                "(command \"CombineDWGFiles\" \"",
                text,
                "\" \"",
                text2,
                "\" \"",
                text3,
                "\"  \"",
                text4,
                "\" \"Y\") "
            });
            acadDocument.SendCommand(command2);
        }

        // Token: 0x06000910 RID: 2320 RVA: 0x00074CA8 File Offset: 0x00073CA8
        private AcadDocument CreateDWGFile(string fileNameToSave)
        {
            AcadDocument acadDocument = null;
            try
            {
                this.cadApp = (AcadApplication)Marshal.GetActiveObject(frmDWGExport._autocadClassId);
                if (this.cadApp.Documents.Count == 0)
                {
                    object preferences = this.cadApp.Preferences;
                    if (frmDWGExport.<>o__25.<>p__3 == null)
                    {
                        frmDWGExport.<>o__25.<>p__3 = CallSite<Func<CallSite, object, string>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(frmDWGExport)));
                    }
                    Func<CallSite, object, string> target = frmDWGExport.<>o__25.<>p__3.Target;
                    CallSite <>p__ = frmDWGExport.<>o__25.<>p__3;
                    if (frmDWGExport.<>o__25.<>p__2 == null)
                    {
                        frmDWGExport.<>o__25.<>p__2 = CallSite<Func<CallSite, Type, object, string, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(CSharpBinderFlags.None, "Combine", null, typeof(frmDWGExport), new CSharpArgumentInfo[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.IsStaticType, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType | CSharpArgumentInfoFlags.Constant, null)
                        }));
                    }
                    Func<CallSite, Type, object, string, object> target2 = frmDWGExport.<>o__25.<>p__2.Target;
                    CallSite <>p__2 = frmDWGExport.<>o__25.<>p__2;
                    Type typeFromHandle = typeof(Path);
                    if (frmDWGExport.<>o__25.<>p__1 == null)
                    {
                        frmDWGExport.<>o__25.<>p__1 = CallSite<Func<CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, "TemplateDWGPath", typeof(frmDWGExport), new CSharpArgumentInfo[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
                    }
                    Func<CallSite, object, object> target3 = frmDWGExport.<>o__25.<>p__1.Target;
                    CallSite <>p__3 = frmDWGExport.<>o__25.<>p__1;
                    if (frmDWGExport.<>o__25.<>p__0 == null)
                    {
                        frmDWGExport.<>o__25.<>p__0 = CallSite<Func<CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, "Files", typeof(frmDWGExport), new CSharpArgumentInfo[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
                    }
                    string text = target(<>p__, target2(<>p__2, typeFromHandle, target3(<>p__3, frmDWGExport.<>o__25.<>p__0.Target(frmDWGExport.<>o__25.<>p__0, preferences)), "acad.dwt"));
                    if (!System.IO.File.Exists(text))
                    {
                        if (mBox.Question(this, text + "\nThe \"acad.dwt\" tập tin không được tìm thấy ở vị trí hiện tại.", false) != DialogResult.Yes)
                        {
                            return null;
                        }
                        if (frmDWGExport.<>o__25.<>p__7 == null)
                        {
                            frmDWGExport.<>o__25.<>p__7 = CallSite<Func<CallSite, object, string>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(frmDWGExport)));
                        }
                        Func<CallSite, object, string> target4 = frmDWGExport.<>o__25.<>p__7.Target;
                        CallSite <>p__4 = frmDWGExport.<>o__25.<>p__7;
                        if (frmDWGExport.<>o__25.<>p__6 == null)
                        {
                            frmDWGExport.<>o__25.<>p__6 = CallSite<Func<CallSite, frmDWGExport, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(CSharpBinderFlags.InvokeSimpleName, "ShowDialogOpenFile", null, typeof(frmDWGExport), new CSharpArgumentInfo[]
                            {
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null),
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                            }));
                        }
                        Func<CallSite, frmDWGExport, object, object> target5 = frmDWGExport.<>o__25.<>p__6.Target;
                        CallSite <>p__5 = frmDWGExport.<>o__25.<>p__6;
                        if (frmDWGExport.<>o__25.<>p__5 == null)
                        {
                            frmDWGExport.<>o__25.<>p__5 = CallSite<Func<CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, "TemplateDWGPath", typeof(frmDWGExport), new CSharpArgumentInfo[]
                            {
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                            }));
                        }
                        Func<CallSite, object, object> target6 = frmDWGExport.<>o__25.<>p__5.Target;
                        CallSite <>p__6 = frmDWGExport.<>o__25.<>p__5;
                        if (frmDWGExport.<>o__25.<>p__4 == null)
                        {
                            frmDWGExport.<>o__25.<>p__4 = CallSite<Func<CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, "Files", typeof(frmDWGExport), new CSharpArgumentInfo[]
                            {
                                CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                            }));
                        }
                        text = target4(<>p__4, target5(<>p__5, this, target6(<>p__6, frmDWGExport.<>o__25.<>p__4.Target(frmDWGExport.<>o__25.<>p__4, preferences))));
                        if (System.IO.File.Exists(text))
                        {
                            return null;
                        }
                    }
                    this.cadApp.Documents.Add(text);
                }
                if (System.IO.File.Exists(fileNameToSave) && frmDWGExport.IsFileInUse(fileNameToSave))
                {
                    string fileName = Path.GetFileName(fileNameToSave);
                    mBox.Warning(this, "Bản vẽ " + fileName + " đang được sử dụng, không thể nối tệp");
                    return null;
                }
                acadDocument = this.cadApp.ActiveDocument;
                if (acadDocument == null)
                {
                    mBox.Warning(this, "Lỗi kết nối tới bản vẽ đang được kích hoạt");
                    return null;
                }
            }
            catch (Exception)
            {
                mBox.Warning(this, "Đã có lỗi xảy ra khi kết nối tới AutoCAD");
                return null;
            }
            return acadDocument;
        }

        // Token: 0x06000911 RID: 2321 RVA: 0x00075028 File Offset: 0x00074028
        private bool IsAutoCADRunning()
        {
            return Process.GetProcessesByName("acad").Length != 0;
        }

        // Token: 0x06000912 RID: 2322 RVA: 0x00075038 File Offset: 0x00074038
        private string ShowDialogOpenFile(string InitialFolder)
        {
            string result = null;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false;
            openFileDialog.CheckFileExists = true;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.InitialDirectory = InitialFolder;
            openFileDialog.Title = "Open Drawing File";
            openFileDialog.Filter = "Drawing Template (*.dwt) |*.dwt";
            if (openFileDialog.ShowDialog() == DialogResult.OK && openFileDialog.FileName.EndsWith(".dwt"))
            {
                result = openFileDialog.FileName;
            }
            return result;
        }

        // Token: 0x06000913 RID: 2323 RVA: 0x000750A4 File Offset: 0x000740A4
        private static bool IsFileInUse(string filePath)
        {
            bool result;
            try
            {
                using (new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    result = false;
                }
            }
            catch (IOException)
            {
                result = true;
            }
            return result;
        }

        // Token: 0x06000914 RID: 2324 RVA: 0x000750EC File Offset: 0x000740EC
        private void picStatus_ButtonClick(object sender, EventArgs e)
        {
            new frmAbout().ShowDialog();
        }

        // Token: 0x06000915 RID: 2325 RVA: 0x000750F9 File Offset: 0x000740F9
        private void textBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        // Token: 0x06000916 RID: 2326 RVA: 0x0007511C File Offset: 0x0007411C
        private void rbAllSheets_CheckedChanged(object sender, EventArgs e)
        {
            if (this.rbAllSheets.Checked)
            {
                this.txtCustomed.Enabled = (this.txtFrom.Enabled = (this.txtTo.Enabled = false));
            }
        }

        // Token: 0x06000917 RID: 2327 RVA: 0x00075160 File Offset: 0x00074160
        private void rbCurrrentSheet_CheckedChanged(object sender, EventArgs e)
        {
            if (this.rbCurrrentSheet.Checked)
            {
                this.txtCustomed.Enabled = (this.txtFrom.Enabled = (this.txtTo.Enabled = false));
            }
        }

        // Token: 0x06000918 RID: 2328 RVA: 0x000751A4 File Offset: 0x000741A4
        private void rbCustomed_CheckedChanged(object sender, EventArgs e)
        {
            if (this.rbCustomed.Checked)
            {
                this.txtCustomed.Enabled = true;
                this.txtFrom.Enabled = (this.txtTo.Enabled = false);
            }
        }

        // Token: 0x06000919 RID: 2329 RVA: 0x000751E4 File Offset: 0x000741E4
        private void rbFrom_CheckedChanged(object sender, EventArgs e)
        {
            this.txtCustomed.Enabled = false;
            this.txtFrom.Enabled = (this.txtTo.Enabled = true);
        }

        // Token: 0x0600091A RID: 2330 RVA: 0x00075217 File Offset: 0x00074217
        private void cbSpecifyExportStyle_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.cbSpecifyExportStyle.SelectedIndex == 0)
            {
                this.txtGap.Enabled = true;
                return;
            }
            this.txtGap.Enabled = false;
        }

        // Token: 0x0600091B RID: 2331 RVA: 0x00075240 File Offset: 0x00074240
        private void txtCustomed_KeyPress(object sender, KeyPressEventArgs e)
        {
            System.Windows.Forms.TextBox textBox = sender as System.Windows.Forms.TextBox;
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != ',')
            {
                e.Handled = true;
            }
            if (e.KeyChar == ',' && textBox != null && textBox.Text.Contains(","))
            {
                e.Handled = true;
            }
            if (e.KeyChar == ',' && textBox != null && textBox.SelectionStart != 0)
            {
                e.Handled = false;
            }
        }

        // Token: 0x0600091C RID: 2332 RVA: 0x000752C1 File Offset: 0x000742C1
        private void btnOptions_ButtonClick(object sender, EventArgs e)
        {
            if (this.oDWGAddIn != null)
            {
                this.oDWGAddIn.ShowSaveCopyAsOptions(this.invApp.ActiveDocument, this.oContext, this.oOptions);
            }
        }

        // Token: 0x0600091D RID: 2333 RVA: 0x000752ED File Offset: 0x000742ED
        protected override void Dispose(bool disposing)
        {
            if (disposing && this.components != null)
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        // Token: 0x0600091E RID: 2334 RVA: 0x0007530C File Offset: 0x0007430C
        private void InitializeComponent()
        {
            this.components = new Container();
            ComponentResourceManager componentResourceManager = new ComponentResourceManager(typeof(frmDWGExport));
            this.gbOutput = new GroupBox();
            this.cbSpecifyExportStyle = new ComboBox();
            this.chkDeleteFileAfterMerging = new CheckBox();
            this.tableLayoutPanel1 = new TableLayoutPanel();
            this.panel1 = new Panel();
            this.txtFileNameAfterExport = new System.Windows.Forms.TextBox();
            this.label2 = new Label();
            this.panel2 = new Panel();
            this.txtGap = new System.Windows.Forms.TextBox();
            this.label4 = new Label();
            this.panel3 = new Panel();
            this.txtSaveFileName = new System.Windows.Forms.TextBox();
            this.label3 = new Label();
            this.label1 = new Label();
            this.txtPathToSaveTheFile = new System.Windows.Forms.TextBox();
            this.btn_browser = new Button();
            this.panel_StatusBottom = new Panel();
            this.statusStrip1 = new StatusStrip();
            this.picStatus = new ToolStripSplitButton();
            this.lbStatus = new ToolStripStatusLabel();
            this.btnExport = new ToolStripSplitButton();
            this.btnOptions = new ToolStripSplitButton();
            this.toolTip1 = new ToolTip(this.components);
            this.rbCustomed = new RadioButton();
            this.rbAllSheets = new RadioButton();
            this.rbCurrrentSheet = new RadioButton();
            this.rbFrom = new RadioButton();
            this.gbOptions = new GroupBox();
            this.label5 = new Label();
            this.txtTo = new System.Windows.Forms.TextBox();
            this.txtCustomed = new System.Windows.Forms.TextBox();
            this.txtFrom = new System.Windows.Forms.TextBox();
            this.gbOutput.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel_StatusBottom.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.gbOptions.SuspendLayout();
            base.SuspendLayout();
            this.gbOutput.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            this.gbOutput.Controls.Add(this.cbSpecifyExportStyle);
            this.gbOutput.Controls.Add(this.chkDeleteFileAfterMerging);
            this.gbOutput.Controls.Add(this.tableLayoutPanel1);
            this.gbOutput.Controls.Add(this.label1);
            this.gbOutput.Controls.Add(this.txtPathToSaveTheFile);
            this.gbOutput.Controls.Add(this.btn_browser);
            this.gbOutput.Location = new System.Drawing.Point(148, 8);
            this.gbOutput.Name = "gbOutput";
            this.gbOutput.Size = new Size(385, 190);
            this.gbOutput.TabIndex = 11;
            this.gbOutput.TabStop = false;
            this.gbOutput.Text = "Output";
            this.cbSpecifyExportStyle.Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            this.cbSpecifyExportStyle.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cbSpecifyExportStyle.FormattingEnabled = true;
            this.cbSpecifyExportStyle.Items.AddRange(new object[]
            {
                "Model",
                "Layout"
            });
            this.cbSpecifyExportStyle.Location = new System.Drawing.Point(9, 78);
            this.cbSpecifyExportStyle.Name = "cbSpecifyExportStyle";
            this.cbSpecifyExportStyle.Size = new Size(367, 24);
            this.cbSpecifyExportStyle.TabIndex = 217;
            this.cbSpecifyExportStyle.SelectedIndexChanged += this.cbSpecifyExportStyle_SelectedIndexChanged;
            this.chkDeleteFileAfterMerging.AutoSize = true;
            this.chkDeleteFileAfterMerging.Location = new System.Drawing.Point(9, 159);
            this.chkDeleteFileAfterMerging.Name = "chkDeleteFileAfterMerging";
            this.chkDeleteFileAfterMerging.Size = new Size(245, 20);
            this.chkDeleteFileAfterMerging.TabIndex = 4;
            this.chkDeleteFileAfterMerging.Text = "Delete the drawing file after merging?";
            this.toolTip1.SetToolTip(this.chkDeleteFileAfterMerging, "Xóa file bản vẽ sau khi gộp");
            this.chkDeleteFileAfterMerging.UseVisualStyleBackColor = true;
            this.tableLayoutPanel1.Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 81.18081f));
            this.tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55f));
            this.tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 247f));
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.panel2, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.panel3, 2, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 18);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            this.tableLayoutPanel1.Size = new Size(380, 59);
            this.tableLayoutPanel1.TabIndex = 216;
            this.panel1.Controls.Add(this.txtFileNameAfterExport);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Dock = DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new Size(72, 53);
            this.panel1.TabIndex = 0;
            this.txtFileNameAfterExport.Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            this.txtFileNameAfterExport.BackColor = SystemColors.Window;
            this.txtFileNameAfterExport.Enabled = false;
            this.txtFileNameAfterExport.Location = new System.Drawing.Point(4, 25);
            this.txtFileNameAfterExport.Margin = new Padding(4);
            this.txtFileNameAfterExport.Name = "txtFileNameAfterExport";
            this.txtFileNameAfterExport.ReadOnly = true;
            this.txtFileNameAfterExport.Size = new Size(67, 22);
            this.txtFileNameAfterExport.TabIndex = 0;
            this.txtFileNameAfterExport.Text = "Sheet";
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(2, 4);
            this.label2.Name = "label2";
            this.label2.Size = new Size(66, 16);
            this.label2.TabIndex = 2;
            this.label2.Text = "File name";
            this.toolTip1.SetToolTip(this.label2, "Tên tệp sau khi export");
            this.panel2.Controls.Add(this.txtGap);
            this.panel2.Controls.Add(this.label4);
            this.panel2.Dock = DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(81, 3);
            this.panel2.Name = "panel2";
            this.panel2.Size = new Size(49, 53);
            this.panel2.TabIndex = 1;
            this.txtGap.Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            this.txtGap.BackColor = SystemColors.Window;
            this.txtGap.Location = new System.Drawing.Point(2, 25);
            this.txtGap.Name = "txtGap";
            this.txtGap.Size = new Size(46, 22);
            this.txtGap.TabIndex = 1;
            this.txtGap.Text = "150";
            this.txtGap.KeyPress += this.textBox_KeyPress;
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(1, 4);
            this.label4.Name = "label4";
            this.label4.Size = new Size(33, 16);
            this.label4.TabIndex = 2;
            this.label4.Text = "Gap";
            this.toolTip1.SetToolTip(this.label4, "Khoảng cách giữa các trang bản vẽ trong Model");
            this.panel3.Controls.Add(this.txtSaveFileName);
            this.panel3.Controls.Add(this.label3);
            this.panel3.Dock = DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(136, 3);
            this.panel3.Name = "panel3";
            this.panel3.Size = new Size(241, 53);
            this.panel3.TabIndex = 2;
            this.txtSaveFileName.Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            this.txtSaveFileName.Location = new System.Drawing.Point(1, 25);
            this.txtSaveFileName.Margin = new Padding(4);
            this.txtSaveFileName.Name = "txtSaveFileName";
            this.txtSaveFileName.Size = new Size(236, 22);
            this.txtSaveFileName.TabIndex = 0;
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(0, 4);
            this.label3.Name = "label3";
            this.label3.Size = new Size(96, 16);
            this.label3.TabIndex = 2;
            this.label3.Text = "Save file name";
            this.toolTip1.SetToolTip(this.label3, "Tên tệp sau khi đã gộp");
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 110);
            this.label1.Name = "label1";
            this.label1.Size = new Size(122, 16);
            this.label1.TabIndex = 2;
            this.label1.Text = "Path to save the file";
            this.toolTip1.SetToolTip(this.label1, "Đường dẫn để lưu tệp");
            this.txtPathToSaveTheFile.Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            this.txtPathToSaveTheFile.BackColor = SystemColors.Window;
            this.txtPathToSaveTheFile.Location = new System.Drawing.Point(9, 130);
            this.txtPathToSaveTheFile.Margin = new Padding(4);
            this.txtPathToSaveTheFile.Name = "txtPathToSaveTheFile";
            this.txtPathToSaveTheFile.ReadOnly = true;
            this.txtPathToSaveTheFile.Size = new Size(278, 22);
            this.txtPathToSaveTheFile.TabIndex = 0;
            this.btn_browser.Anchor = (AnchorStyles.Top | AnchorStyles.Right);
            this.btn_browser.Font = new Font("Microsoft Sans Serif", 9.75f, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.btn_browser.Image = Resources.Folder_Checked;
            this.btn_browser.ImageAlign = ContentAlignment.MiddleRight;
            this.btn_browser.Location = new System.Drawing.Point(294, 127);
            this.btn_browser.Margin = new Padding(5, 4, 5, 4);
            this.btn_browser.Name = "btn_browser";
            this.btn_browser.Size = new Size(82, 28);
            this.btn_browser.TabIndex = 1;
            this.btn_browser.Text = "Browser";
            this.btn_browser.TextAlign = ContentAlignment.MiddleLeft;
            this.toolTip1.SetToolTip(this.btn_browser, "Chọn đường dẫn để lưu tệp");
            this.btn_browser.UseVisualStyleBackColor = true;
            this.btn_browser.Click += this.btn_Browser_Click;
            this.panel_StatusBottom.Controls.Add(this.statusStrip1);
            this.panel_StatusBottom.Dock = DockStyle.Bottom;
            this.panel_StatusBottom.Location = new System.Drawing.Point(0, 208);
            this.panel_StatusBottom.Name = "panel_StatusBottom";
            this.panel_StatusBottom.Size = new Size(544, 28);
            this.panel_StatusBottom.TabIndex = 215;
            this.statusStrip1.Dock = DockStyle.Fill;
            this.statusStrip1.Font = new Font("Microsoft Sans Serif", 9.75f);
            this.statusStrip1.Items.AddRange(new ToolStripItem[]
            {
                this.picStatus,
                this.lbStatus,
                this.btnExport,
                this.btnOptions
            });
            this.statusStrip1.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
            this.statusStrip1.Location = new System.Drawing.Point(0, 0);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new Size(544, 28);
            this.statusStrip1.TabIndex = 0;
            this.statusStrip1.Text = "statusStrip1";
            this.picStatus.DisplayStyle = ToolStripItemDisplayStyle.Image;
            this.picStatus.DropDownButtonWidth = 0;
            this.picStatus.Image = Resources.Information;
            this.picStatus.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.picStatus.Margin = new Padding(5, 2, 0, 0);
            this.picStatus.Name = "picStatus";
            this.picStatus.Size = new Size(21, 26);
            this.picStatus.Text = "toolStripSplitButton1";
            this.picStatus.ButtonClick += this.picStatus_ButtonClick;
            this.lbStatus.BackColor = System.Drawing.Color.Transparent;
            this.lbStatus.Enabled = false;
            this.lbStatus.ForeColor = SystemColors.ControlDark;
            this.lbStatus.Margin = new Padding(2, 2, 0, 2);
            this.lbStatus.Name = "lbStatus";
            this.lbStatus.Size = new Size(0, 24);
            this.btnExport.Alignment = ToolStripItemAlignment.Right;
            this.btnExport.AutoToolTip = false;
            this.btnExport.BackColor = System.Drawing.Color.Transparent;
            this.btnExport.DropDownButtonWidth = 0;
            this.btnExport.Font = new Font("SansSerif", 11f);
            this.btnExport.Image = Resources.btn_Apply_16;
            this.btnExport.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnExport.Margin = new Padding(0, 0, 5, 0);
            this.btnExport.MergeIndex = 0;
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new Size(72, 28);
            this.btnExport.Text = "Export";
            this.btnExport.TextAlign = ContentAlignment.MiddleRight;
            this.btnExport.ButtonClick += this.btnProceed_ButtonClick;
            this.btnOptions.Alignment = ToolStripItemAlignment.Right;
            this.btnOptions.AutoToolTip = false;
            this.btnOptions.BackColor = System.Drawing.Color.Transparent;
            this.btnOptions.DropDownButtonWidth = 0;
            this.btnOptions.Font = new Font("SansSerif", 11f);
            this.btnOptions.Image = (Image)componentResourceManager.GetObject("btnOptions.Image");
            this.btnOptions.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnOptions.Margin = new Padding(0, 0, 5, 0);
            this.btnOptions.MergeIndex = 0;
            this.btnOptions.Name = "btnOptions";
            this.btnOptions.Size = new Size(80, 28);
            this.btnOptions.Text = "Options";
            this.btnOptions.TextAlign = ContentAlignment.MiddleRight;
            this.btnOptions.ButtonClick += this.btnOptions_ButtonClick;
            this.toolTip1.AutomaticDelay = 600;
            this.toolTip1.AutoPopDelay = 6000;
            this.toolTip1.InitialDelay = 600;
            this.toolTip1.IsBalloon = true;
            this.toolTip1.ReshowDelay = 500;
            this.toolTip1.ToolTipIcon = ToolTipIcon.Info;
            this.toolTip1.ToolTipTitle = "I N F O M A T I O N";
            this.rbCustomed.AutoSize = true;
            this.rbCustomed.Location = new System.Drawing.Point(8, 76);
            this.rbCustomed.Name = "rbCustomed";
            this.rbCustomed.Size = new Size(86, 20);
            this.rbCustomed.TabIndex = 2;
            this.rbCustomed.Text = "Customed";
            this.toolTip1.SetToolTip(this.rbCustomed, "Tuỳ chọn xuất các trang bản vẽ (1,3,5..)");
            this.rbCustomed.UseVisualStyleBackColor = true;
            this.rbCustomed.CheckedChanged += this.rbCustomed_CheckedChanged;
            this.rbAllSheets.AutoSize = true;
            this.rbAllSheets.Checked = true;
            this.rbAllSheets.Location = new System.Drawing.Point(8, 20);
            this.rbAllSheets.Name = "rbAllSheets";
            this.rbAllSheets.Size = new Size(83, 20);
            this.rbAllSheets.TabIndex = 1;
            this.rbAllSheets.TabStop = true;
            this.rbAllSheets.Text = "All sheets";
            this.toolTip1.SetToolTip(this.rbAllSheets, "Xuất tất cả các trang bản vẽ");
            this.rbAllSheets.UseVisualStyleBackColor = true;
            this.rbAllSheets.CheckedChanged += this.rbAllSheets_CheckedChanged;
            this.rbCurrrentSheet.AutoSize = true;
            this.rbCurrrentSheet.Location = new System.Drawing.Point(8, 48);
            this.rbCurrrentSheet.Name = "rbCurrrentSheet";
            this.rbCurrrentSheet.Size = new Size(103, 20);
            this.rbCurrrentSheet.TabIndex = 0;
            this.rbCurrrentSheet.Text = "Current sheet";
            this.toolTip1.SetToolTip(this.rbCurrrentSheet, "Xuất trang bản vẽ đang được kích hoạt");
            this.rbCurrrentSheet.UseVisualStyleBackColor = true;
            this.rbCurrrentSheet.CheckedChanged += this.rbCurrrentSheet_CheckedChanged;
            this.rbFrom.AutoSize = true;
            this.rbFrom.Location = new System.Drawing.Point(8, 130);
            this.rbFrom.Name = "rbFrom";
            this.rbFrom.Size = new Size(59, 20);
            this.rbFrom.TabIndex = 2;
            this.rbFrom.Text = "From:";
            this.toolTip1.SetToolTip(this.rbFrom, "Xuất bản vẽ từ");
            this.rbFrom.UseVisualStyleBackColor = true;
            this.rbFrom.CheckedChanged += this.rbFrom_CheckedChanged;
            this.gbOptions.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left);
            this.gbOptions.BackColor = System.Drawing.Color.Transparent;
            this.gbOptions.Controls.Add(this.label5);
            this.gbOptions.Controls.Add(this.txtTo);
            this.gbOptions.Controls.Add(this.txtCustomed);
            this.gbOptions.Controls.Add(this.txtFrom);
            this.gbOptions.Controls.Add(this.rbFrom);
            this.gbOptions.Controls.Add(this.rbCustomed);
            this.gbOptions.Controls.Add(this.rbAllSheets);
            this.gbOptions.Controls.Add(this.rbCurrrentSheet);
            this.gbOptions.Font = new Font("Microsoft Sans Serif", 9.75f, FontStyle.Regular, GraphicsUnit.Point, 163);
            this.gbOptions.Location = new System.Drawing.Point(12, 8);
            this.gbOptions.Name = "gbOptions";
            this.gbOptions.Size = new Size(130, 190);
            this.gbOptions.TabIndex = 216;
            this.gbOptions.TabStop = false;
            this.gbOptions.Text = "Options";
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(68, 132);
            this.label5.Name = "label5";
            this.label5.Size = new Size(27, 16);
            this.label5.TabIndex = 2;
            this.label5.Text = "To:";
            this.txtTo.Enabled = false;
            this.txtTo.Location = new System.Drawing.Point(68, 153);
            this.txtTo.Name = "txtTo";
            this.txtTo.Size = new Size(54, 22);
            this.txtTo.TabIndex = 4;
            this.txtTo.Text = "4";
            this.txtTo.TextAlign = HorizontalAlignment.Center;
            this.txtTo.KeyPress += this.textBox_KeyPress;
            this.txtCustomed.Enabled = false;
            this.txtCustomed.Location = new System.Drawing.Point(9, 100);
            this.txtCustomed.Name = "txtCustomed";
            this.txtCustomed.Size = new Size(113, 22);
            this.txtCustomed.TabIndex = 3;
            this.txtCustomed.Text = "1,3,5";
            this.txtCustomed.KeyPress += this.txtCustomed_KeyPress;
            this.txtFrom.Enabled = false;
            this.txtFrom.Location = new System.Drawing.Point(9, 153);
            this.txtFrom.Name = "txtFrom";
            this.txtFrom.Size = new Size(54, 22);
            this.txtFrom.TabIndex = 3;
            this.txtFrom.Text = "1";
            this.txtFrom.TextAlign = HorizontalAlignment.Center;
            this.txtFrom.KeyPress += this.textBox_KeyPress;
            base.AutoScaleDimensions = new SizeF(8f, 16f);
            base.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = SystemColors.Window;
            base.ClientSize = new Size(544, 236);
            base.Controls.Add(this.gbOptions);
            base.Controls.Add(this.gbOutput);
            base.Controls.Add(this.panel_StatusBottom);
            this.Font = new Font("Microsoft Sans Serif", 9.75f, FontStyle.Regular, GraphicsUnit.Point, 0);
            base.FormBorderStyle = FormBorderStyle.FixedDialog;
            base.Icon = (Icon)componentResourceManager.GetObject("$this.Icon");
            base.Margin = new Padding(4);
            base.MaximizeBox = false;
            base.MinimizeBox = false;
            this.MinimumSize = new Size(560, 275);
            base.Name = "frmDWGExport";
            base.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Export to DWG";
            base.Load += this.frmDWGExport_Load;
            this.gbOutput.ResumeLayout(false);
            this.gbOutput.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.panel_StatusBottom.ResumeLayout(false);
            this.panel_StatusBottom.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.gbOptions.ResumeLayout(false);
            this.gbOptions.PerformLayout();
            base.ResumeLayout(false);
        }

        // Token: 0x0400073E RID: 1854
        private Inventor.Application invApp;

        // Token: 0x0400073F RID: 1855
        private Inventor.Document oDocument;

        // Token: 0x04000740 RID: 1856
        private Inventor.DrawingDocument idwDoc;

        // Token: 0x04000741 RID: 1857
        private TranslatorAddIn oDWGAddIn;

        // Token: 0x04000742 RID: 1858
        private TranslationContext oContext;

        // Token: 0x04000743 RID: 1859
        private NameValueMap oOptions;

        // Token: 0x04000744 RID: 1860
        private DataMedium oDataMedium;

        // Token: 0x04000745 RID: 1861
        private bool oDWGAddInLoadOK;

        // Token: 0x04000746 RID: 1862
        private static string _autocadClassId = "AutoCAD.Application";

        // Token: 0x04000747 RID: 1863
        private AcadApplication cadApp;

        // Token: 0x04000748 RID: 1864
        private string fileMergeDWGFiles;

        // Token: 0x04000749 RID: 1865
        private IContainer components;

        // Token: 0x0400074A RID: 1866
        private System.Windows.Forms.TextBox txtPathToSaveTheFile;

        // Token: 0x0400074B RID: 1867
        private Button btn_browser;

        // Token: 0x0400074C RID: 1868
        private Panel panel_StatusBottom;

        // Token: 0x0400074D RID: 1869
        private StatusStrip statusStrip1;

        // Token: 0x0400074E RID: 1870
        private ToolStripSplitButton picStatus;

        // Token: 0x0400074F RID: 1871
        private ToolStripStatusLabel lbStatus;

        // Token: 0x04000750 RID: 1872
        private GroupBox gbOutput;

        // Token: 0x04000751 RID: 1873
        private System.Windows.Forms.TextBox txtFileNameAfterExport;

        // Token: 0x04000752 RID: 1874
        private Label label2;

        // Token: 0x04000753 RID: 1875
        private Label label1;

        // Token: 0x04000754 RID: 1876
        private Label label3;

        // Token: 0x04000755 RID: 1877
        private System.Windows.Forms.TextBox txtSaveFileName;

        // Token: 0x04000756 RID: 1878
        private ToolStripSplitButton btnExport;

        // Token: 0x04000757 RID: 1879
        private ToolTip toolTip1;

        // Token: 0x04000758 RID: 1880
        private System.Windows.Forms.TextBox txtGap;

        // Token: 0x04000759 RID: 1881
        private TableLayoutPanel tableLayoutPanel1;

        // Token: 0x0400075A RID: 1882
        private CheckBox chkDeleteFileAfterMerging;

        // Token: 0x0400075B RID: 1883
        private Label label4;

        // Token: 0x0400075C RID: 1884
        private Panel panel1;

        // Token: 0x0400075D RID: 1885
        private Panel panel2;

        // Token: 0x0400075E RID: 1886
        private Panel panel3;

        // Token: 0x0400075F RID: 1887
        private GroupBox gbOptions;

        // Token: 0x04000760 RID: 1888
        private Label label5;

        // Token: 0x04000761 RID: 1889
        private System.Windows.Forms.TextBox txtTo;

        // Token: 0x04000762 RID: 1890
        private System.Windows.Forms.TextBox txtFrom;

        // Token: 0x04000763 RID: 1891
        private RadioButton rbCustomed;

        // Token: 0x04000764 RID: 1892
        private RadioButton rbAllSheets;

        // Token: 0x04000765 RID: 1893
        private RadioButton rbCurrrentSheet;

        // Token: 0x04000766 RID: 1894
        private RadioButton rbFrom;

        // Token: 0x04000767 RID: 1895
        private System.Windows.Forms.TextBox txtCustomed;

        // Token: 0x04000768 RID: 1896
        private ComboBox cbSpecifyExportStyle;

        // Token: 0x04000769 RID: 1897
        private ToolStripSplitButton btnOptions;
    }
}