using System.Windows.Media;

namespace NetStatusSharp.Models;

public sealed class ConnectionRow
{
    private static readonly Brush TcpBrush = CreateBrush(38, 115, 201);
    private static readonly Brush UdpBrush = CreateBrush(118, 86, 201);
    private static readonly Brush Ipv6Brush = CreateBrush(132, 83, 193);
    private static readonly Brush SuccessBrush = CreateBrush(35, 134, 85);
    private static readonly Brush WarningBrush = CreateBrush(166, 107, 0);
    private static readonly Brush MutedBrush = CreateBrush(98, 112, 131);

    public int Number { get; init; }
    public ImageSource? ProcessIcon { get; init; }
    public required string ProcessName { get; init; }
    public int ProcessId { get; init; }
    public required string Protocol { get; init; }
    public required string IpVersion { get; init; }
    public required string LocalEndpoint { get; init; }
    public required string RemoteEndpoint { get; init; }
    public required string State { get; init; }

    public Brush ProtocolBrush => Protocol == "TCP" ? TcpBrush : UdpBrush;

    public Brush IpVersionBrush => IpVersion == "IPv6" ? Ipv6Brush : MutedBrush;

    public Brush StateBrush => State switch
    {
        "已建立" => SuccessBrush,
        "监听" => TcpBrush,
        "关闭等待" or "时间等待" => WarningBrush,
        _ => MutedBrush
    };

    private static Brush CreateBrush(byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }
}

public sealed record ConnectionQueryResult(
    IReadOnlyList<ConnectionRow> Rows,
    int TcpCount,
    int UdpCount,
    int EstablishedCount);
