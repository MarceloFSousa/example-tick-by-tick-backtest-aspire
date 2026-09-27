using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct TConnectorSendOrder
{
    public byte Version;
    public TConnectorAccountIdentifier AccountID;
    public TConnectorAssetIdentifier AssetID;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string Password;
    [MarshalAs(UnmanagedType.U1)]
    public TConnectorOrderType OrderType;
    [MarshalAs(UnmanagedType.U1)]
    public TConnectorOrderSide OrderSide;
    public double Price;
    public double StopPrice;
    public long Quantity;

    // V1
    public long MessageID;
}
