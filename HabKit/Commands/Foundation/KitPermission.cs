using System;

namespace HabKit.Commands.Foundation
{
    [Flags]
    public enum KitPermissions
    {
        None = 0,
        Disassemble = 1,
        Assemble = 2,

        Inspect = Disassemble,
        Modify = (Disassemble | Assemble)
    }
}