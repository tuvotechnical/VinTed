using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HNB_MyTools_AutoCAD.lic;

namespace HNB_MyTools_AutoCAD
{
	// Token: 0x02000002 RID: 2
	public class MergedDWGFiles
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		// (set) Token: 0x06000002 RID: 2 RVA: 0x00002058 File Offset: 0x00000258
		private ObjectId Standard_TextStyleID { get; set; }

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000003 RID: 3 RVA: 0x00002061 File Offset: 0x00000261
		// (set) Token: 0x06000004 RID: 4 RVA: 0x00002069 File Offset: 0x00000269
		public double SaveTargetInsertPoint { get; set; }

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000005 RID: 5 RVA: 0x00002072 File Offset: 0x00000272
		// (set) Token: 0x06000006 RID: 6 RVA: 0x0000207A File Offset: 0x0000027A
		public double minPoint { get; set; }

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000007 RID: 7 RVA: 0x00002083 File Offset: 0x00000283
		// (set) Token: 0x06000008 RID: 8 RVA: 0x0000208B File Offset: 0x0000028B
		public double maxPoint { get; set; }

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000009 RID: 9 RVA: 0x00002094 File Offset: 0x00000294
		// (set) Token: 0x0600000A RID: 10 RVA: 0x0000209C File Offset: 0x0000029C
		public double Gap { get; set; }

		// Token: 0x0600000B RID: 11 RVA: 0x000020A8 File Offset: 0x000002A8
		public bool Execute(Document doc, string PathContainingDWGFiles, string FileToSave, double gap = 0.0, bool DeleteFileAfterMerging = false)
		{
			this.Gap = gap;
			MergedDWGFiles.fileToSave = FileToSave;
			if (!Directory.Exists(PathContainingDWGFiles))
			{
				MessageBox.Show("Thư mục chứa file .dwg không tồn tại.\n" + PathContainingDWGFiles, MergedDWGFiles.caption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
				return false;
			}
			MergedDWGFiles.pathContainingDWG = PathContainingDWGFiles;
			if (!Task.Run<bool>(() => MergedDWGFiles.MyAsyncFunction()).Result)
			{
				MessageBox.Show("Mã khóa sản phẩm đã hết hạn", MergedDWGFiles.caption, MessageBoxButton.OK, MessageBoxImage.Asterisk);
				new frmAbout().ShowDialog();
				return false;
			}
			if (!this.CreateNewDrawingFile(MergedDWGFiles.fileToSave))
			{
				MessageBox.Show("Tệp đang được sử dụng " + MergedDWGFiles.fileToSave, MergedDWGFiles.caption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
				return false;
			}
			this.ConfigurationTargetDrawing();
			string str = string.Empty;
			string[] files = new string[0];
			try
			{
				using (Transaction transaction = doc.TransactionManager.StartTransaction())
				{
					BlockTable blockTable = (BlockTable)transaction.GetObject(doc.Database.BlockTableId, 0);
					this.processedFiles = new HashSet<string>();
					bool firstFile = true;
					files = (from f in Directory.GetFiles(MergedDWGFiles.pathContainingDWG, "Sheet_*.dwg")
					orderby MergedDWGFiles.SortedToGetFileNumber(f)
					select f).ToArray<string>();
					if (files.Length == 0)
					{
						transaction.Commit();
						MessageBox.Show("Không tìm thấy các file bản vẽ cần gộp trong thư mục:\n" + MergedDWGFiles.pathContainingDWG + "\n\nChú ý:\n- Các file được gộp phải được đặt tên (Sheet_1, Sheet_2, Sheet_*)\n- Nếu gộp file trên môi trường \"Layout\" thì tên file và tên layout\n  phải giốp nhau.", MergedDWGFiles.caption, MessageBoxButton.OK, MessageBoxImage.Asterisk);
						if (Directory.Exists(MergedDWGFiles.pathContainingDWG))
						{
							Process.Start(MergedDWGFiles.pathContainingDWG);
						}
						return false;
					}
					if (files.Length > 1)
					{
						foreach (string text in files)
						{
							if (!(text == MergedDWGFiles.fileToSave) && !this.processedFiles.Contains(text))
							{
								str = text;
								if (!this.dwgFileProcessing(text, MergedDWGFiles.fileToSave, firstFile))
								{
									return false;
								}
								this.processedFiles.Add(text);
								firstFile = false;
							}
						}
						transaction.Commit();
						if (DeleteFileAfterMerging)
						{
							new Thread(delegate()
							{
								this.DeleteDWGfile(files);
							})
							{
								IsBackground = true
							}.Start();
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Lỗi xử lý tệp " + str + "\n\n Message: " + ex.Message, MergedDWGFiles.caption, MessageBoxButton.OK, MessageBoxImage.Hand);
				return false;
			}
			if (File.Exists(MergedDWGFiles.fileToSave))
			{
				Document document;
				try
				{
					document = DocumentCollectionExtension.Open(Application.DocumentManager, MergedDWGFiles.fileToSave, false);
					Application.DocumentManager.MdiActiveDocument = document;
				}
				catch (Exception ex2)
				{
					MessageBox.Show("Lỗi mở tệp " + MergedDWGFiles.fileToSave + "\n\n Message: " + ex2.Message, MergedDWGFiles.caption, MessageBoxButton.OK, MessageBoxImage.Hand);
					return false;
				}
				if (document != null && !string.IsNullOrEmpty(MergedDWGFiles.SpecifyModel))
				{
					if (MergedDWGFiles.SpecifyModel.ToUpper() == "MODEL")
					{
						document.SendStringToExecute("zoom e ", true, false, true);
						document.SendStringToExecute("regen ", true, false, true);
						document.SendStringToExecute("-PURGE All *\nNo\n", true, false, true);
					}
					else if (MergedDWGFiles.SpecifyModel.ToUpper() == "LAYOUT")
					{
						Application.DocumentManager.DocumentActivated += new DocumentCollectionEventHandler(this.acDocs_DocumentActivated);
					}
				}
			}
			else
			{
				MessageBox.Show("Không tìm thấy tệp " + MergedDWGFiles.fileToSave, MergedDWGFiles.caption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
			MergedDWGFiles.fileToSave = (MergedDWGFiles.pathContainingDWG = (MergedDWGFiles.SpecifyModel = string.Empty));
			this.SaveTargetInsertPoint = 0.0;
			return true;
		}

		// Token: 0x0600000C RID: 12 RVA: 0x000024A8 File Offset: 0x000006A8
		private static bool MyAsyncFunction()
		{
			return new GetLicenseInfo().Get();
		}

		// Token: 0x0600000D RID: 13 RVA: 0x000024B9 File Offset: 0x000006B9
		public void acDocs_DocumentActivated(object sender, DocumentCollectionEventArgs e)
		{
			if (e.Document != null)
			{
				Application.DocumentManager.MdiActiveDocument.SendStringToExecute("LayoutConfiguration ", true, false, false);
			}
			Application.DocumentManager.DocumentActivated -= new DocumentCollectionEventHandler(this.acDocs_DocumentActivated);
		}

		// Token: 0x0600000E RID: 14 RVA: 0x000024F8 File Offset: 0x000006F8
		private bool ConfigurationTargetDrawing()
		{
			try
			{
				using (Database database = new Database(true, false))
				{
					database.ReadDwgFile(MergedDWGFiles.fileToSave, FileShare.ReadWrite, false, null);
					this.CreateOrUpdateTextStyle(database, "Standard", "Arial.ttf");
					database.Lunits = 2;
					database.Luprec = 0;
					database.Insunits = 4;
					database.Aunits = 0;
					database.Auprec = 0;
					database.SaveAs(MergedDWGFiles.fileToSave, 33);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Lỗi khởi tạo \"style\" cho: " + MergedDWGFiles.fileToSave + "\n\n" + ex.Message, MergedDWGFiles.caption, MessageBoxButton.OK, MessageBoxImage.Hand);
			}
			return false;
		}

		// Token: 0x0600000F RID: 15 RVA: 0x000025B4 File Offset: 0x000007B4
		private void DeleteDWGfile(string[] files)
		{
			foreach (string path in files)
			{
				try
				{
					File.Delete(path);
				}
				catch
				{
				}
			}
		}

		// Token: 0x06000010 RID: 16 RVA: 0x000025F0 File Offset: 0x000007F0
		private static int SortedToGetFileNumber(string fileName)
		{
			string text = Path.GetFileNameWithoutExtension(fileName).Substring(4);
			int num = text.IndexOf('_');
			if (num >= 0)
			{
				text = text.Substring(num + 1);
			}
			int result;
			if (int.TryParse(text, out result))
			{
				return result;
			}
			return 0;
		}

		// Token: 0x06000011 RID: 17 RVA: 0x00002630 File Offset: 0x00000830
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
						database.SaveAs(fileName, 33);
					}
					return true;
				}
			}
			catch
			{
			}
			return false;
		}

		// Token: 0x06000012 RID: 18 RVA: 0x00002698 File Offset: 0x00000898
		private bool dwgFileProcessing(string sourceFilename, string targetFilename, bool firstFile)
		{
			using (Database database = new Database(true, false))
			{
				database.ReadDwgFile(targetFilename, FileShare.ReadWrite, false, null);
				int num = 0;
				bool flag = false;
				using (Database database2 = new Database(true, false))
				{
					database2.ReadDwgFile(sourceFilename, FileShare.ReadWrite, false, null);
					if (MergedDWGFiles.CheckObjectExistsInDatabase(database2, "MODEL"))
					{
						MergedDWGFiles.SpecifyModel = "MODEL";
						this.FindBlockToGetSize(database2, out num, sourceFilename);
						if (this.maxPoint > 0.0)
						{
							if (num <= 0)
							{
								num = 1;
							}
							this.ProcessingObjectsInModelSpace_SourceDatabase(database2, num, out flag);
							Point3d point3d;
							if (firstFile)
							{
								double saveTargetInsertPoint = this.maxPoint - this.minPoint;
								point3d..ctor(-this.minPoint, -this.minPoint, 0.0);
								this.SaveTargetInsertPoint = saveTargetInsertPoint;
							}
							else
							{
								double num2 = this.SaveTargetInsertPoint + this.Gap - this.minPoint;
								point3d..ctor(num2, -this.minPoint, 0.0);
								this.SaveTargetInsertPoint = num2 + this.maxPoint;
							}
							Matrix3d matrix3d = Matrix3d.Displacement(point3d - Point3d.Origin);
							database.Insert(matrix3d, database2, true);
						}
						if (flag)
						{
							this.CreateNewDimStyle(database, num);
						}
						string[] layerName = new string[]
						{
							"Hidden 1",
							"Hidden 2"
						};
						MergedDWGFiles.SetLayerLinetype(database, layerName, "HIDDEN");
						if ((int)database.Ltscale < num)
						{
							database.Ltscale = (double)num;
						}
					}
					else if (MergedDWGFiles.CheckObjectExistsInDatabase(database2, "LAYOUT"))
					{
						MergedDWGFiles.SpecifyModel = "LAYOUT";
						string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceFilename);
						if (fileNameWithoutExtension.Contains("_"))
						{
							int num3 = fileNameWithoutExtension.LastIndexOf('_');
							if (num3 != -1)
							{
								string text = fileNameWithoutExtension.Substring(num3 + 1);
								string sourceLayoutName = text + "_" + text;
								this.ProcessingObjectsInPaperSpace_SourceDatabase(database2, database, sourceLayoutName);
							}
						}
					}
					else
					{
						if (MessageBox.Show("Không có đối tượng nào trong \"Model Space\", bạn muốn tiếp tục?\n\n" + sourceFilename, MergedDWGFiles.caption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
						{
							return true;
						}
						return false;
					}
					database.SaveAs(MergedDWGFiles.fileToSave, 33);
					database2.Dispose();
				}
			}
			return true;
		}

		// Token: 0x06000013 RID: 19 RVA: 0x000028E0 File Offset: 0x00000AE0
		private void FindBlockToGetSize(Database db, out int scaleX, string sourceFilename)
		{
			scaleX = 0;
			using (Transaction transaction = db.TransactionManager.StartTransaction())
			{
				BlockTable blockTable = transaction.GetObject(db.BlockTableId, 1) as BlockTable;
				foreach (ObjectId objectId in (transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], 0) as BlockTableRecord))
				{
					DBObject @object = transaction.GetObject(objectId, 0);
					if (@object is BlockReference)
					{
						BlockReference blockReference = (BlockReference)@object;
						if (blockReference.Name.Contains("Borders"))
						{
							scaleX = (int)blockReference.ScaleFactors.X;
							Extents3d extents3d;
							extents3d..ctor();
							Entity entity = transaction.GetObject(objectId, 0) as Entity;
							extents3d.AddExtents(entity.GeometricExtents);
							string text = extents3d.MinPoint.X.ToString();
							string text2 = extents3d.MaxPoint.X.ToString();
							if (!(string.IsNullOrEmpty(text) & string.IsNullOrEmpty(text2)))
							{
								this.minPoint = this.ConvertStringToDouble(text);
								this.maxPoint = this.ConvertStringToDouble(text2);
							}
							string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceFilename);
							((BlockTableRecord)transaction.GetObject(blockTable[blockReference.Name], 1)).Name = fileNameWithoutExtension;
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
									((BlockTableRecord)transaction.GetObject(blockTable[blockReference.Name], 1)).Name = name;
								}
							}
							catch (Exception)
							{
							}
						}
					}
				}
				transaction.Commit();
			}
		}

		// Token: 0x06000014 RID: 20 RVA: 0x00002B14 File Offset: 0x00000D14
		private double ConvertStringToDouble(string input)
		{
			double result = 0.0;
			try
			{
				double.TryParse(input, out result);
			}
			catch
			{
			}
			return result;
		}

		// Token: 0x06000015 RID: 21 RVA: 0x00002B4C File Offset: 0x00000D4C
		private static void SetLayerLinetype(Database db, string[] layerName, string linetypeName)
		{
			using (Transaction transaction = db.TransactionManager.StartTransaction())
			{
				LayerTable layerTable = transaction.GetObject(db.LayerTableId, 0) as LayerTable;
				foreach (string text in layerName)
				{
					if (layerTable.Has(text))
					{
						LayerTableRecord layerTableRecord = transaction.GetObject(layerTable[text], 0) as LayerTableRecord;
						LinetypeTable linetypeTable = transaction.GetObject(db.LinetypeTableId, 0) as LinetypeTable;
						try
						{
							if (!linetypeTable.Has(linetypeName))
							{
								db.LoadLineTypeFile(linetypeName, "acad.lin");
							}
						}
						catch (Exception)
						{
						}
						if (linetypeTable.Has(linetypeName))
						{
							transaction.GetObject(layerTable[text], 1);
							layerTableRecord.LinetypeObjectId = linetypeTable[linetypeName];
						}
					}
				}
				transaction.Commit();
			}
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002C3C File Offset: 0x00000E3C
		private static bool CheckObjectExistsInDatabase(Database db, string SpecifyModel)
		{
			try
			{
				using (Transaction transaction = db.TransactionManager.StartTransaction())
				{
					transaction.GetObject(db.BlockTableId, 0);
					ObjectId objectId = ObjectId.Null;
					if (SpecifyModel == "MODEL")
					{
						objectId = SymbolUtilityServices.GetBlockModelSpaceId(db);
					}
					else if (SpecifyModel == "LAYOUT")
					{
						objectId = SymbolUtilityServices.GetBlockPaperSpaceId(db);
					}
					if (objectId == ObjectId.Null)
					{
						return false;
					}
					if ((transaction.GetObject(objectId, 0) as BlockTableRecord).Cast<ObjectId>().Count<ObjectId>() == 0)
					{
						return false;
					}
					transaction.Commit();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Lỗi kiểm tra đối tượng trong \"sourceDb\" \n" + ex.Message, MergedDWGFiles.caption, MessageBoxButton.OK, MessageBoxImage.Hand);
				return false;
			}
			return true;
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00002D18 File Offset: 0x00000F18
		private void ProcessingObjectsInModelSpace_SourceDatabase(Database db, int value, out bool dimExists)
		{
			dimExists = false;
			using (Transaction transaction = db.TransactionManager.StartTransaction())
			{
				BlockTable blockTable = (BlockTable)transaction.GetObject(db.BlockTableId, 0);
				BlockTableRecord blockTableRecord = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], 0);
				if (blockTableRecord != null)
				{
					foreach (ObjectId objectId in blockTableRecord)
					{
						Entity entity = transaction.GetObject(objectId, 0) as Entity;
						if (entity != null && entity is Dimension)
						{
							dimExists = true;
							Dimension dimension = (Dimension)entity;
							entity.UpgradeOpen();
							dimension.Dimscale = (double)value;
							string styleName = string.Format("1-{0}", value);
							this.CreateNewDimStyle(db, value);
							ObjectId objectID_DimStyle = MergedDWGFiles.GetObjectID_DimStyle(db, styleName);
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
								ObjectId objectID_Arrow = MergedDWGFiles.GetObjectID_Arrow(db, "DIMBLK2", "_CLOSED");
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

		// Token: 0x06000018 RID: 24 RVA: 0x00002F48 File Offset: 0x00001148
		private void CreateNewDimStyle(Database db, int DimscaleValue)
		{
			string text = string.Format("1-{0}", DimscaleValue);
			using (Transaction transaction = db.TransactionManager.StartTransaction())
			{
				DimStyleTable dimStyleTable = (DimStyleTable)transaction.GetObject(db.DimStyleTableId, 0);
				ObjectId objectId = ObjectId.Null;
				if (!dimStyleTable.Has(text))
				{
					dimStyleTable.UpgradeOpen();
					DimStyleTableRecord dimStyleTableRecord = new DimStyleTableRecord();
					this.SetDimStyleVariable(transaction, db, dimStyleTableRecord, text, DimscaleValue);
					objectId = dimStyleTable.Add(dimStyleTableRecord);
					transaction.AddNewlyCreatedDBObject(dimStyleTableRecord, true);
				}
				else
				{
					objectId = dimStyleTable[text];
				}
				DimStyleTableRecord dimStyleTableRecord2 = (DimStyleTableRecord)transaction.GetObject(objectId, 0);
				if (dimStyleTableRecord2.ObjectId != db.Dimstyle)
				{
					db.Dimstyle = dimStyleTableRecord2.ObjectId;
					db.SetDimstyleData(dimStyleTableRecord2);
				}
				transaction.Commit();
			}
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00003024 File Offset: 0x00001224
		private void SetDimStyleVariable(Transaction tr, Database db, DimStyleTableRecord dim, string dimStyleName, int DimscaleValue)
		{
			dim.Dimclrd = Color.FromColorIndex(195, 256);
			ObjectId ojectID_LineStyle = MergedDWGFiles.GetOjectID_LineStyle(db, "BYLAYER");
			if (ojectID_LineStyle != ObjectId.Null)
			{
				dim.Dimltype = ojectID_LineStyle;
			}
			dim.Dimlwd = -1;
			dim.Dimdle = 0.5;
			dim.Dimdli = 0.5;
			dim.Dimsd1 = false;
			dim.Dimsd2 = false;
			dim.Dimclre = Color.FromColorIndex(195, 256);
			dim.Dimltex1 = dim.Dimltype;
			dim.Dimltex2 = dim.Dimltype;
			dim.Dimlwe = -1;
			dim.Dimse1 = false;
			dim.Dimse2 = false;
			dim.Dimexe = 0.5;
			dim.Dimexo = 1.0;
			dim.Dimsah = true;
			ObjectId objectID_Arrow = MergedDWGFiles.GetObjectID_Arrow(db, "DIMBLK1", "_OBLIQUE");
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
			this.Standard_TextStyleID = MergedDWGFiles.GetObjectID_TextStyle(tr, db, "Standard");
			if (this.Standard_TextStyleID != ObjectId.Null)
			{
				dim.Dimtxsty = this.Standard_TextStyleID;
			}
			dim.Dimclrt = Color.FromColorIndex(195, 7);
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
			dim.Dimtfillclr = Color.FromColorIndex(195, 0);
			dim.Dimtix = false;
			dim.Dimtsz = 0.0;
			dim.Dimtvp = 0.0;
			dim.Dimtzin = 0;
		}

		// Token: 0x0600001A RID: 26 RVA: 0x000033A0 File Offset: 0x000015A0
		private static ObjectId GetObjectID_DimStyle(Database db, string styleName)
		{
			ObjectId result = ObjectId.Null;
			using (Transaction transaction = db.TransactionManager.StartTransaction())
			{
				SymbolTable symbolTable = (SymbolTable)transaction.GetObject(db.DimStyleTableId, 0);
				if (((DimStyleTable)transaction.GetObject(db.DimStyleTableId, 0)).Has(styleName))
				{
					result = symbolTable[styleName];
				}
				transaction.Commit();
			}
			return result;
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00003418 File Offset: 0x00001618
		private static ObjectId GetOjectID_LineStyle(Database db, string LineStyleName)
		{
			ObjectId result = ObjectId.Null;
			using (Transaction transaction = db.TransactionManager.StartTransaction())
			{
				LinetypeTable linetypeTable = (LinetypeTable)transaction.GetObject(db.LinetypeTableId, 0);
				if (linetypeTable.Has(LineStyleName))
				{
					result = linetypeTable[LineStyleName];
				}
				transaction.Commit();
			}
			return result;
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00003480 File Offset: 0x00001680
		private static ObjectId GetObjectID_Arrow(Database db, string arrow, string newArrowName)
		{
			ObjectId result = ObjectId.Null;
			try
			{
				string text = Application.GetSystemVariable(arrow) as string;
				Application.SetSystemVariable(arrow, newArrowName);
				if (text.Length != 0)
				{
					Application.SetSystemVariable(arrow, text);
				}
				using (Transaction transaction = db.TransactionManager.StartTransaction())
				{
					BlockTable blockTable = (BlockTable)transaction.GetObject(db.BlockTableId, 0);
					if (blockTable.Has(newArrowName))
					{
						result = blockTable[newArrowName];
					}
					transaction.Commit();
				}
			}
			catch
			{
			}
			return result;
		}

		// Token: 0x0600001D RID: 29 RVA: 0x0000351C File Offset: 0x0000171C
		private static ObjectId GetObjectID_TextStyle(Transaction tr, Database db, string TextStyleName)
		{
			ObjectId result = ObjectId.Null;
			new TextStyleTableRecord();
			TextStyleTable textStyleTable = (TextStyleTable)tr.GetObject(db.TextStyleTableId, 0, true, true);
			if (textStyleTable.Has(TextStyleName))
			{
				result = textStyleTable[TextStyleName];
			}
			return result;
		}

		// Token: 0x0600001E RID: 30 RVA: 0x0000355C File Offset: 0x0000175C
		private static ObjectId GetObjectID_MText(Transaction tr, Database db, string mTextName)
		{
			ObjectId @null = ObjectId.Null;
			try
			{
				foreach (ObjectId objectId in ((SymbolTable)tr.GetObject(db.TextStyleTableId, 0)))
				{
					TextStyleTableRecord textStyleTableRecord = (TextStyleTableRecord)tr.GetObject(objectId, 0);
					Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(string.Format("\nName: {0}", textStyleTableRecord.Name));
				}
			}
			catch (Exception)
			{
			}
			return @null;
		}

		// Token: 0x0600001F RID: 31 RVA: 0x000035F8 File Offset: 0x000017F8
		private void CreateOrUpdateTextStyle(Database db, string textStyleName, string fontName)
		{
			using (Transaction transaction = db.TransactionManager.StartTransaction())
			{
				TextStyleTable textStyleTable = transaction.GetObject(db.TextStyleTableId, 0) as TextStyleTable;
				if (textStyleTable.Has(textStyleName))
				{
					(transaction.GetObject(textStyleTable[textStyleName], 1) as TextStyleTableRecord).FileName = fontName;
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

		// Token: 0x06000020 RID: 32 RVA: 0x00003694 File Offset: 0x00001894
		private void ProcessingObjectsInPaperSpace_SourceDatabase(Database sourceDb, Database targetDb, string sourceLayoutName)
		{
			using (Transaction transaction = targetDb.TransactionManager.StartTransaction())
			{
				try
				{
					HostApplicationServices.WorkingDatabase = targetDb;
					LayoutManager layoutManager = LayoutManager.Current;
					ObjectId objectId = layoutManager.CreateLayout(sourceLayoutName);
					Layout layout = (Layout)transaction.GetObject(objectId, 1);
					HostApplicationServices.WorkingDatabase = sourceDb;
					using (Transaction transaction2 = sourceDb.TransactionManager.StartTransaction())
					{
						if ((transaction2.GetObject(sourceDb.LayoutDictionaryId, 0) as DBDictionary).Contains(sourceLayoutName))
						{
							ObjectId layoutId = layoutManager.GetLayoutId(sourceLayoutName);
							Layout layout2 = (Layout)transaction2.GetObject(layoutId, 0);
							layout.CopyFrom(layout2);
							ObjectIdCollection objectIdCollection = new ObjectIdCollection((transaction2.GetObject(layout2.BlockTableRecordId, 0) as BlockTableRecord).Cast<ObjectId>().ToArray<ObjectId>());
							IdMapping idMapping = new IdMapping();
							targetDb.WblockCloneObjects(objectIdCollection, layout.BlockTableRecordId, idMapping, 2, false);
						}
						transaction2.Commit();
					}
					transaction.Commit();
				}
				catch (Exception ex)
				{
					MessageBox.Show("Đã xảy ra lỗi khi xử lý đối tượng trên \"Layout\" \n" + ex.Message, MergedDWGFiles.caption, MessageBoxButton.OK, MessageBoxImage.Hand);
					transaction.Abort();
				}
			}
		}

		// Token: 0x04000006 RID: 6
		private HashSet<string> processedFiles;

		// Token: 0x04000007 RID: 7
		private static string fileToSave;

		// Token: 0x04000008 RID: 8
		private static string pathContainingDWG;

		// Token: 0x04000009 RID: 9
		private static string SpecifyModel;

		// Token: 0x0400000A RID: 10
		private static string caption = "Combine DWG Files";
	}
}
