using System.Threading.Tasks;
using System.Collections.Generic;

using HabKit.Utilities;
using HabKit.Commands.Foundation;

using Sulakore.Habbo;

namespace HabKit.Commands
{
    [KitCommand("fetch")]
    public class FetchCommand : KitCommand
    {
        public FetchCommand(KitOptions options, Queue<string> arguments)
            : base(options, arguments)
        { }

        [KitArgument("client", "c")]
        public Task<HGame> FetchClientAsync(string revision = null)
        {
            return null;
        }
    }
}