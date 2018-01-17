using System.Collections.Generic;

using HabKit.Utilities;

using Sulakore.Habbo;

namespace HabKit.Commands.Physical
{
    [PhysicalCommand("clean")]
    public class CleanCommand : Command
    {
        [PhysicalArgument("rename-registers", Alias = 'r')]
        public bool IsRenamingRegisters { get; }

        [PhysicalArgument("rename-identifiers", Alias = 'i')]
        public bool IsRenamingIdentifiers { get; }

        [PhysicalArgument("deobfuscate", Alias = 'd')]
        public bool IsDeobfuscating { get; }

        public CleanCommand(HOptions options, Queue<string> arguments)
            : base(options, arguments)
        { }

        protected override void Execute()
        {
            var sanitation = Sanitizers.None;
            if (IsDeobfuscating) sanitation |= Sanitizers.Deobfuscate;
            if (IsRenamingRegisters) sanitation |= Sanitizers.RegisterRename;
            if (IsRenamingIdentifiers) sanitation |= Sanitizers.IdentifierRename;

            Game.Sanitize(sanitation);
        }
    }
}