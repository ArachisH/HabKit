using System.Threading.Tasks;

using HabKit.Commands.Foundation;

using Sulakore.Habbo;

namespace HabKit.Commands
{
    [KitCommand("fetch")]
    public class FetchCommand
    {
        [KitArgument("client", "c")]
        public Task<HGame> FetchClientAsync(string revision = null)
        {
            return null;
        }
    }
}