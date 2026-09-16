using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct TConnectorOrderIdentifier
{
    public byte Version;
    public long LocalOrderID;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string ClOrderID;

    public override string ToString() => string.IsNullOrWhiteSpace(ClOrderID) ? LocalOrderID.ToString() : ClOrderID;
}
