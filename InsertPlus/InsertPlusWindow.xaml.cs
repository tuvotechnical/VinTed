using System;
using System.Windows;
using Inventor;

namespace VinTed.InsertPlus
{
    public partial class InsertPlusWindow : Window
    {
        private Inventor.Application _invApp;
        private AssemblyDocument _asmDoc;
        private InsertPlusEngine _engine;

        public InsertPlusWindow(Inventor.Application invApp, AssemblyDocument asmDoc)
        {
            InitializeComponent();
            _invApp = invApp;
            _asmDoc = asmDoc;
            _engine = new InsertPlusEngine(invApp, asmDoc);
        }

        private void BtnSelectSource_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Hide();
                bool success = _engine.SelectSourceHardware(
                    chkIncludeHardware.IsChecked == true
                );
                
                if (success)
                {
                    txtSourceInfo.Text = string.Format("Đã chọn: {0}\n({1} chi tiết liên quan)", 
                        _engine.SourcePrimaryOccurrence.Name,
                        _engine.SourceOccurrencesToCopy.Count);
                    txtSourceInfo.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 150, 0));
                    
                    btnManualAttach.IsEnabled = true;
                    btnAutoAttach.IsEnabled = true;
                }
                else
                {
                    txtSourceInfo.Text = "Chưa chọn đối tượng hoặc bị hủy";
                    txtSourceInfo.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 0, 0));
                    
                    btnManualAttach.IsEnabled = false;
                    btnAutoAttach.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi chọn nguồn: " + ex.Message, "VinTed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                this.Show();
            }
        }

        private void BtnManualAttach_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Hide();
                double offset = 0;
                double.TryParse(txtOffset.Text, out offset);
                // Convert from mm to cm (Inventor internal unit is cm)
                offset = offset / 10.0;
                
                bool isOpposed = rbOpposed.IsChecked == true;
                bool lockRotation = chkLockRotation.IsChecked == true;

                _engine.ManualAttach(offset, isOpposed, lockRotation);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi copy thủ công: " + ex.Message, "VinTed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                this.Show();
            }
        }

        private void BtnAutoAttach_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Hide();
                double offset = 0;
                double.TryParse(txtOffset.Text, out offset);
                offset = offset / 10.0;
                
                bool isOpposed = rbOpposed.IsChecked == true;
                bool lockRotation = chkLockRotation.IsChecked == true;

                _engine.AutoAttachFace(offset, isOpposed, lockRotation);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi copy tự động: " + ex.Message, "VinTed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                this.Show();
            }
        }
    }
}
