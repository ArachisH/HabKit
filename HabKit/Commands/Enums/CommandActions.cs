using System;

namespace HabKit.Commands
{
    [Flags]
    public enum CommandActions
    {
        None = 0,

        Disassemble = 1,
        Assemble = (Disassemble | 2),
        Extract = (Disassemble | 4),
        Modify = (Disassemble | Assemble | 8)
    }
}