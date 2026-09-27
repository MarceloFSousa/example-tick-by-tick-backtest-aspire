using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct TConnectorAccountIdentifierOut
{
    public byte Version;
    public int BrokerID;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
    public char[] AccountID;
    public int AccountIDLength;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
    public char[] SubAccountID;
    public int SubAccountIDLength;
    public long Reserved;

    public override string ToString() => $"{BrokerID} | {new string(AccountID, 0, AccountIDLength)} | {new string(SubAccountID, 0, SubAccountIDLength)} ";
}
