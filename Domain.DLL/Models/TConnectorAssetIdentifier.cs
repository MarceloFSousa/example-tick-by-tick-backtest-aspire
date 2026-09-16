using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct TConnectorAssetIdentifier
{
    public byte Version;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string Ticker;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string Exchange;
    public byte FeedType;

    public override string ToString() => $"{Ticker}:{Exchange}";
}
