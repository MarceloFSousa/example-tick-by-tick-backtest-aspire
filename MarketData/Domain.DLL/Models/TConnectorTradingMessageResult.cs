using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct TConnectorTradingMessageResult
{
    public byte Version;

    // V0
    public int BrokerID;
    public TConnectorOrderIdentifier OrderID;
    public long MessageID;
    public TConnectorTradingMessageResultCode ResultCode;
    [MarshalAs(UnmanagedType.LPWStr)] public string Message;
    public int MessageLength;
}
