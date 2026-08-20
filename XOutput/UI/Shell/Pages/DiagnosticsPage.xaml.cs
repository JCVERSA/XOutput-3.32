using System.Windows.Controls;
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
    }
}
