using System.Collections.Generic;

using HabBit.Habbo;

namespace HabBit.Commands
{
    public class CleanCommand : Command
    {
        public Sanitizers Sanitizations { get; set; } = (Sanitizers.Deobfuscate | Sanitizers.RegisterRename | Sanitizers.IdentifierRename);

        public override void Populate(Queue<string> parameters)
        {
            if (parameters.Count == 0) return;
            Sanitizations = Sanitizers.None;

            while (parameters.Count > 0)
            {
                string parameter = parameters.Dequeue();
                switch (parameter)
                {
                    case "-deob": Sanitizations |= Sanitizers.Deobfuscate; break;
                    case "-rr": Sanitizations |= Sanitizers.RegisterRename; break;
                    case "-ir": Sanitizations |= Sanitizers.IdentifierRename; break;
                }
            }
        }
    }
}