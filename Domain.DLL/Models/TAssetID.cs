using System.Runtime.InteropServices;

namespace Domain.DLL.Models
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TAssetID
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Ticker;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Exchange;
        public int Feed;
    };

}
