using System;
using System.Windows;
using System.Windows.Input;

namespace VinTed.FindReplace
{
    /// <summary>
    /// Code-behind cho FindReplaceWindow.
    /// Kết nối UI WPF (ModernWpf Light Theme) → FindReplaceEngine (core logic bảo toàn từ iLogic).
    /// </summary>
    public partial class FindReplaceWindow : Window
    {
        private readonly FindReplaceEngine _engine;

        public FindReplaceWindow(Inventor.Application invApp, Inventor.DrawingDocument drawDoc)
        {
            InitializeComponent();
            _engine = new FindReplaceEngine(invApp, drawDoc);
        }

        private void TxtFind_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnFind_Click(sender, e);
            }
        }

        private void BtnFind_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string result = _engine.FindNext(txtFind.Text);
                if (!String.IsNullOrEmpty(result))
                {
                    lblStatus.Text = result;
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

        private void BtnReplace_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string result = _engine.ReplaceCurrent(txtFind.Text, txtReplace.Text);
                if (!String.IsNullOrEmpty(result))
                {
                    lblStatus.Text = result;
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

        private void BtnReplaceAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string result = _engine.ReplaceAll(txtFind.Text, txtReplace.Text);
                if (!String.IsNullOrEmpty(result))
                {
                    lblStatus.Text = result;
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
