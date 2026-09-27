using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct TConnectorTrade
{
    public byte Version;
    public SystemTime TradeDate;
    public uint TradeNumber;
    public double Price;
    public long Quantity;
    public double Volume;
    public int BuyAgent;
    public int SellAgent;
    public byte TradeType;

    public override string ToString() => $"{TradeDate} | {Price} | {Quantity}";
}
