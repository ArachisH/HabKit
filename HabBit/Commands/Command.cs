using System.Collections.Generic;

namespace HabBit.Commands
{
    public abstract class Command
    {
        public abstract void Populate(Queue<string> parameters);
    }
}