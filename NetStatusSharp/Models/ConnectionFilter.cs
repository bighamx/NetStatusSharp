using ProcessViewer;

namespace NetStatusSharp.Models;

public sealed class ConnectionFilter
{
    public ConnectionFilter(
        IEnumerable<string> processNames,
        IEnumerable<int> processIds,
        IEnumerable<ushort> localPorts,
        IEnumerable<ushort> remotePorts,
        bool? isTcp,
        string? ipVersion,
        ConnectionState? state)
    {
        ProcessNames = new HashSet<string>(processNames, StringComparer.OrdinalIgnoreCase);
        ProcessIds = new HashSet<int>(processIds);
        LocalPorts = new HashSet<ushort>(localPorts);
        RemotePorts = new HashSet<ushort>(remotePorts);
        IsTcp = isTcp;
        IpVersion = ipVersion;
        State = state;
    }

    public HashSet<string> ProcessNames { get; }
    public HashSet<int> ProcessIds { get; }
    public HashSet<ushort> LocalPorts { get; }
    public HashSet<ushort> RemotePorts { get; }
    public bool? IsTcp { get; }
    public string? IpVersion { get; }
    public ConnectionState? State { get; }
}
