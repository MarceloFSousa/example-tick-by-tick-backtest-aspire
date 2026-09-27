using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct TConnectorZeroPosition
{
    public byte Version;
    public TConnectorAccountIdentifier AccountID;
    public TConnectorAssetIdentifier AssetID;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string Password;
    public double Price;

    // V1
    [MarshalAs(UnmanagedType.U1)] public TConnectorPositionType PositionType;

    // V2
    public long MessageID;
}
