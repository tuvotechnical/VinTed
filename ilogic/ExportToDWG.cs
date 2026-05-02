using System;
using System.IO;
using System.Windows.Forms;
using Inventor;
using Microsoft.Win32;

namespace HNB_MyTools_Inventor
{
	// Token: 0x0200008D RID: 141
	public class ExportToDWG
	{
		// Token: 0x06000C08 RID: 3080 RVA: 0x000AA1E0 File Offset: 0x000A91E0
		public bool Execute(Inventor.Application invApp, string file_Code)
		{
			this.invApp = invApp;
			try
			{
				string text;
				if (!this.CheckAutoCADInstallation(out text))
				{
					MessageBox.Show("AutoCAD chưa được cài đặt, vui lòng kiểm tra lại.", this.caption, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					return false;
				}
				if (!this.DWGOutUsingTranslatorAddIn(file_Code))
				{
					return false;
				}
			}
			catch (Exception)
			{
				return false;
			}
			return true;
		}

		// Token: 0x06000C09 RID: 3081 RVA: 0x000AA240 File Offset: 0x000A9240
		public bool CheckAutoCADInstallation(out string versionNo)
		{
			versionNo = null;
			try
			{
				RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\\\Autodesk\\\\AutoCAD");
				if (registryKey != null)
				{
					versionNo = (registryKey.GetValue("CurVer") as string);
					return true;
				}
				return false;
			}
			catch
			{
			}
			return false;
		}

		// Token: 0x06000C0A RID: 3082 RVA: 0x000AA294 File Offset: 0x000A9294
		public bool DWGOutUsingTranslatorAddIn(string file_Code)
		{
			string directoryName = Path.GetDirectoryName(this.invApp.ActiveDocument.FullFileName);
			string inventorVersion = this.invApp.SoftwareVersion.ProductName + " " + this.invApp.SoftwareVersion.DisplayVersion.Substring(0, 4);
			Inventor.DrawingDocument drawingDocument = this.invApp.ActiveDocument as Inventor.DrawingDocument;
			drawingDocument.DisplayName.Replace(".idw", null);
			Inventor.Transaction transaction = null;
			try
			{
				transaction = this.invApp.TransactionManager.StartTransaction((Inventor._Document)drawingDocument, "DWGExport");
				TranslatorAddIn translatorAddIn = null;
				try
				{
					translatorAddIn = (this.invApp.ApplicationAddIns.get_ItemById("{C24E3AC2-122E-11D5-8E91-0010B541CD80}") as TranslatorAddIn);
				}
				catch (Exception)
				{
					MessageBox.Show("DWG translator not found", this.caption, MessageBoxButtons.OK, MessageBoxIcon.Hand);
					return false;
				}
				if (!translatorAddIn.Activated)
				{
					translatorAddIn.Activate();
				}
				NameValueMap nameValueMap = this.invApp.TransientObjects.CreateNameValueMap();
				string value;
				if (!this.LoadConfigurationFile(directoryName, out value, inventorVersion))
				{
					return false;
				}
				nameValueMap.Add("Export_Acad_IniFile", value);
				TranslationContext translationContext = this.invApp.TransientObjects.CreateTranslationContext();
				translationContext.Type = IOMechanismEnum.kFileBrowseIOMechanism;
				string fileName = directoryName + "\\" + file_Code + ".dwg";
				DataMedium dataMedium = this.invApp.TransientObjects.CreateDataMedium();
				dataMedium.FileName = fileName;
				translatorAddIn.SaveCopyAs(this.invApp.ActiveDocument, translationContext, nameValueMap, dataMedium);
				transaction.End();
				return true;
			}
			catch (Exception ex)
			{
				if (transaction != null)
				{
					transaction.Abort();
				}
				MessageBox.Show("Đã xảy ra lỗi trong quá trình xuất tệp .dwg\n" + ex.Message, this.caption, MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
			return false;
		}

		// Token: 0x06000C0B RID: 3083 RVA: 0x000AA480 File Offset: 0x000A9480
		private bool LoadConfigurationFile(string OutputFolder, out string duongDanDwgIni, string InventorVersion)
		{
			duongDanDwgIni = OutputFolder + "\\ExportToDWG.ini";
			try
			{
				using (StreamWriter streamWriter = System.IO.File.CreateText(duongDanDwgIni))
				{
					streamWriter.WriteLine("[EXPORT SELECT OPTIONS]");
					streamWriter.WriteLine("AUTOCAD VERSION=AutoCAD 2007");
					streamWriter.WriteLine("CREATE AUTOCAD MECHANICAL=No");
					streamWriter.WriteLine("USE TRANSMITTAL=No");
					streamWriter.WriteLine("USE CUSTOMIZE=No");
					streamWriter.WriteLine("CUSTOMIZE FILE=C:\\Users\\Public\\Documents\\Autodesk\\" + InventorVersion + "\\Design Data\\DWG-DXF\\FlatPattern.xml");
					streamWriter.WriteLine("CREATE LAYER GROUP=No");
					streamWriter.WriteLine("PARTS ONLY=No");
					streamWriter.WriteLine("REPLACE SPLINE=No");
					streamWriter.WriteLine("CHORD TOLERANCE=0.001000");
					streamWriter.WriteLine("[EXPORT PROPERTIES]");
					streamWriter.WriteLine("SELECTED PROPERTIES=");
					streamWriter.WriteLine("[EXPORT DESTINATION]");
					streamWriter.WriteLine("SPACE=Model");
					streamWriter.WriteLine("SCALING=Geometry");
					streamWriter.WriteLine("ALL SHEETS=No");
					streamWriter.WriteLine("MAPPING=MapsBest");
					streamWriter.WriteLine("MODEL GEOMETRY ONLY=No");
					streamWriter.WriteLine("EXPLODE DIMENSIONS=No");
					streamWriter.WriteLine("SYMBOLS ARE BLOCKED=Yes");
					streamWriter.WriteLine("AUTOCAD TEMPLATE=C:\\Users\\banghn\\AppData\\Local\\Autodesk\\AutoCAD 2022\\R24.1\\enu\\Template\\acad.dwt");
					streamWriter.WriteLine("DESTINATION DXF=No");
					streamWriter.WriteLine("USE ACI FOR ENTITIES AND LAYERS=No");
					streamWriter.WriteLine("ALLOW RASTER VIEWS=No");
					streamWriter.WriteLine("SHOW DESTINATION PAGE=Yes");
					streamWriter.WriteLine("ENABLE POSTPROCESS=Yes");
					streamWriter.WriteLine("[EXPORT LINE TYPE & LINE SCALE]");
					streamWriter.WriteLine("LINE TYPE FILE=C:\\Users\\Public\\Documents\\Autodesk\\" + InventorVersion + "\\COMPATIBILITY\\Support\\invISO.lin");
					streamWriter.WriteLine("Continuous=Continuous;0.");
					streamWriter.WriteLine("Dashed=DASHED;0.");
					streamWriter.WriteLine("Dashed Space=DASHED_SPACE;0.");
					streamWriter.WriteLine("Long Dash Dotted=LONG_DASH_DOTTED;0.");
					streamWriter.WriteLine("Long Dash Double Dot=LONG_DASH_DOUBLE_DOT;0.");
					streamWriter.WriteLine("Dotted=DOTTED;0.");
					streamWriter.WriteLine("Chain=CHAIN;0.");
					streamWriter.WriteLine("Double Dash Chain=DOUBLE_DASH_CHAIN;0.");
					streamWriter.WriteLine("Dash Double Dot=DASH_DOUBLE_DOT;0.");
					streamWriter.WriteLine("Dash Dot=DASH_DOT;0.");
					streamWriter.WriteLine("Double Dash Dot=DOUBLE_DASH_DOT;0.");
					streamWriter.WriteLine("Double Dash Double Dot=DOUBLE_DASH_DOUBLE_DOT;0.");
					streamWriter.WriteLine("Dash Triple Dot=DASH_TRIPLE_DOT;0.");
					streamWriter.WriteLine("Double Dash Triple Dot=DOUBLE_DASH_TRIPLE_DOT;0.");
				}
				return true;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Lỗi khi tạo tệp Configuration.ini: " + ex.Message, this.caption, MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
			return false;
		}

		// Token: 0x04000A69 RID: 2665
		private Inventor.Application invApp;

		// Token: 0x04000A6A RID: 2666
		private string caption = "Export To DWG";
	}
}
