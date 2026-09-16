using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct TConnectorCancelOrder
{
    public byte Version;
    public TConnectorAccountIdentifier AccountID;
    public TConnectorOrderIdentifier OrderID;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string Password;

    // V1
    public long MessageID;
}
