using NetStatusSharp.Models;
using ProcessViewer;
using System.Diagnostics;
using System.Net;
using System.Windows.Media;

namespace NetStatusSharp.Core;

public sealed class ConnectionProvider
{
    private sealed record ProcessDetails(string Name, ImageSource? Icon);

    public ConnectionQueryResult Query(ConnectionFilter filter, CancellationToken cancellationToken)
    {
        var candidates = new List<Candidate>();

        if (filter.IsTcp is null or true)
        {
            foreach (TcpRow row in NetProcessAPI.GetAllTcpConnections())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!MatchesCommon(filter, row.owningPid, row.LocalPort) ||
                    (filter.RemotePorts.Count > 0 && !filter.RemotePorts.Contains(row.RemotePort)) ||
                    (filter.State.HasValue && row.state != filter.State.Value))
                {
                    continue;
                }

                candidates.Add(new Candidate(
                    row.owningPid,
                    "TCP",
                    FormatEndpoint(row.LocalAddress, row.LocalPort),
                    FormatEndpoint(row.RemoteAddress, row.RemotePort),
                    GetStateText(row.state),
                    row.LocalPort));
            }
        }

        if (filter.IsTcp is null or false)
        {
            foreach (UdpRow row in NetProcessAPI.GetAllUdpConnections())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (MatchesCommon(filter, row.owningPid, row.LocalPort))
                {
                    candidates.Add(new Candidate(
                        row.owningPid,
                        "UDP",
                        FormatEndpoint(row.LocalAddress, row.LocalPort),
                        "-",
                        "-",
                        row.LocalPort));
                }
            }
        }

        var processCache = new Dictionary<int, ProcessDetails>();
        var resolvedRows = new List<(Candidate Candidate, ProcessDetails Process)>();

        foreach (Candidate candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!processCache.TryGetValue(candidate.ProcessId, out ProcessDetails? process))
            {
                process = ResolveProcess(candidate.ProcessId);
                processCache[candidate.ProcessId] = process;
            }

            if (MatchesProcessName(filter.ProcessNames, process.Name))
            {
                resolvedRows.Add((candidate, process));
            }
        }

        resolvedRows.Sort(static (left, right) =>
        {
            int byName = string.Compare(left.Process.Name, right.Process.Name, StringComparison.OrdinalIgnoreCase);
            if (byName != 0) return byName;
            int byPid = left.Candidate.ProcessId.CompareTo(right.Candidate.ProcessId);
            if (byPid != 0) return byPid;
            int byProtocol = string.Compare(left.Candidate.Protocol, right.Candidate.Protocol, StringComparison.Ordinal);
            return byProtocol != 0 ? byProtocol : left.Candidate.LocalPort.CompareTo(right.Candidate.LocalPort);
        });

        var rows = new List<ConnectionRow>(resolvedRows.Count);
        for (int index = 0; index < resolvedRows.Count; index++)
        {
            (Candidate candidate, ProcessDetails process) = resolvedRows[index];
            rows.Add(new ConnectionRow
            {
                Number = index + 1,
                ProcessIcon = process.Icon,
                ProcessName = process.Name,
                ProcessId = candidate.ProcessId,
                Protocol = candidate.Protocol,
                LocalEndpoint = candidate.LocalEndpoint,
                RemoteEndpoint = candidate.RemoteEndpoint,
                State = candidate.State
            });
        }

        return new ConnectionQueryResult(
            rows,
            rows.Count(row => row.Protocol == "TCP"),
            rows.Count(row => row.Protocol == "UDP"),
            rows.Count(row => row.State == "已建立"));
    }

    private static bool MatchesCommon(ConnectionFilter filter, int processId, ushort localPort)
    {
        return (filter.ProcessIds.Count == 0 || filter.ProcessIds.Contains(processId)) &&
               (filter.LocalPorts.Count == 0 || filter.LocalPorts.Contains(localPort));
    }

    private static bool MatchesProcessName(IReadOnlyCollection<string> names, string processName)
    {
        return names.Count == 0 || names.Any(
            name => processName.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    private static ProcessDetails ResolveProcess(int processId)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);
            string name = process.ProcessName;
            string? path = ProcessPathResolver.TryGetExecutablePath(processId);

            return new ProcessDetails(name, ProcessIconCache.Get(path));
        }
        catch
        {
            // 连接采集后进程可能已经退出。
            return new ProcessDetails("Unknown", null);
        }
    }

    private static string FormatEndpoint(IPAddress address, ushort port) => $"{address}:{port}";

    private static string GetStateText(ConnectionState state) => state switch
    {
        ConnectionState.Closed => "已关闭",
        ConnectionState.Listen => "监听",
        ConnectionState.Syn_Sent => "SYN 已发送",
        ConnectionState.Syn_Rcvd => "SYN 已接收",
        ConnectionState.Established => "已建立",
        ConnectionState.Fin_Wait1 => "FIN 等待 1",
        ConnectionState.Fin_Wait2 => "FIN 等待 2",
        ConnectionState.Close_Wait => "关闭等待",
        ConnectionState.Closing => "正在关闭",
        ConnectionState.Last_Ack => "最后确认",
        ConnectionState.Time_Wait => "时间等待",
        ConnectionState.Delete_TCB => "删除 TCB",
        _ => state.ToString()
    };

    private sealed record Candidate(
        int ProcessId,
        string Protocol,
        string LocalEndpoint,
        string RemoteEndpoint,
        string State,
        ushort LocalPort);
}
