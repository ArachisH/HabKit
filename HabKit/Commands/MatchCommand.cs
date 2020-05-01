using System.CommandLine;

namespace HabKit.Commands
{
    public class MatchCommand : Command
    {
        public MatchCommand() 
            : base("match", "Replaces the headers in the given Client/Server header files by comparing the hashes with the provided client, against the current one.")
        {
            AddAlias("m");

            // TODO:
        }
    }
}
