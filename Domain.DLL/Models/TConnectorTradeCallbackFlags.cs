using System;

[Flags]
public enum TConnectorTradeCallbackFlags : uint
{
    TC_IS_EDIT = 1,
    TC_LAST_PACKET = 2
}
