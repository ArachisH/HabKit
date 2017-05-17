using System.Collections.Generic;

using HabBit.Habbo;

namespace HabBit.Commands
{
    public class CleanCommand : Command
    {
        public GSanitizations Sanitizations { get; set; } = (GSanitizations.Deobfuscate | GSanitizations.RegisterRename | GSanitizations.IdentifierRename);

        public override void Populate(Queue<string> parameters)
        {
            if (parameters.Count > 0)
            {
                Sanitizations = GSanitizations.None;
                while (parameters.Count > 0)
                {
                    string parameter = parameters.Dequeue();
                    switch (parameter)
                    {
                        case "-deob": Sanitizations |= GSanitizations.Deobfuscate; break;
                        case "-rr": Sanitizations |= GSanitizations.RegisterRename; break;
                        case "-ir": Sanitizations |= GSanitizations.IdentifierRename; break;
                    }
                }
            }
        }
    }
}