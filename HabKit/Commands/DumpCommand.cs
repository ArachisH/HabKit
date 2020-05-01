using System.CommandLine;

namespace HabKit.Commands
{
    public class DumpCommand : Command
    {
        public DumpCommand() 
            : base("dump", "Dump message data to a specified format")
        {
            AddAlias("d");

            // TODO: JSON,XML,CSV,TXT?
        }
    }
}
