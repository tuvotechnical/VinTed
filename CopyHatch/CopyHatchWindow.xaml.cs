using System;
using System.Windows;

namespace VinTed.CopyHatch
{
    /// <summary>
    /// Code-behind cho CopyHatchWindow.
    /// Click "Bắt đầu" → đóng window → chạy pick loop trực tiếp trên Inventor → hiện kết quả.
    /// </summary>
    public partial class CopyHatchWindow : Window
    {
        private readonly Inventor.Application _invApp;
        private readonly Inventor.DrawingDocument _drawDoc;

        public CopyHatchWindow(Inventor.Application invApp, Inventor.DrawingDocument drawDoc)
        {
            InitializeComponent();
            _invApp = invApp;
            _drawDoc = drawDoc;
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Đóng hoàn toàn window trước khi pick — tránh can thiệp focus của Inventor
                this.Close();

                // Chạy engine trực tiếp trên STA thread (blocking — giống iLogic)
                CopyHatchEngine engine = new CopyHatchEngine(_invApp, _drawDoc);
                engine.Execute();

                // Hiện kết quả
                if (engine.CopiedCount > 0)
                {
                    System.Windows.MessageBox.Show(
                        String.Format("Hoàn tất — đã copy {0} hatch thành công!", engine.CopiedCount),
                        "VinTed — Copy Hatch",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "Lỗi: " + ex.Message,
                    "VinTed Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
