using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct TConnectorTradingAccountPosition
{
    public byte Version;
    public TConnectorAccountIdentifier AccountID;
    public TConnectorAssetIdentifier AssetID;
    public long OpenQuantity;
    public double OpenAveragePrice;
    public byte OpenSide;
    public double DailyAverageSellPrice;
    public long DailySellQuantity;
    public double DailyAverageBuyPrice;
    public long DailyBuyQuantity;
    public long DailyQuantityD1;
    public long DailyQuantityD2;
    public long DailyQuantityD3;
    public long DailyQuantityBlocked;
    public long DailyQuantityPending;
    public long DailyQuantityAlloc;
    public long DailyQuantityProvision;
    public long DailyQuantity;
    public long DailyQuantityAvailable;

    // V1
    [MarshalAs(UnmanagedType.U1)] public TConnectorPositionType PositionType;

    // V2
    public long EventID;
}
