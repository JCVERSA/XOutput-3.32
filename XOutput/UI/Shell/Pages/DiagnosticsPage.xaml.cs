using System;
using System.Windows;
using System.Windows.Controls;
using XOutput.Tools;
using XOutput.UI.Windows;

namespace XOutput.UI.Shell.Pages
{
    /// <summary>
    /// Diagnostics page (formerly DiagnosticsWindow).
    /// </summary>
    public partial class DiagnosticsPage : UserControl, IViewBase<DiagnosticsViewModel, DiagnosticsModel>
    {
        private readonly DiagnosticsViewModel viewModel;
        public DiagnosticsViewModel ViewModel => viewModel;

        public DiagnosticsPage(DiagnosticsViewModel viewModel)
        {
            this.viewModel = viewModel;
            DataContext = viewModel;
            InitializeComponent();
        }

        private void ExportClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = DiagnosticsExporter.Export(viewModel, Environment.CurrentDirectory);
                ShellViewModel.Instance?.ShowMessage(LanguageModel.Instance.Translate("ExportReport"),
                    path);
            }
            catch (Exception ex)
            {
                ShellViewModel.Instance?.ShowMessage(LanguageModel.Instance.Translate("Error"), ex.Message);
            }
        }
    }
}
