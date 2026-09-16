using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct TConnectorCancelOrders
{
    public byte Version;
    public TConnectorAccountIdentifier AccountID;
    public TConnectorAssetIdentifier AssetID;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string Password;
}
