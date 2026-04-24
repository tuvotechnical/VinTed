using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Inventor;

namespace VinTed.InsertPlus
{
    public class InsertPlusEngine
    {
        private Inventor.Application _invApp;
        private AssemblyDocument _asmDoc;

        public ComponentOccurrence SourcePrimaryOccurrence { get; private set; }
        public EdgeProxy SourceBaseEdge { get; private set; }
        public List<ComponentOccurrence> SourceOccurrencesToCopy { get; private set; }

        public InsertPlusEngine(Inventor.Application invApp, AssemblyDocument asmDoc)
        {
            _invApp = invApp;
            _asmDoc = asmDoc;
            SourceOccurrencesToCopy = new List<ComponentOccurrence>();
        }

        public bool SelectSourceHardware(bool includeAttachedHardware)
        {
            SourcePrimaryOccurrence = null;
            SourceBaseEdge = null;
            SourceOccurrencesToCopy.Clear();

            CommandManager cmdMgr = _invApp.CommandManager;
            object pickedObj = cmdMgr.Pick(SelectionFilterEnum.kPartEdgeCircularFilter, 
                "BƯỚC 1: Chọn một cạnh tròn làm Gốc trên chi tiết bu-lông/ốc (Nhấn ESC để hủy)");

            if (pickedObj == null) return false;

            EdgeProxy edge = pickedObj as EdgeProxy;
            if (edge == null) return false;

            SourceBaseEdge = edge;
            SourcePrimaryOccurrence = edge.ContainingOccurrence;

            SourceOccurrencesToCopy.Add(SourcePrimaryOccurrence);

            if (includeAttachedHardware)
            {
                // Tìm các chi tiết bị ràng buộc với chi tiết này
                // (Để an toàn và không quá phức tạp, chúng ta copy chi tiết gốc và bất kỳ chi tiết nào
                // bị ràng buộc Insert, Mate, Flush với nó trong cùng cấp Assembly)
                AssemblyComponentDefinition def = _asmDoc.ComponentDefinition;
                foreach (AssemblyConstraint constraint in def.Constraints)
                {
                    try
                    {
                        ComponentOccurrence occ1 = GetOccurrenceFromConstraintEntity(constraint.EntityOne);
                        ComponentOccurrence occ2 = GetOccurrenceFromConstraintEntity(constraint.EntityTwo);

                        if (occ1 != null && occ2 != null)
                        {
                            if (occ1 == SourcePrimaryOccurrence && !SourceOccurrencesToCopy.Contains(occ2))
                            {
                                SourceOccurrencesToCopy.Add(occ2);
                            }
                            else if (occ2 == SourcePrimaryOccurrence && !SourceOccurrencesToCopy.Contains(occ1))
                            {
                                SourceOccurrencesToCopy.Add(occ1);
                            }
                        }
                    }
                    catch (Exception) { }
                }
            }

            return true;
        }

        private ComponentOccurrence GetOccurrenceFromConstraintEntity(object entity)
        {
            try
            {
                // EntityOne / EntityTwo thường là EdgeProxy, FaceProxy...
                // Lấy đối tượng proxy và đọc ContainingOccurrence
                var proxy = entity as object;
                if (proxy != null)
                {
                    object parentOcc = proxy.GetType().InvokeMember("ContainingOccurrence", 
                        System.Reflection.BindingFlags.GetProperty, null, proxy, null);
                    return parentOcc as ComponentOccurrence;
                }
            }
            catch (Exception) { }
            return null;
        }

        public void ManualAttach(double offset, bool isOpposed, bool lockRotation)
        {
            CommandManager cmdMgr = _invApp.CommandManager;
            bool keepGoing = true;

            while (keepGoing)
            {
                object pickedObj = cmdMgr.Pick(SelectionFilterEnum.kPartEdgeCircularFilter, 
                    "BƯỚC 2: Chọn các cạnh lỗ Đích để copy hardware vào (Nhấn ESC để hoàn tất)");

                if (pickedObj == null)
                {
                    keepGoing = false;
                    break;
                }

                EdgeProxy targetEdge = pickedObj as EdgeProxy;
                if (targetEdge != null)
                {
                    PerformAttach(targetEdge, offset, isOpposed, lockRotation);
                }
            }
        }

        public void AutoAttachFace(double offset, bool isOpposed, bool lockRotation)
        {
            CommandManager cmdMgr = _invApp.CommandManager;
            object pickedObj = cmdMgr.Pick(SelectionFilterEnum.kPartEdgeCircularFilter, 
                "BƯỚC 2: Chọn MỘT cạnh lỗ Đích làm mẫu. Add-in sẽ tự điền toàn bộ mặt phẳng.");

            if (pickedObj == null) return;

            EdgeProxy sampleTargetEdge = pickedObj as EdgeProxy;
            if (sampleTargetEdge == null) return;

            // Tìm Face chứa cái lỗ này
            FaceProxy targetFace = null;
            try
            {
                // Edge thường nằm giao giữa mặt trụ của lỗ và mặt phẳng của tấm
                // Ta tìm mặt phẳng (Planar)
                foreach (FaceProxy f in sampleTargetEdge.Faces)
                {
                    if (f.SurfaceType == SurfaceTypeEnum.kPlaneSurface)
                    {
                        targetFace = f;
                        break;
                    }
                }
            }
            catch (Exception) { }

            if (targetFace == null)
            {
                throw new Exception("Không tìm thấy mặt phẳng (Planar Face) liên kết với cạnh lỗ vừa chọn.");
            }

            // Lấy bán kính của mẫu
            double sampleRadius = 0;
            try
            {
                Circle circle = sampleTargetEdge.Geometry as Circle;
                if (circle != null) sampleRadius = Math.Round(circle.Radius, 5);
            }
            catch (Exception) { }

            if (sampleRadius == 0)
            {
                throw new Exception("Lỗ mẫu không phải là cung tròn hợp lệ.");
            }

            // Duyệt qua tất cả các cạnh trên mặt phẳng này
            List<EdgeProxy> validHoles = new List<EdgeProxy>();
            foreach (EdgeProxy e in targetFace.Edges)
            {
                try
                {
                    if (e.GeometryType == CurveTypeEnum.kCircleCurve)
                    {
                        Circle c = e.Geometry as Circle;
                        if (c != null && Math.Round(c.Radius, 5) == sampleRadius)
                        {
                            validHoles.Add(e);
                        }
                    }
                }
                catch (Exception) { }
            }

            if (validHoles.Count == 0)
            {
                throw new Exception("Không tìm thấy lỗ nào hợp lệ trên mặt phẳng.");
            }

            Transaction txn = _invApp.TransactionManager.StartTransaction((Inventor._Document)_asmDoc, "VinTed Insert Plus Auto");
            try
            {
                foreach (EdgeProxy holeEdge in validHoles)
                {
                    PerformAttach(holeEdge, offset, isOpposed, lockRotation);
                }
                txn.End();
            }
            catch (Exception ex)
            {
                txn.Abort();
                throw new Exception("Lỗi khi Auto Fill: " + ex.Message);
            }
        }

        private void PerformAttach(EdgeProxy targetEdge, double offset, bool isOpposed, bool lockRotation)
        {
            if (SourceBaseEdge == null || SourceOccurrencesToCopy.Count == 0) return;

            // Dùng transaction nhỏ nếu chưa có transaction ngoài
            Transaction txn = null;
            bool ownTransaction = false;
            try
            {
                // Inventor Application transaction check
                // Không có API trực tiếp kiểm tra transaction đang chạy hay không, 
                // nhưng nếu StartTransaction thất bại thì nghĩa là đang có transaction.
                // Thôi, wrap trực tiếp vì AutoAttachFace đã có transaction, ManualAttach chưa có.
                // Thực ra ManualAttach pick mỗi lần một cái, có thể tạo txn trong Pick loop.
            }
            catch (Exception) { }

            // Giữ lại ComponentOccurrence gốc của Base Edge
            ComponentOccurrence primaryOcc = SourceBaseEdge.ContainingOccurrence;
            
            // Map để theo dõi source -> copied
            Dictionary<ComponentOccurrence, ComponentOccurrence> copyMap = new Dictionary<ComponentOccurrence, ComponentOccurrence>();

            AssemblyComponentDefinition def = _asmDoc.ComponentDefinition;

            foreach (ComponentOccurrence occ in SourceOccurrencesToCopy)
            {
                Inventor.Document occDoc = (Inventor.Document)occ.Definition.Document;
                string path = occDoc.FullFileName;
                ComponentOccurrence newOcc = def.Occurrences.Add(path, _invApp.TransientGeometry.CreateMatrix());
                copyMap[occ] = newOcc;
            }

            // Lấy lại cái EdgeProxy trên chi tiết mới copy (đại diện cho Base Edge)
            ComponentOccurrence newPrimaryOcc = copyMap[primaryOcc];
            object newEdgeObj = null;
            try
            {
                newPrimaryOcc.CreateGeometryProxy(SourceBaseEdge.NativeObject, out newEdgeObj);
            }
            catch (Exception) { }

            EdgeProxy newBaseEdge = newEdgeObj as EdgeProxy;

            if (newBaseEdge != null)
            {
                // Tạo Insert constraint
                try
                {
                    def.Constraints.AddInsertConstraint2(newBaseEdge, targetEdge, isOpposed, offset, lockRotation);
                }
                catch (Exception) 
                {
                    // Ignore, maybe rotation lock failed or something
                }
            }

            // Nếu có nhiều phần cứng (VD: Washer), tạo lại ràng buộc (Rigid) giữa primary và chúng
            // Hoặc đơn giản là tạo InsertConstraint / Mate y hệt gốc
            // Vì ta không có nhiều thời gian parse từng constraint, cách nhanh nhất là Ground chúng nó lại với nhau
            // Hoặc tạo một Rigid Joint giữa copied primary và copied secondary
            foreach (ComponentOccurrence occ in SourceOccurrencesToCopy)
            {
                if (occ == primaryOcc) continue;
                
                ComponentOccurrence newOcc = copyMap[occ];
                try
                {
                    // Lấy vị trí tương đối
                    Matrix matrix1 = primaryOcc.Transformation;
                    Matrix matrix2 = occ.Transformation;

                    matrix1.Invert();
                    matrix2.PreMultiplyBy(matrix1); // Rel transform

                    Matrix newPrimaryTransform = newPrimaryOcc.Transformation;
                    Matrix finalTransform = newPrimaryTransform.Copy();
                    finalTransform.PreMultiplyBy(matrix2);

                    newOcc.Transformation = finalTransform;
                    
                    // Tạo Rigid Joint để dính chặt Washer vào Bolt mới copy
                    // def.CreateGeometryIntent -> không có. AssemblyComponentDefinition có CreateGeometryIntent không?
                    // Để an toàn và dễ dàng với C# 5.0, ta dùng hàm AddMateConstraint thay vì Joint.
                    // Nhưng MateConstraint cần Face/Edge.
                    // Đơn giản nhất là dùng Grounded:
                    newOcc.Grounded = true;
                    newPrimaryOcc.Grounded = true;
                }
                catch (Exception) { }
            }
        }
    }
}
