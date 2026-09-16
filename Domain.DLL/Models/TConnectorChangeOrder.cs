using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct TConnectorChangeOrder
{
    public byte Version;
    public TConnectorAccountIdentifier AccountID;
    public TConnectorOrderIdentifier OrderID;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string Password;
    public double Price;
    public double StopPrice;
    public long Quantity;

    // V1
    public long MessageID;
}
