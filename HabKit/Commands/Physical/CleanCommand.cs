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

        protected override void Execute(ref HGame game)
        {
            var sanitation = Sanitizers.None;
            if (IsDeobfuscating) sanitation |= Sanitizers.Deobfuscate;
            if (IsRenamingRegisters) sanitation |= Sanitizers.RegisterRename;
            if (IsRenamingIdentifiers) sanitation |= Sanitizers.IdentifierRename;

            game.Sanitize(sanitation);
        }
    }
}