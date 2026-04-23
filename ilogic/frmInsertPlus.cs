using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using HNB_MyTools_Inventor.Properties;
using Inventor;
using Microsoft.CSharp.RuntimeBinder;
using MyToolsExt;

namespace HNB_MyTools_Inventor
{
	// Token: 0x02000051 RID: 81
	public class frmInsertPlus : Form
	{
		// Token: 0x1700014C RID: 332
		// (get) Token: 0x060007BD RID: 1981 RVA: 0x00063E87 File Offset: 0x00062E87
		// (set) Token: 0x060007BE RID: 1982 RVA: 0x00063E8F File Offset: 0x00062E8F
		private string iamIsActive { get; set; }

		// Token: 0x1700014D RID: 333
		// (get) Token: 0x060007BF RID: 1983 RVA: 0x00063E98 File Offset: 0x00062E98
		// (set) Token: 0x060007C0 RID: 1984 RVA: 0x00063EA0 File Offset: 0x00062EA0
		private bool LastAxesOpposed { get; set; }

		// Token: 0x060007C1 RID: 1985 RVA: 0x00063EAC File Offset: 0x00062EAC
		public frmInsertPlus()
		{
			this.invApp = InventorManagerHelper.GetApplication;
			this.listInsertConstraints = new List<string>();
			this.InitializeComponent();
		}

		// Token: 0x060007C2 RID: 1986 RVA: 0x00063F00 File Offset: 0x00062F00
		private void frmInsertPlus_Load(object sender, EventArgs e)
		{
			if (this.invApp.ActiveDocumentType == Inventor.DocumentTypeEnum.kAssemblyDocumentObject)
			{
				if (this.iamDoc == null)
				{
					this.GetDoc();
				}
				this.invApp.ScreenUpdating = true;
				if (this.invApp.ActiveView != null)
				{
					base.Left = this.invApp.ActiveView.Left - 7;
					base.Top = this.invApp.ActiveView.Top;
				}
			}
		}

		// Token: 0x060007C3 RID: 1987 RVA: 0x0002526C File Offset: 0x0002426C
		private void frmInsertPlus_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				base.Close();
			}
		}

		// Token: 0x060007C4 RID: 1988 RVA: 0x00063F74 File Offset: 0x00062F74
		private void frmInsertPlus_Activated(object sender, EventArgs e)
		{
			this.SetActivatedControl();
		}

		// Token: 0x060007C5 RID: 1989 RVA: 0x00063F7C File Offset: 0x00062F7C
		private void SetActivatedControl()
		{
			this.InvokeSafe(delegate
			{
				base.ActiveControl = (this.AxesOpposed ? this.btnOpposed : this.btnAligned);
			});
		}

		// Token: 0x060007C6 RID: 1990 RVA: 0x00063F90 File Offset: 0x00062F90
		private void frmInsertPlus_FormClosed(object sender, FormClosedEventArgs e)
		{
			List<string> list = this.listInsertConstraints;
			if (list != null)
			{
				list.Clear();
			}
			new ComAwareEventInfo(typeof(ApplicationEventsSink_Event), "OnActivateDocument").RemoveEventHandler(this.invApp.ApplicationEvents, new ApplicationEventsSink_OnActivateDocumentEventHandler(this, (UIntPtr)ldftn(ApplicationEvents_OnActivateDocument)));
		}

		// Token: 0x060007C7 RID: 1991 RVA: 0x00063FE0 File Offset: 0x00062FE0
		private void GetDoc()
		{
			this.iamDoc = (Inventor.AssemblyDocument)this.invApp.ActiveDocument;
			if (this.iamDoc == null)
			{
				return;
			}
			this.iamDef = this.iamDoc.ComponentDefinition;
			if (this.iamDef == null)
			{
				return;
			}
			if (this.iamDef.ActiveOccurrence != null)
			{
				mBox.Warning(this, "Chế độ \"Edit Object\" đang được kích hoạt");
				return;
			}
			if (!this.iamDef.RepresentationsManager.ActivePositionalRepresentation.Master)
			{
				this.iamDef.RepresentationsManager.PositionalRepresentations[1].Activate();
			}
		}

		// Token: 0x060007C8 RID: 1992 RVA: 0x00064078 File Offset: 0x00063078
		private Task ProcessObjectsInList2(string transactionName, double offsetValue)
		{
			frmInsertPlus.<ProcessObjectsInList2>d__25 <ProcessObjectsInList2>d__;
			<ProcessObjectsInList2>d__.<>t__builder = AsyncTaskMethodBuilder.Create();
			<ProcessObjectsInList2>d__.<>4__this = this;
			<ProcessObjectsInList2>d__.transactionName = transactionName;
			<ProcessObjectsInList2>d__.offsetValue = offsetValue;
			<ProcessObjectsInList2>d__.<>1__state = -1;
			<ProcessObjectsInList2>d__.<>t__builder.Start<frmInsertPlus.<ProcessObjectsInList2>d__25>(ref <ProcessObjectsInList2>d__);
			return <ProcessObjectsInList2>d__.<>t__builder.Task;
		}

		// Token: 0x060007C9 RID: 1993 RVA: 0x000640CC File Offset: 0x000630CC
		private Task ProcessObjectsInList(string transactionName, double offsetValue)
		{
			frmInsertPlus.<ProcessObjectsInList>d__26 <ProcessObjectsInList>d__;
			<ProcessObjectsInList>d__.<>t__builder = AsyncTaskMethodBuilder.Create();
			<ProcessObjectsInList>d__.<>4__this = this;
			<ProcessObjectsInList>d__.transactionName = transactionName;
			<ProcessObjectsInList>d__.offsetValue = offsetValue;
			<ProcessObjectsInList>d__.<>1__state = -1;
			<ProcessObjectsInList>d__.<>t__builder.Start<frmInsertPlus.<ProcessObjectsInList>d__26>(ref <ProcessObjectsInList>d__);
			return <ProcessObjectsInList>d__.<>t__builder.Task;
		}

		// Token: 0x060007CA RID: 1994 RVA: 0x00064120 File Offset: 0x00063120
		private void LockAndUnLockControls(bool _IsLock)
		{
			this.InvokeSafe(delegate
			{
				this.btnAligned.Enabled = _IsLock;
				this.btnOpposed.Enabled = _IsLock;
				this.Cursor = (_IsLock ? Cursors.Default : Cursors.WaitCursor);
			});
		}

		// Token: 0x060007CB RID: 1995 RVA: 0x0000FE49 File Offset: 0x0000EE49
		private void InvokeSafe(Action action)
		{
			if (base.InvokeRequired)
			{
				base.Invoke(action);
				return;
			}
			action();
		}

		// Token: 0x060007CC RID: 1996 RVA: 0x00064154 File Offset: 0x00063154
		private InsertConstraint _GetInsertConstraint(Inventor.AssemblyDocument iamDoc, string insertName)
		{
			InsertConstraint result;
			try
			{
				object obj;
				if (iamDoc == null)
				{
					obj = null;
				}
				else
				{
					AssemblyConstraints constraints = iamDoc.ComponentDefinition.Constraints;
					obj = ((constraints != null) ? constraints[insertName] : null);
				}
				result = (InsertConstraint)obj;
			}
			catch
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060007CD RID: 1997 RVA: 0x000641A0 File Offset: 0x000631A0
		private void btnFlipConstraint_Click(object sender, EventArgs e)
		{
			frmInsertPlus.<btnFlipConstraint_Click>d__30 <btnFlipConstraint_Click>d__;
			<btnFlipConstraint_Click>d__.<>t__builder = AsyncVoidMethodBuilder.Create();
			<btnFlipConstraint_Click>d__.<>4__this = this;
			<btnFlipConstraint_Click>d__.sender = sender;
			<btnFlipConstraint_Click>d__.<>1__state = -1;
			<btnFlipConstraint_Click>d__.<>t__builder.Start<frmInsertPlus.<btnFlipConstraint_Click>d__30>(ref <btnFlipConstraint_Click>d__);
		}

		// Token: 0x060007CE RID: 1998 RVA: 0x000641DF File Offset: 0x000631DF
		private void chkLockRotation_CheckedChanged(object sender, EventArgs e)
		{
			this.LockRotation = this.chkLockRotation.Checked;
		}

		// Token: 0x060007CF RID: 1999 RVA: 0x000641F4 File Offset: 0x000631F4
		private void txtOffset_KeyPress(object sender, KeyPressEventArgs e)
		{
			System.Windows.Forms.TextBox textBox = sender as System.Windows.Forms.TextBox;
			if (textBox != null)
			{
				if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != '-')
				{
					e.Handled = true;
				}
				if (e.KeyChar == '.' && textBox.Text.IndexOf('.') >= 0)
				{
					e.Handled = true;
				}
				if (e.KeyChar == '-' && textBox.SelectionStart != 0)
				{
					e.Handled = true;
				}
			}
		}

		// Token: 0x060007D0 RID: 2000 RVA: 0x0006427C File Offset: 0x0006327C
		private void txtOffset_KeyDown(object sender, KeyEventArgs e)
		{
			frmInsertPlus.<txtOffset_KeyDown>d__33 <txtOffset_KeyDown>d__;
			<txtOffset_KeyDown>d__.<>t__builder = AsyncVoidMethodBuilder.Create();
			<txtOffset_KeyDown>d__.<>4__this = this;
			<txtOffset_KeyDown>d__.e = e;
			<txtOffset_KeyDown>d__.<>1__state = -1;
			<txtOffset_KeyDown>d__.<>t__builder.Start<frmInsertPlus.<txtOffset_KeyDown>d__33>(ref <txtOffset_KeyDown>d__);
		}

		// Token: 0x060007D1 RID: 2001 RVA: 0x000642BC File Offset: 0x000632BC
		private Task AdjustOffsetDistance(double OffsetValue)
		{
			frmInsertPlus.<AdjustOffsetDistance>d__34 <AdjustOffsetDistance>d__;
			<AdjustOffsetDistance>d__.<>t__builder = AsyncTaskMethodBuilder.Create();
			<AdjustOffsetDistance>d__.<>4__this = this;
			<AdjustOffsetDistance>d__.OffsetValue = OffsetValue;
			<AdjustOffsetDistance>d__.<>1__state = -1;
			<AdjustOffsetDistance>d__.<>t__builder.Start<frmInsertPlus.<AdjustOffsetDistance>d__34>(ref <AdjustOffsetDistance>d__);
			return <AdjustOffsetDistance>d__.<>t__builder.Task;
		}

		// Token: 0x060007D2 RID: 2002 RVA: 0x00064308 File Offset: 0x00063308
		private bool ConvertOffsetValue(string str, out double result)
		{
			result = 0.0;
			if (!string.IsNullOrWhiteSpace(this.txtOffset.Text))
			{
				result = ConvertHelper.ToDouble(this.txtOffset.Text);
				if (result != -1.0)
				{
					return true;
				}
			}
			mBox.Warning(this, "Không thể chuyển đổi thông số Offset.");
			base.ActiveControl = this.txtOffset;
			return false;
		}

		// Token: 0x060007D3 RID: 2003 RVA: 0x0006436C File Offset: 0x0006336C
		private void btnAddObjectsToList_Click(object sender, EventArgs e)
		{
			frmInsertPlus.<btnAddObjectsToList_Click>d__36 <btnAddObjectsToList_Click>d__;
			<btnAddObjectsToList_Click>d__.<>t__builder = AsyncVoidMethodBuilder.Create();
			<btnAddObjectsToList_Click>d__.<>4__this = this;
			<btnAddObjectsToList_Click>d__.<>1__state = -1;
			<btnAddObjectsToList_Click>d__.<>t__builder.Start<frmInsertPlus.<btnAddObjectsToList_Click>d__36>(ref <btnAddObjectsToList_Click>d__);
		}

		// Token: 0x060007D4 RID: 2004 RVA: 0x000643A3 File Offset: 0x000633A3
		private void lbCountSelectedObjects_Update()
		{
			this.InvokeSafe(delegate
			{
				Control control = this.lbCountSelectedObjects;
				List<string> list = this.listInsertConstraints;
				control.Text = ((list != null && list.Count > 0) ? string.Format("{0} Items", this.listInsertConstraints.Count) : null);
			});
		}

		// Token: 0x060007D5 RID: 2005 RVA: 0x000643B8 File Offset: 0x000633B8
		private void btnAttachToSelection_Click(object sender, EventArgs e)
		{
			if (this.invApp.ActiveDocumentType == Inventor.DocumentTypeEnum.kAssemblyDocumentObject)
			{
				if (this.iamDoc == null)
				{
					this.GetDoc();
				}
				double offsetValue;
				if (!this.ConvertOffsetValue(this.txtOffset.Text, out offsetValue))
				{
					return;
				}
				base.WindowState = FormWindowState.Minimized;
				Inventor.EdgeProxy edgeProxy = this.PickToGetEdge(this.str1, null);
				if (edgeProxy != null)
				{
					List<string> list = this.listInsertConstraints;
					if (list != null)
					{
						list.Clear();
					}
					this.iamIsActive = this.iamDoc.InternalName;
					new ComAwareEventInfo(typeof(ApplicationEventsSink_Event), "OnActivateDocument").AddEventHandler(this.invApp.ApplicationEvents, new ApplicationEventsSink_OnActivateDocumentEventHandler(this, (UIntPtr)ldftn(ApplicationEvents_OnActivateDocument)));
					this.PickToAttachAsync(edgeProxy, offsetValue);
				}
			}
		}

		// Token: 0x060007D6 RID: 2006 RVA: 0x00064474 File Offset: 0x00063474
		private Inventor.EdgeProxy PickToGetEdge(string promptText, Inventor.ComponentOccurrence OccurrenceOfFirstEdge = null)
		{
			Inventor.EdgeProxy edgeProxy = null;
			while (edgeProxy == null)
			{
				if (frmInsertPlus.<>o__39.<>p__0 == null)
				{
					frmInsertPlus.<>o__39.<>p__0 = CallSite<Func<CallSite, object, Inventor.EdgeProxy>>.Create(Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(Inventor.EdgeProxy), typeof(frmInsertPlus)));
				}
				Inventor.EdgeProxy edgeProxy2 = frmInsertPlus.<>o__39.<>p__0.Target(frmInsertPlus.<>o__39.<>p__0, this.invApp.CommandManager.Pick(Inventor.SelectionFilterEnum.kPartEdgeCircularFilter, promptText));
				if (edgeProxy2 == null)
				{
					break;
				}
				if (OccurrenceOfFirstEdge == null || edgeProxy2.ContainingOccurrence != OccurrenceOfFirstEdge)
				{
					edgeProxy = edgeProxy2;
				}
			}
			return edgeProxy;
		}

		// Token: 0x060007D7 RID: 2007 RVA: 0x000644F1 File Offset: 0x000634F1
		private Inventor.ComponentOccurrence GetOccurrenceContainingEdge(Inventor.EdgeProxy edge)
		{
			if (((edge != null) ? edge.ContainingOccurrence : null) == null)
			{
				return null;
			}
			return edge.ContainingOccurrence;
		}

		// Token: 0x060007D8 RID: 2008 RVA: 0x0006450C File Offset: 0x0006350C
		private void ApplicationEvents_OnActivateDocument(Inventor._Document DocumentObject, EventTimingEnum BeforeOrAfter, NameValueMap Context, out HandlingCodeEnum HandlingCode)
		{
			HandlingCode = HandlingCodeEnum.kEventNotHandled;
			if (BeforeOrAfter == EventTimingEnum.kBefore)
			{
				bool _isLock = this.invApp.ActiveDocumentType == Inventor.DocumentTypeEnum.kAssemblyDocumentObject && DocumentObject.InternalName == this.iamIsActive;
				this.InvokeSafe(delegate
				{
					this.tableLayoutPanel1.Enabled = _isLock;
				});
				HandlingCode = HandlingCodeEnum.kEventHandled;
			}
		}

		// Token: 0x060007D9 RID: 2009 RVA: 0x00064580 File Offset: 0x00063580
		private Task PickToAttachAsync(Inventor.EdgeProxy firstEdge, double OffsetValue)
		{
			frmInsertPlus.<PickToAttachAsync>d__42 <PickToAttachAsync>d__;
			<PickToAttachAsync>d__.<>t__builder = AsyncTaskMethodBuilder.Create();
			<PickToAttachAsync>d__.<>4__this = this;
			<PickToAttachAsync>d__.firstEdge = firstEdge;
			<PickToAttachAsync>d__.OffsetValue = OffsetValue;
			<PickToAttachAsync>d__.<>1__state = -1;
			<PickToAttachAsync>d__.<>t__builder.Start<frmInsertPlus.<PickToAttachAsync>d__42>(ref <PickToAttachAsync>d__);
			return <PickToAttachAsync>d__.<>t__builder.Task;
		}

		// Token: 0x060007DA RID: 2010 RVA: 0x000645D4 File Offset: 0x000635D4
		private Inventor.ComponentOccurrence CreateAndAddOccurrence(Inventor.ComponentOccurrence originOccurrence, Inventor.ComponentOccurrence occurrenceContainingFirstEdge, out bool isInSubAssembly, out int edgeIndex)
		{
			isInSubAssembly = false;
			edgeIndex = 1;
			if (originOccurrence != occurrenceContainingFirstEdge && originOccurrence.DefinitionDocumentType == Inventor.DocumentTypeEnum.kAssemblyDocumentObject)
			{
				isInSubAssembly = true;
				Inventor.ComponentOccurrencesEnumerator leafOccurrences = originOccurrence.Definition.Occurrences.get_AllLeafOccurrences(Type.Missing);
				edgeIndex = Enumerable.Range(1, leafOccurrences.Count).FirstOrDefault(delegate(int i)
				{
					object obj;
					originOccurrence.CreateGeometryProxy(leafOccurrences[i], out obj);
					return obj == occurrenceContainingFirstEdge;
				});
			}
			return this.CreateOccurrence(originOccurrence);
		}

		// Token: 0x060007DB RID: 2011 RVA: 0x00064670 File Offset: 0x00063670
		private Inventor.ComponentOccurrence GetOccurrenceProxy(Inventor.ComponentOccurrence newOccurrence, int edgeIndex)
		{
			Inventor.ComponentOccurrencesEnumerator componentOccurrencesEnumerator = newOccurrence.Definition.Occurrences.get_AllLeafOccurrences(Type.Missing);
			if (edgeIndex <= 0 || edgeIndex > componentOccurrencesEnumerator.Count)
			{
				return newOccurrence;
			}
			object obj;
			newOccurrence.CreateGeometryProxy(componentOccurrencesEnumerator[edgeIndex], out obj);
			return obj as Inventor.ComponentOccurrence;
		}

		// Token: 0x060007DC RID: 2012 RVA: 0x000646B8 File Offset: 0x000636B8
		private Inventor.ComponentOccurrence CreateOccurrence(Inventor.ComponentOccurrence originOccurrence)
		{
			if (frmInsertPlus.<>o__45.<>p__1 == null)
			{
				frmInsertPlus.<>o__45.<>p__1 = CallSite<Func<CallSite, object, string>>.Create(Binder.Convert(CSharpBinderFlags.None, typeof(string), typeof(frmInsertPlus)));
			}
			Func<CallSite, object, string> target = frmInsertPlus.<>o__45.<>p__1.Target;
			CallSite <>p__ = frmInsertPlus.<>o__45.<>p__1;
			if (frmInsertPlus.<>o__45.<>p__0 == null)
			{
				frmInsertPlus.<>o__45.<>p__0 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "FullFileName", typeof(frmInsertPlus), new CSharpArgumentInfo[]
				{
					CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
				}));
			}
			string text = target(<>p__, frmInsertPlus.<>o__45.<>p__0.Target(frmInsertPlus.<>o__45.<>p__0, originOccurrence.Definition.Document));
			if (!System.IO.File.Exists(text))
			{
				return null;
			}
			return this.iamDoc.ComponentDefinition.Occurrences.Add(text, this.invApp.TransientGeometry.CreateMatrix());
		}

		// Token: 0x060007DD RID: 2013 RVA: 0x0006478C File Offset: 0x0006378C
		private Inventor.EdgeProxy CreateEdgeProxy(Inventor.ComponentOccurrence occurrence, Inventor.EdgeProxy sourceEdge)
		{
			if (occurrence == null || sourceEdge == null)
			{
				return null;
			}
			try
			{
				object obj;
				occurrence.CreateGeometryProxy(sourceEdge.NativeObject, out obj);
				return (Inventor.EdgeProxy)obj;
			}
			catch (Exception ex)
			{
				occurrence.Delete2(true);
				mBox.Warning(this, "Lỗi tạo proxy hình học: \n" + ex.Message);
			}
			return null;
		}

		// Token: 0x060007DE RID: 2014 RVA: 0x000647EC File Offset: 0x000637EC
		private InsertConstraint AddInsertConstraint(Inventor.EdgeProxy oEdge1, Inventor.EdgeProxy oEdge2, double OffsetDistance, bool addConstraintToList)
		{
			InsertConstraint result;
			try
			{
				InsertConstraint insertConstraint = this.iamDoc.ComponentDefinition.Constraints.AddInsertConstraint2(oEdge1, oEdge2, this.AxesOpposed, OffsetDistance / 10.0, this.LockRotation, Type.Missing, Type.Missing);
				if (addConstraintToList)
				{
					this.listInsertConstraints.Add(insertConstraint.Name);
					this.lbCountSelectedObjects_Update();
				}
				Inventor.AssemblyDocument assemblyDocument = this.iamDoc;
				if (assemblyDocument != null)
				{
					assemblyDocument.Update2(true);
				}
				result = insertConstraint;
			}
			catch (Exception ex)
			{
				mBox.Warning(this, "Không thể tạo mới Insert Constraint.\nMessage error: " + ex.Message);
				result = null;
			}
			return result;
		}

		// Token: 0x060007DF RID: 2015 RVA: 0x0006489C File Offset: 0x0006389C
		private void SaveLastSettings(double offsetValue)
		{
			this.LastAxesOpposed = this.AxesOpposed;
			this.LastOffsetValue = offsetValue;
		}

		// Token: 0x060007E0 RID: 2016 RVA: 0x000648B1 File Offset: 0x000638B1
		private void RestoreUIState()
		{
			this.InvokeSafe(delegate
			{
				this.toolTip1.SetToolTip(this.txtOffset, "Nhập thông số để điều chỉnh Offset");
				base.WindowState = FormWindowState.Normal;
			});
		}

		// Token: 0x060007E1 RID: 2017 RVA: 0x000648C8 File Offset: 0x000638C8
		private Inventor.ComponentOccurrence CheckObjectIsSubAssembly(Inventor.ComponentOccurrence oOcc)
		{
			Inventor.ComponentOccurrence componentOccurrence = oOcc;
			while (componentOccurrence.ParentOccurrence != this.iamDef.ActiveOccurrence)
			{
				componentOccurrence = componentOccurrence.ParentOccurrence;
				if (componentOccurrence == null)
				{
					break;
				}
			}
			if (componentOccurrence == oOcc)
			{
				return null;
			}
			return componentOccurrence;
		}

		// Token: 0x060007E2 RID: 2018 RVA: 0x00064900 File Offset: 0x00063900
		private void btnAutomaticallyAttach_Click(object sender, EventArgs e)
		{
			frmInsertPlus.<btnAutomaticallyAttach_Click>d__51 <btnAutomaticallyAttach_Click>d__;
			<btnAutomaticallyAttach_Click>d__.<>t__builder = AsyncVoidMethodBuilder.Create();
			<btnAutomaticallyAttach_Click>d__.<>4__this = this;
			<btnAutomaticallyAttach_Click>d__.<>1__state = -1;
			<btnAutomaticallyAttach_Click>d__.<>t__builder.Start<frmInsertPlus.<btnAutomaticallyAttach_Click>d__51>(ref <btnAutomaticallyAttach_Click>d__);
		}

		// Token: 0x060007E3 RID: 2019 RVA: 0x00064938 File Offset: 0x00063938
		private Task FindSimilarHolesOnFaceAsync(Inventor.EdgeProxy firstEdge, Inventor.EdgeProxy edgeOnHole)
		{
			frmInsertPlus.<FindSimilarHolesOnFaceAsync>d__52 <FindSimilarHolesOnFaceAsync>d__;
			<FindSimilarHolesOnFaceAsync>d__.<>t__builder = AsyncTaskMethodBuilder.Create();
			<FindSimilarHolesOnFaceAsync>d__.<>4__this = this;
			<FindSimilarHolesOnFaceAsync>d__.edgeOnHole = edgeOnHole;
			<FindSimilarHolesOnFaceAsync>d__.<>1__state = -1;
			<FindSimilarHolesOnFaceAsync>d__.<>t__builder.Start<frmInsertPlus.<FindSimilarHolesOnFaceAsync>d__52>(ref <FindSimilarHolesOnFaceAsync>d__);
			return <FindSimilarHolesOnFaceAsync>d__.<>t__builder.Task;
		}

		// Token: 0x060007E4 RID: 2020 RVA: 0x00064984 File Offset: 0x00063984
		private void RemoveConstrainedEdges(Inventor.EdgeProxy edgeOnHole)
		{
			if (this.oCircleCollection.Count == 0)
			{
				return;
			}
			Inventor.ComponentOccurrence componentOccurrence = edgeOnHole.ContainingOccurrence.ParentOccurrence ?? edgeOnHole.ContainingOccurrence;
			if (componentOccurrence == null)
			{
				return;
			}
			Parallel.ForEach<AssemblyConstraint>(componentOccurrence.Constraints.OfType<AssemblyConstraint>(), delegate(AssemblyConstraint constraint)
			{
				if (constraint.Suppressed || constraint.Type != Inventor.ObjectTypeEnum.kInsertConstraintObject)
				{
					return;
				}
				try
				{
					if (frmInsertPlus.<>o__53.<>p__4 == null)
					{
						frmInsertPlus.<>o__53.<>p__4 = CallSite<Func<CallSite, object, bool>>.Create(Binder.UnaryOperation(CSharpBinderFlags.None, ExpressionType.IsTrue, typeof(frmInsertPlus), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					Func<CallSite, object, bool> target = frmInsertPlus.<>o__53.<>p__4.Target;
					CallSite <>p__ = frmInsertPlus.<>o__53.<>p__4;
					if (frmInsertPlus.<>o__53.<>p__0 == null)
					{
						frmInsertPlus.<>o__53.<>p__0 = CallSite<Func<CallSite, object, object, object>>.Create(Binder.BinaryOperation(CSharpBinderFlags.None, ExpressionType.NotEqual, typeof(frmInsertPlus), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.Constant, null)
						}));
					}
					object obj = frmInsertPlus.<>o__53.<>p__0.Target(frmInsertPlus.<>o__53.<>p__0, constraint.EntityOne, null);
					if (frmInsertPlus.<>o__53.<>p__3 == null)
					{
						frmInsertPlus.<>o__53.<>p__3 = CallSite<Func<CallSite, object, bool>>.Create(Binder.UnaryOperation(CSharpBinderFlags.None, ExpressionType.IsFalse, typeof(frmInsertPlus), new CSharpArgumentInfo[]
						{
							CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
						}));
					}
					object arg2;
					if (!frmInsertPlus.<>o__53.<>p__3.Target(frmInsertPlus.<>o__53.<>p__3, obj))
					{
						if (frmInsertPlus.<>o__53.<>p__2 == null)
						{
							frmInsertPlus.<>o__53.<>p__2 = CallSite<Func<CallSite, object, object, object>>.Create(Binder.BinaryOperation(CSharpBinderFlags.BinaryOperationLogical, ExpressionType.And, typeof(frmInsertPlus), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
							}));
						}
						Func<CallSite, object, object, object> target2 = frmInsertPlus.<>o__53.<>p__2.Target;
						CallSite <>p__2 = frmInsertPlus.<>o__53.<>p__2;
						object arg = obj;
						if (frmInsertPlus.<>o__53.<>p__1 == null)
						{
							frmInsertPlus.<>o__53.<>p__1 = CallSite<Func<CallSite, object, object, object>>.Create(Binder.BinaryOperation(CSharpBinderFlags.None, ExpressionType.NotEqual, typeof(frmInsertPlus), new CSharpArgumentInfo[]
							{
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
								CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.Constant, null)
							}));
						}
						arg2 = target2(<>p__2, arg, frmInsertPlus.<>o__53.<>p__1.Target(frmInsertPlus.<>o__53.<>p__1, constraint.EntityTwo, null));
					}
					else
					{
						arg2 = obj;
					}
					if (target(<>p__, arg2))
					{
						Inventor.Edge edgeFromCollection = this.GetEdgeFromCollection(constraint, edgeOnHole);
						if (edgeFromCollection != null)
						{
							Inventor.ObjectCollection obj2 = this.oCircleCollection;
							lock (obj2)
							{
								this.oCircleCollection.RemoveByObject(edgeFromCollection);
							}
						}
					}
				}
				catch
				{
				}
			});
		}

		// Token: 0x060007E5 RID: 2021 RVA: 0x000649F4 File Offset: 0x000639F4
		private Inventor.Edge GetEdgeFromCollection(AssemblyConstraint constraint, Inventor.EdgeProxy edgeOnHole)
		{
			return this.oCircleCollection.AsParallel().OfType<Inventor.Edge>().FirstOrDefault(delegate(Inventor.Edge edge)
			{
				if (frmInsertPlus.<>o__54.<>p__0 == null)
				{
					frmInsertPlus.<>o__54.<>p__0 = CallSite<Func<CallSite, object, Circle>>.Create(Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(Circle), typeof(frmInsertPlus)));
				}
				Inventor.Point center = frmInsertPlus.<>o__54.<>p__0.Target(frmInsertPlus.<>o__54.<>p__0, edge.Geometry).Center;
				if (frmInsertPlus.<>o__54.<>p__3 == null)
				{
					frmInsertPlus.<>o__54.<>p__3 = CallSite<Func<CallSite, object, Inventor.Point>>.Create(Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(Inventor.Point), typeof(frmInsertPlus)));
				}
				Func<CallSite, object, Inventor.Point> target = frmInsertPlus.<>o__54.<>p__3.Target;
				CallSite <>p__ = frmInsertPlus.<>o__54.<>p__3;
				if (frmInsertPlus.<>o__54.<>p__2 == null)
				{
					frmInsertPlus.<>o__54.<>p__2 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "Center", typeof(frmInsertPlus), new CSharpArgumentInfo[]
					{
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
					}));
				}
				Func<CallSite, object, object> target2 = frmInsertPlus.<>o__54.<>p__2.Target;
				CallSite <>p__2 = frmInsertPlus.<>o__54.<>p__2;
				if (frmInsertPlus.<>o__54.<>p__1 == null)
				{
					frmInsertPlus.<>o__54.<>p__1 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "Geometry", typeof(frmInsertPlus), new CSharpArgumentInfo[]
					{
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
					}));
				}
				Inventor.Point point = target(<>p__, target2(<>p__2, frmInsertPlus.<>o__54.<>p__1.Target(frmInsertPlus.<>o__54.<>p__1, constraint.EntityOne)));
				if (frmInsertPlus.<>o__54.<>p__6 == null)
				{
					frmInsertPlus.<>o__54.<>p__6 = CallSite<Func<CallSite, object, Inventor.Point>>.Create(Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(Inventor.Point), typeof(frmInsertPlus)));
				}
				Func<CallSite, object, Inventor.Point> target3 = frmInsertPlus.<>o__54.<>p__6.Target;
				CallSite <>p__3 = frmInsertPlus.<>o__54.<>p__6;
				if (frmInsertPlus.<>o__54.<>p__5 == null)
				{
					frmInsertPlus.<>o__54.<>p__5 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "Center", typeof(frmInsertPlus), new CSharpArgumentInfo[]
					{
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
					}));
				}
				Func<CallSite, object, object> target4 = frmInsertPlus.<>o__54.<>p__5.Target;
				CallSite <>p__4 = frmInsertPlus.<>o__54.<>p__5;
				if (frmInsertPlus.<>o__54.<>p__4 == null)
				{
					frmInsertPlus.<>o__54.<>p__4 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "Geometry", typeof(frmInsertPlus), new CSharpArgumentInfo[]
					{
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
					}));
				}
				Inventor.Point point2 = target3(<>p__3, target4(<>p__4, frmInsertPlus.<>o__54.<>p__4.Target(frmInsertPlus.<>o__54.<>p__4, constraint.EntityTwo)));
				if (frmInsertPlus.<>o__54.<>p__8 == null)
				{
					frmInsertPlus.<>o__54.<>p__8 = CallSite<Func<CallSite, object, Inventor.Point>>.Create(Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(Inventor.Point), typeof(frmInsertPlus)));
				}
				Func<CallSite, object, Inventor.Point> target5 = frmInsertPlus.<>o__54.<>p__8.Target;
				CallSite <>p__5 = frmInsertPlus.<>o__54.<>p__8;
				if (frmInsertPlus.<>o__54.<>p__7 == null)
				{
					frmInsertPlus.<>o__54.<>p__7 = CallSite<Func<CallSite, object, object>>.Create(Binder.GetMember(CSharpBinderFlags.None, "Center", typeof(frmInsertPlus), new CSharpArgumentInfo[]
					{
						CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
					}));
				}
				Inventor.Point point3 = target5(<>p__5, frmInsertPlus.<>o__54.<>p__7.Target(frmInsertPlus.<>o__54.<>p__7, edgeOnHole.Geometry));
				return (Math.Round(center.DistanceTo(point), 5) == 0.0 || Math.Round(center.DistanceTo(point2), 5) == 0.0) && Math.Round(center.DistanceTo(point3), 5) != 0.0;
			});
		}

		// Token: 0x060007E6 RID: 2022 RVA: 0x00064A38 File Offset: 0x00063A38
		private Task CreateConstraintsForCollection(double offsetValue, Inventor.EdgeProxy firstEdge)
		{
			frmInsertPlus.<CreateConstraintsForCollection>d__55 <CreateConstraintsForCollection>d__;
			<CreateConstraintsForCollection>d__.<>t__builder = AsyncTaskMethodBuilder.Create();
			<CreateConstraintsForCollection>d__.<>4__this = this;
			<CreateConstraintsForCollection>d__.offsetValue = offsetValue;
			<CreateConstraintsForCollection>d__.firstEdge = firstEdge;
			<CreateConstraintsForCollection>d__.<>1__state = -1;
			<CreateConstraintsForCollection>d__.<>t__builder.Start<frmInsertPlus.<CreateConstraintsForCollection>d__55>(ref <CreateConstraintsForCollection>d__);
			return <CreateConstraintsForCollection>d__.<>t__builder.Task;
		}

		// Token: 0x060007E7 RID: 2023 RVA: 0x00064A8C File Offset: 0x00063A8C
		private bool ProcessEdgeProxy(Inventor.ComponentOccurrence originOccurrence, Inventor.EdgeProxy firstEdge, Inventor.ComponentOccurrence containingOccurrence, Inventor.EdgeProxy edgeProxy, double offsetValue)
		{
			try
			{
				bool flag;
				int edgeIndex;
				Inventor.ComponentOccurrence componentOccurrence = this.CreateAndAddOccurrence(originOccurrence, containingOccurrence, out flag, out edgeIndex);
				if (componentOccurrence == null)
				{
					mBox.Warning(this, string.Concat(new string[]
					{
						"Không thể thêm \"",
						containingOccurrence._DisplayName,
						"\" vào Assembly \"",
						this.iamDoc.DisplayName,
						"\""
					}));
					return false;
				}
				if (flag)
				{
					componentOccurrence = this.GetOccurrenceProxy(componentOccurrence, edgeIndex);
				}
				Inventor.EdgeProxy edgeProxy2 = this.CreateEdgeProxy(componentOccurrence, firstEdge);
				if (edgeProxy2 != null || edgeProxy != null)
				{
					return this.AddInsertConstraint(edgeProxy2, edgeProxy, offsetValue, true) != null;
				}
			}
			catch (Exception ex)
			{
				mBox.Warning(this, "Lỗi trong ProcessEdgeProxy: " + ex.Message);
			}
			return false;
		}

		// Token: 0x060007E8 RID: 2024 RVA: 0x00064B50 File Offset: 0x00063B50
		private Inventor.Transaction StartTransactionSafe(Inventor._Document document, string transactionName)
		{
			Inventor.Transaction result;
			try
			{
				result = this.invApp.TransactionManager.StartTransaction(document, transactionName);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("Không thể khởi tạo giao dịch: " + ex.Message, ex);
			}
			return result;
		}

		// Token: 0x060007E9 RID: 2025 RVA: 0x00064B9C File Offset: 0x00063B9C
		private void InitializeComponent()
		{
			this.components = new Container();
			ComponentResourceManager componentResourceManager = new ComponentResourceManager(typeof(frmInsertPlus));
			this.txtOffset = new System.Windows.Forms.TextBox();
			this.chkLockRotation = new CheckBox();
			this.btnAutomaticallyAttach = new Button();
			this.btnAttachToSelection = new Button();
			this.btnAligned = new Button();
			this.btnOpposed = new Button();
			this.toolTip1 = new ToolTip(this.components);
			this.btnAddObjectsToList = new Button();
			this.tableLayoutPanel1 = new TableLayoutPanel();
			this.tableLayoutPanel4 = new TableLayoutPanel();
			this.panel2 = new Panel();
			this.lbCountSelectedObjects = new Label();
			this.label5 = new Label();
			this.tableLayoutPanel2 = new TableLayoutPanel();
			this.panel1 = new Panel();
			this.label4 = new Label();
			this.label3 = new Label();
			this.tableLayoutPanel3 = new TableLayoutPanel();
			this.label1 = new Label();
			this.panelBackground = new Panel();
			this.tableLayoutPanel1.SuspendLayout();
			this.tableLayoutPanel4.SuspendLayout();
			this.panel2.SuspendLayout();
			this.tableLayoutPanel2.SuspendLayout();
			this.panel1.SuspendLayout();
			this.tableLayoutPanel3.SuspendLayout();
			this.panelBackground.SuspendLayout();
			base.SuspendLayout();
			this.txtOffset.Dock = DockStyle.Top;
			this.txtOffset.Location = new System.Drawing.Point(0, 0);
			this.txtOffset.Name = "txtOffset";
			this.txtOffset.Size = new Size(124, 22);
			this.txtOffset.TabIndex = 3;
			this.txtOffset.Text = "0.000";
			this.txtOffset.KeyDown += this.txtOffset_KeyDown;
			this.txtOffset.KeyPress += this.txtOffset_KeyPress;
			this.chkLockRotation.AutoSize = true;
			this.chkLockRotation.Checked = true;
			this.chkLockRotation.CheckState = CheckState.Checked;
			this.chkLockRotation.Dock = DockStyle.Bottom;
			this.chkLockRotation.Location = new System.Drawing.Point(0, 36);
			this.chkLockRotation.Name = "chkLockRotation";
			this.chkLockRotation.Size = new Size(124, 20);
			this.chkLockRotation.TabIndex = 1;
			this.chkLockRotation.Text = "Lock Rotation?";
			this.toolTip1.SetToolTip(this.chkLockRotation, "Khóa không cho hardware xoay");
			this.chkLockRotation.UseVisualStyleBackColor = true;
			this.chkLockRotation.CheckedChanged += this.chkLockRotation_CheckedChanged;
			this.btnAutomaticallyAttach.Dock = DockStyle.Fill;
			this.btnAutomaticallyAttach.Image = Resources.btn_Attach_16;
			this.btnAutomaticallyAttach.ImageAlign = ContentAlignment.MiddleRight;
			this.btnAutomaticallyAttach.Location = new System.Drawing.Point(175, 19);
			this.btnAutomaticallyAttach.Name = "btnAutomaticallyAttach";
			this.btnAutomaticallyAttach.Size = new Size(167, 42);
			this.btnAutomaticallyAttach.TabIndex = 3;
			this.btnAutomaticallyAttach.Text = "Automatically Attach";
			this.btnAutomaticallyAttach.TextAlign = ContentAlignment.MiddleLeft;
			this.toolTip1.SetToolTip(this.btnAutomaticallyAttach, "Tự động tìm các lỗ có cạnh tương đồng với lỗ được chọn.\r\nAutomatically attach holes similar to the selected hole.");
			this.btnAutomaticallyAttach.UseVisualStyleBackColor = true;
			this.btnAutomaticallyAttach.Click += this.btnAutomaticallyAttach_Click;
			this.btnAttachToSelection.Dock = DockStyle.Fill;
			this.btnAttachToSelection.Image = Resources.btn_Select_16;
			this.btnAttachToSelection.ImageAlign = ContentAlignment.MiddleRight;
			this.btnAttachToSelection.Location = new System.Drawing.Point(3, 19);
			this.btnAttachToSelection.Name = "btnAttachToSelection";
			this.btnAttachToSelection.Size = new Size(166, 42);
			this.btnAttachToSelection.TabIndex = 3;
			this.btnAttachToSelection.Text = "Attach to Selection";
			this.btnAttachToSelection.TextAlign = ContentAlignment.MiddleLeft;
			this.toolTip1.SetToolTip(this.btnAttachToSelection, "Chọn lỗ để gắn hardware.\r\nSelect holes to mounting hardware.");
			this.btnAttachToSelection.UseVisualStyleBackColor = true;
			this.btnAttachToSelection.Click += this.btnAttachToSelection_Click;
			this.btnAligned.Dock = DockStyle.Fill;
			this.btnAligned.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(192, 255, 192);
			this.btnAligned.Image = Resources.Opposed;
			this.btnAligned.Location = new System.Drawing.Point(240, 19);
			this.btnAligned.Name = "btnAligned";
			this.btnAligned.Size = new Size(102, 56);
			this.btnAligned.TabIndex = 4;
			this.toolTip1.SetToolTip(this.btnAligned, "Aligned");
			this.btnAligned.UseVisualStyleBackColor = true;
			this.btnAligned.Click += this.btnFlipConstraint_Click;
			this.btnOpposed.Dock = DockStyle.Fill;
			this.btnOpposed.Image = Resources.Aligned;
			this.btnOpposed.Location = new System.Drawing.Point(133, 19);
			this.btnOpposed.Name = "btnOpposed";
			this.btnOpposed.Size = new Size(101, 56);
			this.btnOpposed.TabIndex = 4;
			this.toolTip1.SetToolTip(this.btnOpposed, "Opposed");
			this.btnOpposed.UseVisualStyleBackColor = true;
			this.btnOpposed.Click += this.btnFlipConstraint_Click;
			this.toolTip1.AutoPopDelay = 5000;
			this.toolTip1.InitialDelay = 500;
			this.toolTip1.IsBalloon = true;
			this.toolTip1.ReshowDelay = 200;
			this.toolTip1.ShowAlways = true;
			this.toolTip1.ToolTipIcon = ToolTipIcon.Info;
			this.toolTip1.ToolTipTitle = "I N F O R M A T I O N";
			this.btnAddObjectsToList.Dock = DockStyle.Fill;
			this.btnAddObjectsToList.Image = (Image)componentResourceManager.GetObject("btnAddObjectsToList.Image");
			this.btnAddObjectsToList.ImageAlign = ContentAlignment.MiddleLeft;
			this.btnAddObjectsToList.Location = new System.Drawing.Point(239, 3);
			this.btnAddObjectsToList.Name = "btnAddObjectsToList";
			this.btnAddObjectsToList.Size = new Size(103, 32);
			this.btnAddObjectsToList.TabIndex = 3;
			this.btnAddObjectsToList.Text = "Add Object";
			this.btnAddObjectsToList.TextAlign = ContentAlignment.MiddleRight;
			this.toolTip1.SetToolTip(this.btnAddObjectsToList, "Chọn các đối tượng \"Insert Constraint\" \r\ncần điều chỉnh thông số offset và đảo chiều.");
			this.btnAddObjectsToList.UseVisualStyleBackColor = true;
			this.btnAddObjectsToList.Click += this.btnAddObjectsToList_Click;
			this.tableLayoutPanel1.ColumnCount = 1;
			this.tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
			this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel4, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel3, 0, 2);
			this.tableLayoutPanel1.Dock = DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 3;
			this.tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 84f));
			this.tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
			this.tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
			this.tableLayoutPanel1.Size = new Size(351, 198);
			this.tableLayoutPanel1.TabIndex = 8;
			this.tableLayoutPanel4.ColumnCount = 2;
			this.tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68.4058f));
			this.tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31.5942f));
			this.tableLayoutPanel4.Controls.Add(this.btnAddObjectsToList, 1, 0);
			this.tableLayoutPanel4.Controls.Add(this.panel2, 0, 0);
			this.tableLayoutPanel4.Dock = DockStyle.Fill;
			this.tableLayoutPanel4.Location = new System.Drawing.Point(3, 87);
			this.tableLayoutPanel4.Name = "tableLayoutPanel4";
			this.tableLayoutPanel4.RowCount = 1;
			this.tableLayoutPanel4.RowStyles.Add(new RowStyle());
			this.tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
			this.tableLayoutPanel4.Size = new Size(345, 38);
			this.tableLayoutPanel4.TabIndex = 2;
			this.panel2.Controls.Add(this.lbCountSelectedObjects);
			this.panel2.Controls.Add(this.label5);
			this.panel2.Dock = DockStyle.Fill;
			this.panel2.Location = new System.Drawing.Point(3, 3);
			this.panel2.Name = "panel2";
			this.panel2.Size = new Size(230, 32);
			this.panel2.TabIndex = 4;
			this.lbCountSelectedObjects.Dock = DockStyle.Fill;
			this.lbCountSelectedObjects.ForeColor = System.Drawing.Color.IndianRed;
			this.lbCountSelectedObjects.Location = new System.Drawing.Point(148, 0);
			this.lbCountSelectedObjects.Name = "lbCountSelectedObjects";
			this.lbCountSelectedObjects.Size = new Size(82, 32);
			this.lbCountSelectedObjects.TabIndex = 124;
			this.lbCountSelectedObjects.TextAlign = ContentAlignment.MiddleLeft;
			this.label5.Dock = DockStyle.Left;
			this.label5.Font = new Font("Microsoft Sans Serif", 9.75f, FontStyle.Regular, GraphicsUnit.Point, 163);
			this.label5.Location = new System.Drawing.Point(0, 0);
			this.label5.Name = "label5";
			this.label5.Size = new Size(148, 32);
			this.label5.TabIndex = 125;
			this.label5.Text = "Insert Constraint Items:";
			this.label5.TextAlign = ContentAlignment.MiddleLeft;
			this.tableLayoutPanel2.ColumnCount = 3;
			this.tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130f));
			this.tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
			this.tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
			this.tableLayoutPanel2.Controls.Add(this.panel1, 0, 1);
			this.tableLayoutPanel2.Controls.Add(this.btnAligned, 2, 1);
			this.tableLayoutPanel2.Controls.Add(this.label4, 0, 0);
			this.tableLayoutPanel2.Controls.Add(this.btnOpposed, 1, 1);
			this.tableLayoutPanel2.Controls.Add(this.label3, 1, 0);
			this.tableLayoutPanel2.Dock = DockStyle.Fill;
			this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 3);
			this.tableLayoutPanel2.Name = "tableLayoutPanel2";
			this.tableLayoutPanel2.RowCount = 2;
			this.tableLayoutPanel2.RowStyles.Add(new RowStyle());
			this.tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
			this.tableLayoutPanel2.Size = new Size(345, 78);
			this.tableLayoutPanel2.TabIndex = 0;
			this.panel1.Controls.Add(this.chkLockRotation);
			this.panel1.Controls.Add(this.txtOffset);
			this.panel1.Dock = DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(3, 19);
			this.panel1.Name = "panel1";
			this.panel1.Size = new Size(124, 56);
			this.panel1.TabIndex = 124;
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(3, 0);
			this.label4.Name = "label4";
			this.label4.Size = new Size(41, 16);
			this.label4.TabIndex = 124;
			this.label4.Text = "Offset";
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(133, 0);
			this.label3.Name = "label3";
			this.label3.Size = new Size(55, 16);
			this.label3.TabIndex = 124;
			this.label3.Text = "Solution";
			this.tableLayoutPanel3.ColumnCount = 2;
			this.tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
			this.tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
			this.tableLayoutPanel3.Controls.Add(this.btnAutomaticallyAttach, 1, 1);
			this.tableLayoutPanel3.Controls.Add(this.label1, 0, 0);
			this.tableLayoutPanel3.Controls.Add(this.btnAttachToSelection, 0, 1);
			this.tableLayoutPanel3.Dock = DockStyle.Fill;
			this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 131);
			this.tableLayoutPanel3.Name = "tableLayoutPanel3";
			this.tableLayoutPanel3.RowCount = 2;
			this.tableLayoutPanel3.RowStyles.Add(new RowStyle());
			this.tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
			this.tableLayoutPanel3.Size = new Size(345, 64);
			this.tableLayoutPanel3.TabIndex = 1;
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 0);
			this.label1.Name = "label1";
			this.label1.Size = new Size(62, 16);
			this.label1.TabIndex = 124;
			this.label1.Text = "Apply To";
			this.panelBackground.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
			this.panelBackground.BorderStyle = BorderStyle.FixedSingle;
			this.panelBackground.Controls.Add(this.tableLayoutPanel1);
			this.panelBackground.Location = new System.Drawing.Point(10, 10);
			this.panelBackground.Name = "panelBackground";
			this.panelBackground.Size = new Size(353, 200);
			this.panelBackground.TabIndex = 124;
			base.AutoScaleDimensions = new SizeF(8f, 16f);
			base.AutoScaleMode = AutoScaleMode.Font;
			this.BackColor = SystemColors.Window;
			base.ClientSize = new Size(373, 221);
			base.Controls.Add(this.panelBackground);
			this.Font = new Font("Microsoft Sans Serif", 9.75f, FontStyle.Regular, GraphicsUnit.Point, 163);
			base.FormBorderStyle = FormBorderStyle.FixedSingle;
			base.Icon = (Icon)componentResourceManager.GetObject("$this.Icon");
			base.KeyPreview = true;
			base.Margin = new Padding(4);
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			this.MinimumSize = new Size(380, 260);
			base.Name = "frmInsertPlus";
			base.ShowInTaskbar = false;
			this.Text = "Insert Plus+";
			base.Activated += this.frmInsertPlus_Activated;
			base.FormClosed += this.frmInsertPlus_FormClosed;
			base.Load += this.frmInsertPlus_Load;
			base.KeyDown += this.frmInsertPlus_KeyDown;
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel4.ResumeLayout(false);
			this.panel2.ResumeLayout(false);
			this.tableLayoutPanel2.ResumeLayout(false);
			this.tableLayoutPanel2.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.tableLayoutPanel3.ResumeLayout(false);
			this.tableLayoutPanel3.PerformLayout();
			this.panelBackground.ResumeLayout(false);
			base.ResumeLayout(false);
		}

		// Token: 0x04000633 RID: 1587
		private Inventor.Application invApp;

		// Token: 0x04000634 RID: 1588
		private Inventor.AssemblyDocument iamDoc;

		// Token: 0x04000635 RID: 1589
		private Inventor.AssemblyComponentDefinition iamDef;

		// Token: 0x04000636 RID: 1590
		private List<string> listInsertConstraints;

		// Token: 0x04000637 RID: 1591
		private bool LockRotation = true;

		// Token: 0x04000638 RID: 1592
		private string str1 = "Select first edge";

		// Token: 0x04000639 RID: 1593
		private string str2 = "Attach to hole";

		// Token: 0x0400063B RID: 1595
		private bool AxesOpposed = true;

		// Token: 0x0400063D RID: 1597
		private double LastOffsetValue;

		// Token: 0x0400063E RID: 1598
		private Inventor.ObjectCollection oCircleCollection;

		// Token: 0x0400063F RID: 1599
		private CheckBox chkLockRotation;

		// Token: 0x04000640 RID: 1600
		private System.Windows.Forms.TextBox txtOffset;

		// Token: 0x04000641 RID: 1601
		private Button btnAligned;

		// Token: 0x04000642 RID: 1602
		private Button btnOpposed;

		// Token: 0x04000643 RID: 1603
		private Button btnAutomaticallyAttach;

		// Token: 0x04000644 RID: 1604
		private Button btnAttachToSelection;

		// Token: 0x04000645 RID: 1605
		private ToolTip toolTip1;

		// Token: 0x04000646 RID: 1606
		private IContainer components;

		// Token: 0x04000647 RID: 1607
		private TableLayoutPanel tableLayoutPanel1;

		// Token: 0x04000648 RID: 1608
		private Label label4;

		// Token: 0x04000649 RID: 1609
		private TableLayoutPanel tableLayoutPanel2;

		// Token: 0x0400064A RID: 1610
		private Label label3;

		// Token: 0x0400064B RID: 1611
		private Panel panel1;

		// Token: 0x0400064C RID: 1612
		private Panel panelBackground;

		// Token: 0x0400064D RID: 1613
		private TableLayoutPanel tableLayoutPanel3;

		// Token: 0x0400064E RID: 1614
		private Label label1;

		// Token: 0x0400064F RID: 1615
		private TableLayoutPanel tableLayoutPanel4;

		// Token: 0x04000650 RID: 1616
		private Label lbCountSelectedObjects;

		// Token: 0x04000651 RID: 1617
		private Button btnAddObjectsToList;

		// Token: 0x04000652 RID: 1618
		private Panel panel2;

		// Token: 0x04000653 RID: 1619
		private Label label5;
	}
}
