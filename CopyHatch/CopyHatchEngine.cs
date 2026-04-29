using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Inventor;

namespace VinTed.CopyHatch
{
    /// <summary>
    /// Engine xử lý logic Copy Hatch Pattern giữa các chi tiết trong Section View.
    /// Port 1:1 từ iLogic CopyHatch.iLogicVb — sử dụng late binding qua reflection
    /// để đảm bảo tương thích COM giống hệt VB iLogic.
    /// </summary>
    public class CopyHatchEngine
    {
        private readonly Application _invApp;
        private readonly DrawingDocument _drawDoc;

        private enum HatchPickStatus
        {
            Found,
            Cancelled,
            Retry
        }

        private sealed class HatchPickResult
        {
            public HatchPickStatus Status { get; private set; }
            public DrawingViewHatchRegion Hatch { get; private set; }

            private HatchPickResult(HatchPickStatus status, DrawingViewHatchRegion hatch)
            {
                Status = status;
                Hatch = hatch;
            }

            public static HatchPickResult Found(DrawingViewHatchRegion hatch)
            {
                return new HatchPickResult(HatchPickStatus.Found, hatch);
            }

            public static HatchPickResult Cancelled()
            {
                return new HatchPickResult(HatchPickStatus.Cancelled, null);
            }

            public static HatchPickResult Retry()
            {
                return new HatchPickResult(HatchPickStatus.Retry, null);
            }
        }

        /// <summary>
        /// Đếm số lượng hatch đã copy thành công.
        /// </summary>
        public int CopiedCount { get; private set; }

        public CopyHatchEngine(Application invApp, DrawingDocument drawDoc)
        {
            _invApp = invApp;
            _drawDoc = drawDoc;
            CopiedCount = 0;
        }

        /// <summary>
        /// Thực thi toàn bộ workflow Copy Hatch (blocking trên STA thread).
        /// Gọi trực tiếp từ button handler, KHÔNG cần WPF window.
        /// </summary>
        public void Execute()
        {
            // BƯỚC 1: Chọn source hatch qua cạnh
            DrawingViewHatchRegion sourceHatch = null;
            while (sourceHatch == null)
            {
                HatchPickResult sourcePick = PickHatchByCurve(
                    "BƯỚC 1: Chọn MỘT CẠNH của chi tiết MẪU (Source). Nhấn ESC để thoát");

                if (sourcePick.Status == HatchPickStatus.Cancelled)
                {
                    return;
                }

                if (sourcePick.Status == HatchPickStatus.Found)
                {
                    sourceHatch = sourcePick.Hatch;
                }
            }

            // Lấy thông số từ mặt cắt mẫu — dùng late binding giống iLogic
            object sourcePattern = GetComProperty(sourceHatch, "Pattern");
            object sourceScale = GetComProperty(sourceHatch, "Scale");
            object sourceAngle = GetComProperty(sourceHatch, "Angle");
            object sourceColor = null;
            try
            {
                sourceColor = GetComProperty(sourceHatch, "Color");
            }
            catch (Exception) { }

            // BƯỚC 2: Vòng lặp liên tục chọn đích
            bool keepGoing = true;
            while (keepGoing)
            {
                HatchPickResult targetPick = PickHatchByCurve(
                    "BƯỚC 2: Chọn MỘT CẠNH của chi tiết ĐÍCH (Nhấn ESC để thoát)");

                if (targetPick.Status == HatchPickStatus.Cancelled)
                {
                    keepGoing = false;
                    continue;
                }

                if (targetPick.Status == HatchPickStatus.Retry)
                {
                    continue;
                }

                DrawingViewHatchRegion targetHatch = targetPick.Hatch;
                Transaction txn = null;
                try
                {
                    // BẮT BUỘC: Wrap trong Transaction
                    txn = _invApp.TransactionManager.StartTransaction(
                        _invApp.ActiveDocument, "VinTed Copy Hatch");

                    System.Collections.Generic.List<string> errs = new System.Collections.Generic.List<string>();

                    try 
                    { 
                        if (targetHatch.ByMaterial) 
                            targetHatch.ByMaterial = false; 
                    }
                    catch (Exception ex) { errs.Add("ByMaterial: " + ex.Message); }

                    try { targetHatch.Pattern = sourcePattern as DrawingHatchPattern; }
                    catch (Exception ex) { errs.Add("Pattern: " + ex.Message); }

                    try { targetHatch.Scale = Convert.ToDouble(sourceScale); }
                    catch (Exception ex) { errs.Add("Scale: " + ex.Message); }

                    try { targetHatch.Angle = Convert.ToDouble(sourceAngle); }
                    catch (Exception ex) { errs.Add("Angle: " + ex.Message); }

                    if (sourceColor != null)
                    {
                        try { targetHatch.Color = sourceColor as Color; }
                        catch (Exception ex) { errs.Add("Color: " + ex.Message); }
                    }

                    if (errs.Count > 0)
                    {
                        throw new Exception(string.Join("\n", errs));
                    }

                    txn.End();
                    CopiedCount++;
                }
                catch (Exception ex)
                {
                    try { if (txn != null) txn.Abort(); } catch (Exception) { }
                    System.Windows.MessageBox.Show(
                        "Lỗi khi áp dụng pattern:\n" + ex.Message,
                        "VinTed — Debug Info",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }

            // Không gọi ActiveDocument.Update() toàn bộ sau khi nhấn ESC.
            // Inventor đã invalidate drawing view sau mỗi Transaction.End(); ép update ở đây
            // có thể làm UI đơ vài giây trước khi hiện thông báo hoàn tất trên bản vẽ lớn.
        }

        /// <summary>
        /// Chọn Hatch Region thông qua cạnh (DrawingCurveSegment) trong Drawing View.
        /// Port chính xác từ iLogic PickHatchByCurve — dùng late binding cho COM navigation.
        /// </summary>
        private HatchPickResult PickHatchByCurve(string prompt)
        {
            try
            {
                CommandManager cmdMgr = _invApp.CommandManager;

                // Lọc chỉ cho phép chọn Cạnh (Curve) trong bản vẽ
                object pickedObj = cmdMgr.Pick(
                    SelectionFilterEnum.kDrawingCurveSegmentFilter, prompt);

                if (pickedObj == null)
                {
                    return HatchPickResult.Cancelled();
                }

                DrawingCurveSegment segment = (DrawingCurveSegment)pickedObj;
                DrawingCurve curve = segment.Parent;

                // Lấy DrawingView — dùng late binding giống VB: oCurve.Parent
                DrawingView view = null;
                try
                {
                    // Early binding trước
                    view = curve.Parent;
                }
                catch (Exception)
                {
                    // Fallback: late binding qua reflection
                    object viewObj = GetComProperty(curve, "Parent");
                    if (viewObj is DrawingView)
                    {
                        view = (DrawingView)viewObj;
                    }
                }

                if (view == null)
                {
                    ShowRetryWarning(
                        "Không nhận diện được hình chiếu từ cạnh vừa chọn.\nVui lòng chọn một cạnh thuộc Section View.");
                    return HatchPickResult.Retry();
                }

                // Truy xuất SurfaceBody từ ModelGeometry — DÙNG LATE BINDING giống VB
                SurfaceBody surfaceBody = null;
                try
                {
                    object modelGeom = curve.ModelGeometry;

                    if (modelGeom is Edge)
                    {
                        // VB iLogic: oSurfaceBody = oModelGeom.Parent.Parent
                        // Late binding qua reflection (IDispatch) — giống hệt VB
                        object parent1 = GetComProperty(modelGeom, "Parent");
                        if (parent1 != null)
                        {
                            object parent2 = GetComProperty(parent1, "Parent");
                            if (parent2 is SurfaceBody)
                            {
                                surfaceBody = (SurfaceBody)parent2;
                            }
                        }
                    }
                    else if (modelGeom is Face)
                    {
                        // VB iLogic: oSurfaceBody = oModelGeom.Parent
                        object parent = GetComProperty(modelGeom, "Parent");
                        if (parent is SurfaceBody)
                        {
                            surfaceBody = (SurfaceBody)parent;
                        }
                    }
                    else if (modelGeom is SurfaceBody)
                    {
                        surfaceBody = (SurfaceBody)modelGeom;
                    }
                }
                catch (Exception)
                {
                    ShowRetryWarning(
                        "Không thể phân tích khối 3D từ đối tượng này.\nVui lòng chọn một cạnh biên rõ ràng hơn.");
                    return HatchPickResult.Retry();
                }

                if (surfaceBody == null)
                {
                    ShowRetryWarning(
                        "Không tìm thấy khối vật thể liên kết với cạnh này.\nVui lòng chọn cạnh khác hoặc nhấn ESC để kết thúc.");
                    return HatchPickResult.Retry();
                }

                // Lấy HatchRegions từ DrawingView (chỉ khả dụng từ Inventor 2022+)
                object hatchRegionsObj = null;
                try
                {
                    hatchRegionsObj = view.HatchRegions;
                }
                catch (Exception)
                {
                    // Fallback: late binding
                    try
                    {
                        hatchRegionsObj = GetComProperty(view, "HatchRegions");
                    }
                    catch (Exception)
                    {
                        ShowRetryWarning(
                            "Phiên bản Inventor của bạn có thể cũ hơn 2022.\nAPI chưa hỗ trợ tính năng HatchRegions.");
                        return HatchPickResult.Cancelled();
                    }
                }

                if (hatchRegionsObj == null)
                {
                    ShowRetryWarning(
                        "Hình chiếu này không có dữ liệu HatchRegions.\nVui lòng chọn cạnh khác hoặc nhấn ESC để kết thúc.");
                    return HatchPickResult.Retry();
                }

                // Duyệt tìm hatch region khớp với SurfaceBody
                // Dùng COM identity comparison (IUnknown pointer) — giống VB "Is"
                DrawingViewHatchRegions hatchRegions = (DrawingViewHatchRegions)hatchRegionsObj;
                IntPtr pTargetUnk = IntPtr.Zero;
                try
                {
                    pTargetUnk = Marshal.GetIUnknownForObject(surfaceBody);

                    foreach (DrawingViewHatchRegion hatch in hatchRegions)
                    {
                        IntPtr pHatchBodyUnk = IntPtr.Zero;
                        try
                        {
                            SurfaceBody hatchBody = hatch.SurfaceBody as SurfaceBody;
                            if (hatchBody == null)
                            {
                                // Fallback: late binding
                                object bodyObj = GetComProperty(hatch, "SurfaceBody");
                                hatchBody = bodyObj as SurfaceBody;
                            }
                            if (hatchBody != null)
                            {
                                pHatchBodyUnk = Marshal.GetIUnknownForObject(hatchBody);
                                if (pHatchBodyUnk == pTargetUnk)
                                {
                                    return HatchPickResult.Found(hatch); // Tìm thấy!
                                }
                            }
                        }
                        catch (Exception)
                        {
                            // Bỏ qua hatch region lỗi
                        }
                        finally
                        {
                            if (pHatchBodyUnk != IntPtr.Zero)
                            {
                                Marshal.Release(pHatchBodyUnk);
                            }
                        }
                    }
                }
                finally
                {
                    if (pTargetUnk != IntPtr.Zero)
                    {
                        Marshal.Release(pTargetUnk);
                    }
                }

                ShowRetryWarning(
                    "Không tìm thấy mặt cắt (Hatch) nào khớp với chi tiết bạn vừa click.\n" +
                    "Đảm bảo bạn đang chọn cạnh thuộc hình cắt (Section View).\n" +
                    "Vui lòng chọn lại hoặc nhấn ESC để kết thúc.");
                return HatchPickResult.Retry();
            }
            catch (Exception)
            {
                // User nhấn ESC hoặc hủy Pick → kết thúc workflow.
                return HatchPickResult.Cancelled();
            }
        }

        private static void ShowRetryWarning(string message)
        {
            System.Windows.MessageBox.Show(
                message,
                "VinTed — Copy Hatch",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }

        /// <summary>
        /// Late binding: gọi property trên COM object qua reflection/IDispatch.
        /// Tương đương VB late-bound ".PropertyName" trên Object.
        /// </summary>
        private static object GetComProperty(object comObj, string propertyName)
        {
            if (comObj == null) return null;
            try
            {
                return comObj.GetType().InvokeMember(
                    propertyName,
                    BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public,
                    null,
                    comObj,
                    null);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Late binding: set property trên COM object qua reflection/IDispatch.
        /// Tương đương VB late-bound "obj.PropertyName = value" qua DISPATCH_PROPERTYPUT.
        /// </summary>
        private static void SetComProperty(object comObj, string propertyName, object value)
        {
            comObj.GetType().InvokeMember(
                propertyName,
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.Public,
                null,
                comObj,
                new object[] { value });
        }
    }
}
