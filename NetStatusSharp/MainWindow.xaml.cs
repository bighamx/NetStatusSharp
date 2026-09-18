using NetStatusSharp.Core;
using NetStatusSharp.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        // 先还原上次调整的列宽，再取数据填充表格。
        ConnectionGrid.ColumnReordered += ConnectionGrid_ColumnReordered;
        // DataGrid 会在内部把拖拽柄的 DragCompleted 标记为已处理，
        // 所以必须 handledEventsToo: true 才能收到列宽拖拽结束事件。
        ConnectionGrid.AddHandler(
            Thumb.DragCompletedEvent,
            new DragCompletedEventHandler(ColumnGrip_DragCompleted),
            handledEventsToo: true);
        ColumnLayoutStore.Load(ConnectionGrid);
        await viewModel.RefreshAsync();
    }

    private void ConnectionGrid_ColumnReordered(object? sender, DataGridColumnEventArgs e)
    {
        ColumnLayoutStore.Save(ConnectionGrid);
    }

    private void ColumnGrip_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (e.OriginalSource is not Thumb { Name: "PART_RightHeaderGripper" or "PART_LeftHeaderGripper" })
        {
            // 滚动条等其它 Thumb 的拖拽不涉及列宽。
            return;
        }

        // 拖完列宽立刻落盘，不必等到关闭窗口。
        ColumnLayoutStore.Save(ConnectionGrid);
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
        ColumnLayoutStore.Save(ConnectionGrid);
        viewModel.Dispose();
    }
}
