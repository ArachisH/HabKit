using System.Collections.Generic;

namespace HabKit.Commands
{
    public abstract class Command
    {
        public abstract void Populate(Queue<string> parameters);
    }
}