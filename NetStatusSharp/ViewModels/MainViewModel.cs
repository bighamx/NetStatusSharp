using NetStatusSharp.Core;
using NetStatusSharp.Models;
using ProcessViewer;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace NetStatusSharp.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ConnectionProvider provider = new();
    private readonly DispatcherTimer autoRefreshTimer;
    private CancellationTokenSource? refreshCancellation;
    private ObservableCollection<ConnectionRow> rows = [];
    private string processNameFilter = string.Empty;
    private string processIdFilter = string.Empty;
    private string localPortFilter = string.Empty;
    private string remotePortFilter = string.Empty;
    private string selectedProtocol = "TCP";
    private string selectedIpVersion = "全部";
    private string selectedState = "全部状态";
    private string statusText = "准备就绪";
    private bool isBusy;
    private bool autoRefresh;
    private int totalCount;
    private int tcpCount;
    private int udpCount;
    private int establishedCount;

    public MainViewModel()
    {
        ProtocolOptions = ["全部", "TCP", "UDP"];
        IpVersionOptions = ["全部", "IPv4", "IPv6"];
        StateOptions =
        [
            "全部状态", "已关闭", "监听", "SYN 已发送", "SYN 已接收", "已建立",
            "FIN 等待 1", "FIN 等待 2", "关闭等待", "正在关闭", "最后确认", "时间等待", "删除 TCB"
        ];

        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy);
        CancelCommand = new RelayCommand(Cancel, () => IsBusy);
        RefreshOrCancelCommand = new RelayCommand(RefreshOrCancel);
        ClearCommand = new AsyncRelayCommand(ClearAndRefreshAsync, () => !IsBusy);

        autoRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        autoRefreshTimer.Tick += async (_, _) =>
        {
            if (!IsBusy)
            {
                await RefreshAsync();
            }
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<string>? ErrorRequested;

    public ObservableCollection<ConnectionRow> Rows
    {
        get => rows;
        private set => SetField(ref rows, value);
    }

    public IReadOnlyList<string> ProtocolOptions { get; }
    public IReadOnlyList<string> IpVersionOptions { get; }
    public IReadOnlyList<string> StateOptions { get; }
    public AsyncRelayCommand RefreshCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand RefreshOrCancelCommand { get; }
    public AsyncRelayCommand ClearCommand { get; }

    public string RefreshButtonText => IsBusy ? "停止" : "刷新";

    public string ProcessNameFilter
    {
        get => processNameFilter;
        set => SetField(ref processNameFilter, value);
    }

    public string ProcessIdFilter
    {
        get => processIdFilter;
        set => SetField(ref processIdFilter, value);
    }

    public string LocalPortFilter
    {
        get => localPortFilter;
        set => SetField(ref localPortFilter, value);
    }

    public string RemotePortFilter
    {
        get => remotePortFilter;
        set => SetField(ref remotePortFilter, value);
    }

    public string SelectedProtocol
    {
        get => selectedProtocol;
        set
        {
            if (SetField(ref selectedProtocol, value))
            {
                OnPropertyChanged(nameof(RemoteFiltersEnabled));
            }
        }
    }

    public string SelectedState
    {
        get => selectedState;
        set => SetField(ref selectedState, value);
    }

    public string SelectedIpVersion
    {
        get => selectedIpVersion;
        set => SetField(ref selectedIpVersion, value);
    }

    public bool RemoteFiltersEnabled => SelectedProtocol != "UDP";

    public bool AutoRefresh
    {
        get => autoRefresh;
        set
        {
            if (!SetField(ref autoRefresh, value))
            {
                return;
            }

            if (value)
            {
                autoRefreshTimer.Start();
            }
            else
            {
                autoRefreshTimer.Stop();
            }
        }
    }

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetField(ref isBusy, value))
            {
                RefreshCommand.RaiseCanExecuteChanged();
                CancelCommand.RaiseCanExecuteChanged();
                ClearCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(RefreshButtonText));
            }
        }
    }

    public string StatusText
    {
        get => statusText;
        private set => SetField(ref statusText, value);
    }

    public int TotalCount
    {
        get => totalCount;
        private set => SetField(ref totalCount, value);
    }

    public int TcpCount
    {
        get => tcpCount;
        private set => SetField(ref tcpCount, value);
    }

    public int UdpCount
    {
        get => udpCount;
        private set => SetField(ref udpCount, value);
    }

    public int EstablishedCount
    {
        get => establishedCount;
        private set => SetField(ref establishedCount, value);
    }

    public async Task RefreshAsync()
    {
        if (IsBusy || !TryBuildFilter(out ConnectionFilter filter))
        {
            return;
        }

        refreshCancellation = new CancellationTokenSource();
        CancellationToken token = refreshCancellation.Token;
        var stopwatch = Stopwatch.StartNew();
        IsBusy = true;
        StatusText = "正在读取系统连接并解析进程信息…";

        try
        {
            ConnectionQueryResult result = await Task.Run(() => provider.Query(filter, token), token);
            token.ThrowIfCancellationRequested();

            Rows = new ObservableCollection<ConnectionRow>(result.Rows);
            TotalCount = result.Rows.Count;
            TcpCount = result.TcpCount;
            UdpCount = result.UdpCount;
            EstablishedCount = result.EstablishedCount;
            StatusText = string.Format(
                CultureInfo.CurrentCulture,
                "已显示 {0:N0} 条连接  ·  用时 {1:N0} ms  ·  更新于 {2:HH:mm:ss}",
                result.Rows.Count,
                stopwatch.Elapsed.TotalMilliseconds,
                DateTime.Now);
        }
        catch (OperationCanceledException)
        {
            StatusText = "刷新已取消，当前列表保持不变";
        }
        catch (Exception exception)
        {
            StatusText = "刷新失败";
            ErrorRequested?.Invoke("刷新连接列表失败：" + exception.Message);
        }
        finally
        {
            stopwatch.Stop();
            IsBusy = false;
            refreshCancellation.Dispose();
            refreshCancellation = null;
        }
    }

    public void Cancel()
    {
        refreshCancellation?.Cancel();
        StatusText = "正在取消刷新…";
    }

    private async void RefreshOrCancel()
    {
        if (IsBusy)
        {
            Cancel();
            return;
        }

        await RefreshAsync();
    }

    public void Dispose()
    {
        autoRefreshTimer.Stop();
        refreshCancellation?.Cancel();
        refreshCancellation?.Dispose();
    }

    private async Task ClearAndRefreshAsync()
    {
        ProcessNameFilter = string.Empty;
        ProcessIdFilter = string.Empty;
        LocalPortFilter = string.Empty;
        RemotePortFilter = string.Empty;
        SelectedProtocol = "TCP";
        SelectedIpVersion = "全部";
        SelectedState = "全部状态";
        await RefreshAsync();
    }

    private bool TryBuildFilter(out ConnectionFilter filter)
    {
        List<string> processNames = SplitValues(ProcessNameFilter);
        if (!TryParseNumbers(ProcessIdFilter, "PID", int.TryParse, out List<int> processIds) ||
            !TryParseNumbers(LocalPortFilter, "本地端口", ushort.TryParse, out List<ushort> localPorts) ||
            !TryParseNumbers(RemotePortFilter, "远程端口", ushort.TryParse, out List<ushort> remotePorts))
        {
            filter = null!;
            return false;
        }

        bool? isTcp = SelectedProtocol switch
        {
            "TCP" => true,
            "UDP" => false,
            _ => null
        };
        ConnectionState? state = SelectedState == "全部状态"
            ? null
            : (ConnectionState?)StateOptions.IndexOf(SelectedState);
        string? ipVersion = SelectedIpVersion == "全部" ? null : SelectedIpVersion;

        filter = new ConnectionFilter(processNames, processIds, localPorts, remotePorts, isTcp, ipVersion, state);
        return true;
    }

    private bool TryParseNumbers<T>(
        string text,
        string fieldName,
        TryParse<T> parser,
        out List<T> values)
        where T : struct
    {
        values = [];
        foreach (string item in SplitValues(text))
        {
            if (!parser(item, out T value))
            {
                ErrorRequested?.Invoke($"{fieldName} 中包含无效值：{item}");
                values = [];
                return false;
            }

            values.Add(value);
        }

        values = values.Distinct().ToList();
        return true;
    }

    private static List<string> SplitValues(string? text) =>
        (text ?? string.Empty)
        .Split([',', '，', ';', '；'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(value => value.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private delegate bool TryParse<T>(string text, out T value);
}

internal static class ReadOnlyListExtensions
{
    public static int IndexOf<T>(this IReadOnlyList<T> source, T value)
    {
        for (int index = 0; index < source.Count; index++)
        {
            if (EqualityComparer<T>.Default.Equals(source[index], value))
            {
                return index;
            }
        }

        return -1;
    }
}
