using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace VinTed.Features.AssemblyBrowser
{
    public partial class AssemblyBrowserWindow : Window
    {
        private Inventor.Application _invApp;
        private Inventor.AssemblyDocument _asmDoc;
        private Inventor.UserInputEvents _userInputEvents;
        private ObservableCollection<ComponentRowData> _componentData;
        
        // CRITICAL: Anti-infinite loop flag
        private bool _isSyncingSelection = false;

        public AssemblyBrowserWindow(Inventor.Application invApp)
        {
            InitializeComponent();
            _invApp = invApp;
            _componentData = new ObservableCollection<ComponentRowData>();
            dgComponents.ItemsSource = _componentData;

            Loaded += AssemblyBrowserWindow_Loaded;
            Closed += AssemblyBrowserWindow_Closed;
        }

        private void AssemblyBrowserWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate active document
                if (_invApp.ActiveDocument == null || 
                    _invApp.ActiveDocument.DocumentType != Inventor.DocumentTypeEnum.kAssemblyDocumentObject)
                {
                    System.Windows.MessageBox.Show(
                        "Tính năng Assembly Browser chỉ hoạt động trong môi trường Assembly (.iam).",
                        "VinTed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    Close();
                    return;
                }

                _asmDoc = (Inventor.AssemblyDocument)_invApp.ActiveDocument;
                txtDocumentName.Text = System.IO.Path.GetFileName(_asmDoc.FullFileName);

                // Hook UserInputEvents for Model -> UI sync
                _userInputEvents = _invApp.CommandManager.UserInputEvents;
                _userInputEvents.OnSelect += UserInputEvents_OnSelect;

                // Initial load
                LoadComponentData();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "Lỗi khi khởi tạo Assembly Browser: " + ex.Message,
                    "VinTed Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AssemblyBrowserWindow_Closed(object sender, EventArgs e)
        {
            try
            {
                // Unhook events
                if (_userInputEvents != null)
                {
                    _userInputEvents.OnSelect -= UserInputEvents_OnSelect;
                    _userInputEvents = null;
                }
            }
            catch { }
        }

        /// <summary>
        /// Load all ComponentOccurrences from active Assembly into DataGrid (recursive)
        /// </summary>
        private void LoadComponentData()
        {
            try
            {
                _componentData.Clear();

                if (_asmDoc == null || _asmDoc.ComponentDefinition == null)
                    return;

                Inventor.ComponentOccurrences occurrences = _asmDoc.ComponentDefinition.Occurrences;
                
                // Recursively load all occurrences including sub-assemblies
                foreach (Inventor.ComponentOccurrence occ in occurrences)
                {
                    LoadOccurrenceRecursive(occ, 0, "");
                }

                txtStatus.Text = String.Format("Loaded {0} components", _componentData.Count);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "Lỗi khi load dữ liệu: " + ex.Message,
                    "VinTed Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Recursively load occurrence and all its sub-occurrences
        /// </summary>
        private void LoadOccurrenceRecursive(Inventor.ComponentOccurrence occ, int level, string parentPath)
        {
            try
            {
                // Add current occurrence to grid
                string name = occ.Name;
                string material = GetMaterial(occ);
                string quantity = "1";
                string mass = GetMass(occ);
                string fullPath = GetOccurrenceFullPath(occ);

                // Add indentation for visual hierarchy (optional)
                string indent = new string(' ', level * 2);
                
                // Build occurrence path for selection (e.g., "SubAsm:1\Part:2")
                string occPath = String.IsNullOrEmpty(parentPath) 
                    ? occ.Name 
                    : parentPath + "\\" + occ.Name;
                
                ComponentRowData row = new ComponentRowData
                {
                    Name = indent + name,
                    Material = material,
                    Quantity = quantity,
                    Mass = mass,
                    FullPath = fullPath,
                    OccurrenceReference = occ,
                    OccurrencePath = occPath
                };

                _componentData.Add(row);

                // If this is a sub-assembly, recursively load its children
                if (occ.DefinitionDocumentType == Inventor.DocumentTypeEnum.kAssemblyDocumentObject)
                {
                    try
                    {
                        if (occ.SubOccurrences != null)
                        {
                            foreach (Inventor.ComponentOccurrence subOcc in occ.SubOccurrences)
                            {
                                LoadOccurrenceRecursive(subOcc, level + 1, occPath);
                            }
                        }
                    }
                    catch
                    {
                        // Skip if sub-assembly cannot be accessed
                    }
                }
            }
            catch
            {
                // Skip invalid occurrences
            }
        }

        /// <summary>
        /// Get material name from ComponentOccurrence
        /// </summary>
        private string GetMaterial(Inventor.ComponentOccurrence occ)
        {
            try
            {
                if (occ.DefinitionDocumentType == Inventor.DocumentTypeEnum.kPartDocumentObject)
                {
                    Inventor.PartDocument partDoc = (Inventor.PartDocument)occ.Definition.Document;
                    if (partDoc != null && partDoc.PropertySets != null)
                    {
                        Inventor.PropertySet designProps = partDoc.PropertySets["Design Tracking Properties"];
                        if (designProps != null)
                        {
                            return designProps["Material"].Value.ToString();
                        }
                    }
                }
            }
            catch { }
            return "-";
        }

        /// <summary>
        /// Get mass from ComponentOccurrence
        /// </summary>
        private string GetMass(Inventor.ComponentOccurrence occ)
        {
            try
            {
                if (occ.DefinitionDocumentType == Inventor.DocumentTypeEnum.kPartDocumentObject)
                {
                    Inventor.PartDocument partDoc = (Inventor.PartDocument)occ.Definition.Document;
                    if (partDoc != null && partDoc.ComponentDefinition != null)
                    {
                        double massKg = partDoc.ComponentDefinition.MassProperties.Mass;
                        return massKg.ToString("F3");
                    }
                }
            }
            catch { }
            return "-";
        }

        /// <summary>
        /// Get full occurrence path (for sub-assemblies)
        /// </summary>
        private string GetOccurrenceFullPath(Inventor.ComponentOccurrence occ)
        {
            try
            {
                List<string> pathParts = new List<string>();
                Inventor.ComponentOccurrence current = occ;

                while (current != null)
                {
                    pathParts.Insert(0, current.Name);
                    
                    // Navigate up to parent
                    object parent = current.GetType().InvokeMember("Parent",
                        System.Reflection.BindingFlags.GetProperty | 
                        System.Reflection.BindingFlags.Instance | 
                        System.Reflection.BindingFlags.Public,
                        null, current, null);

                    if (parent is Inventor.ComponentOccurrence)
                    {
                        current = (Inventor.ComponentOccurrence)parent;
                    }
                    else
                    {
                        break;
                    }
                }

                return String.Join(" > ", pathParts);
            }
            catch
            {
                return occ.Name;
            }
        }

        /// <summary>
        /// DIRECTION 1: UI -> Model
        /// When user selects rows in DataGrid, highlight the parts in 3D model
        /// </summary>
        private void DgComponents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Prevent recursive event firing
            if (_isSyncingSelection)
                return;

            try
            {
                _isSyncingSelection = true;

                // Get all selected rows
                var selectedRows = dgComponents.SelectedItems.Cast<ComponentRowData>().ToList();
                if (selectedRows.Count == 0)
                {
                    // Clear selection if nothing selected
                    _asmDoc.SelectSet.Clear();
                    txtStatus.Text = "No selection";
                    return;
                }

                // Clear current selection
                _asmDoc.SelectSet.Clear();

                // Select all occurrences in 3D model
                int successCount = 0;
                foreach (var row in selectedRows)
                {
                    if (row.OccurrenceReference != null)
                    {
                        try
                        {
                            _asmDoc.SelectSet.Select(row.OccurrenceReference);
                            successCount++;
                        }
                        catch { }
                    }
                }

                // // Zoom to selected components (optional)
                // if (successCount > 0)
                // {
                //     try
                //     {
                //         _invApp.ActiveView.Fit(true);
                //     }
                //     catch { }
                // }

                txtStatus.Text = String.Format("Selected: {0} component(s)", successCount);
            }
            catch (Exception ex)
            {
                txtStatus.Text = "Error: " + ex.Message;
            }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        /// <summary>
        /// DIRECTION 2: Model -> UI
        /// When user selects parts in 3D model, highlight the corresponding rows in DataGrid
        /// </summary>
        private void UserInputEvents_OnSelect(
            Inventor.ObjectsEnumerator JustSelectedEntities,
            ref Inventor.ObjectCollection MoreSelectedEntities,
            Inventor.SelectionDeviceEnum SelectionDevice,
            Inventor.Point ModelPosition,
            Inventor.Point2d ViewPosition,
            Inventor.View View)
        {
            // Prevent recursive event firing
            if (_isSyncingSelection)
                return;

            try
            {
                _isSyncingSelection = true;

                // Collect all selected occurrences from current SelectSet
                List<Inventor.ComponentOccurrence> selectedOccurrences = new List<Inventor.ComponentOccurrence>();
                
                if (_asmDoc.SelectSet != null && _asmDoc.SelectSet.Count > 0)
                {
                    for (int i = 1; i <= _asmDoc.SelectSet.Count; i++)
                    {
                        try
                        {
                            object entity = _asmDoc.SelectSet[i];
                            Inventor.ComponentOccurrence occ = GetComponentOccurrenceFromEntity(entity);
                            if (occ != null && !selectedOccurrences.Contains(occ))
                            {
                                selectedOccurrences.Add(occ);
                            }
                        }
                        catch { }
                    }
                }

                // Find corresponding rows in DataGrid
                List<ComponentRowData> matchingRows = new List<ComponentRowData>();
                foreach (var occ in selectedOccurrences)
                {
                    ComponentRowData row = FindRowByOccurrence(occ);
                    if (row != null && !matchingRows.Contains(row))
                    {
                        matchingRows.Add(row);
                    }
                }

                // Dispatch to UI thread
                if (matchingRows.Count > 0)
                {
                    Dispatcher.BeginInvoke(new Action(delegate()
                    {
                        try
                        {
                            dgComponents.SelectedItems.Clear();
                            foreach (var row in matchingRows)
                            {
                                dgComponents.SelectedItems.Add(row);
                            }
                            
                            // Scroll to first selected item
                            if (matchingRows.Count > 0)
                            {
                                dgComponents.ScrollIntoView(matchingRows[0]);
                            }
                            
                            txtStatus.Text = String.Format("Synced from model: {0} component(s)", matchingRows.Count);
                        }
                        catch { }
                    }));
                }
            }
            catch { }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        /// <summary>
        /// Extract ComponentOccurrence from selected entity (handles sub-occurrences)
        /// </summary>
        private Inventor.ComponentOccurrence GetComponentOccurrenceFromEntity(object entity)
        {
            try
            {
                // Direct ComponentOccurrence
                if (entity is Inventor.ComponentOccurrence)
                {
                    return (Inventor.ComponentOccurrence)entity;
                }

                // Face -> ComponentOccurrence
                if (entity is Inventor.Face)
                {
                    Inventor.Face face = (Inventor.Face)entity;
                    object parent = face.GetType().InvokeMember("Parent",
                        System.Reflection.BindingFlags.GetProperty | 
                        System.Reflection.BindingFlags.Instance | 
                        System.Reflection.BindingFlags.Public,
                        null, face, null);

                    if (parent is Inventor.ComponentOccurrence)
                    {
                        return (Inventor.ComponentOccurrence)parent;
                    }
                }

                // Edge -> Face -> ComponentOccurrence
                if (entity is Inventor.Edge)
                {
                    Inventor.Edge edge = (Inventor.Edge)entity;
                    object parent = edge.GetType().InvokeMember("Parent",
                        System.Reflection.BindingFlags.GetProperty | 
                        System.Reflection.BindingFlags.Instance | 
                        System.Reflection.BindingFlags.Public,
                        null, edge, null);

                    if (parent != null)
                    {
                        return GetComponentOccurrenceFromEntity(parent);
                    }
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Find DataGrid row by ComponentOccurrence reference
        /// Handles sub-occurrence mapping to top-level parent
        /// </summary>
        private ComponentRowData FindRowByOccurrence(Inventor.ComponentOccurrence targetOcc)
        {
            try
            {
                // Get top-level occurrence (in case of sub-assembly selection)
                Inventor.ComponentOccurrence topLevelOcc = GetTopLevelOccurrence(targetOcc);

                // Search in DataGrid
                foreach (ComponentRowData row in _componentData)
                {
                    if (row.OccurrenceReference == null)
                        continue;

                    // Compare by full path (more reliable than reference equality)
                    string rowPath = GetOccurrenceFullPath(row.OccurrenceReference);
                    string targetPath = GetOccurrenceFullPath(topLevelOcc);

                    if (rowPath == targetPath)
                    {
                        return row;
                    }
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Get top-level ComponentOccurrence (navigate up from sub-assembly)
        /// </summary>
        private Inventor.ComponentOccurrence GetTopLevelOccurrence(Inventor.ComponentOccurrence occ)
        {
            try
            {
                Inventor.ComponentOccurrence current = occ;
                Inventor.ComponentOccurrence topLevel = occ;

                while (current != null)
                {
                    topLevel = current;

                    object parent = current.GetType().InvokeMember("Parent",
                        System.Reflection.BindingFlags.GetProperty | 
                        System.Reflection.BindingFlags.Instance | 
                        System.Reflection.BindingFlags.Public,
                        null, current, null);

                    if (parent is Inventor.ComponentOccurrence)
                    {
                        current = (Inventor.ComponentOccurrence)parent;
                    }
                    else
                    {
                        break;
                    }
                }

                return topLevel;
            }
            catch
            {
                return occ;
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadComponentData();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    /// <summary>
    /// Data model for DataGrid row
    /// </summary>
    public class ComponentRowData
    {
        public string Name { get; set; }
        public string Material { get; set; }
        public string Quantity { get; set; }
        public string Mass { get; set; }
        public string FullPath { get; set; }
        
        // Store COM reference for selection sync
        public Inventor.ComponentOccurrence OccurrenceReference { get; set; }
        
        // Store occurrence path for nested occurrences (e.g., "SubAsm:1\Part:2")
        public string OccurrencePath { get; set; }
    }
}
