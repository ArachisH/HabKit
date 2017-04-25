using System;

namespace HabBit.Commands
{
    [Flags]
    public enum CommandActions
    {
        None = 0,
        Fetch = 1,
        Extract = 2,
        Modify = 4
    }
}