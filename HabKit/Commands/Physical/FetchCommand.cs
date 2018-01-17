using System.Threading.Tasks;
using System.Collections.Generic;

using HabKit.Utilities;

namespace HabKit.Commands.Physical
{
    [PhysicalCommand("fetch")]
    public class FetchCommand : AsyncCommand
    {
        [PhysicalArgument("revision", 1, Alias = 'r')]
        public string Revision { get; }

        public FetchCommand(HOptions options, Queue<string> arguments)
            : base(options, arguments)
        { }

        protected override async Task ExecuteAsync()
        {
            await Task.Delay(100);
        }
    }
}