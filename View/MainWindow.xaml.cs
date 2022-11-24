using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AgdaLibraryLookup.ViewModel;

namespace AgdaLibraryLookup
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel = new MainWindowViewModel();

        public MainWindow()
        {
            this.InitializeComponent();

            this.DataContext = _viewModel;
        }
    }
}
