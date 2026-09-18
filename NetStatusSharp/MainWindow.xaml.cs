using NetStatusSharp.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NetStatusSharp;

public partial class MainWindow : Window
{
    private readonly MainViewModel viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.ErrorRequested += ShowError;
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await viewModel.RefreshAsync();
    }

    private async void FilterBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && viewModel.RefreshCommand.CanExecute(null))
        {
            e.Handled = true;
            await viewModel.RefreshAsync();
        }
    }

    private void ConnectionGrid_CopyingRowClipboardContent(
        object sender,
        DataGridRowClipboardEventArgs e)
    {
        // 图标列不应在复制结果中产生空白字段。
        if (e.ClipboardRowContent.Count > 1)
        {
            e.ClipboardRowContent.RemoveAt(1);
        }
    }

    private void ShowError(string message)
    {
        MessageBox.Show(this, message, "NetStatusSharp", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        viewModel.ErrorRequested -= ShowError;
        viewModel.Dispose();
    }
}
