using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct TConnectorAssetIdentifierOut
{
    public byte Version;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string Ticker;
    public int TickerLength;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string Exchange;
    public int ExchangeLength;
    public byte FeedType;
}
