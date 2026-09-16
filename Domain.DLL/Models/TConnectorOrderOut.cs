using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct TConnectorOrderOut
{
    public byte Version;
    public TConnectorOrderIdentifier OrderID;
    public TConnectorAccountIdentifierOut AccountID;
    public TConnectorAssetIdentifierOut AssetID;
    public long Quantity;
    public long TradedQuantity;
    public long LeavesQuantity;
    public double Price;
    public double StopPrice;
    public double AveragePrice;
    [MarshalAs(UnmanagedType.U1)]
    public TConnectorOrderSide OrderSide;
    [MarshalAs(UnmanagedType.U1)]
    public TConnectorOrderType OrderType;
    public byte OrderStatus;
    public byte ValidityType;
    public SystemTime Date;
    public SystemTime LastUpdate;
    public SystemTime CloseDate;
    public SystemTime ValidityDate;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string TextMessage;
    public int TextMessageLength;

    // V1
    public long EventID;
}
