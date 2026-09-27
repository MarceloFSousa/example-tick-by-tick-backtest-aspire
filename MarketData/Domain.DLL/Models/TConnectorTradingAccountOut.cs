using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct TConnectorTradingAccountOut
{
    public byte Version;
    public TConnectorAccountIdentifier AccountID;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string BrokerName;
    public int BrokerNameLength;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string OwnerName;
    public int OwnerNameLength;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string SubOwnerName;
    public int SubOwnerNameLength;
    public int AccountFlags;
    public byte AccountType;
}
