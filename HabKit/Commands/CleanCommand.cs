using System.CommandLine;

namespace HabKit.Commands
{
    public class CleanCommand : Command
    {
        public CleanCommand() 
            : base("clean", "Sanitizes the client by deobfuscating methods, and renaming invalid identifiers")
        {
            AddAlias("c");

            // TODO:
        }
    }
}
