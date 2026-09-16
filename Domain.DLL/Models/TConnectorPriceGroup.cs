using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct TConnectorPriceGroup
{
    public byte Version;

    public double Price;
    public uint Count;
    public long Quantity;

    public uint PriceGroupFlags;
}
