using System.Threading.Tasks;

using Sulakore.Habbo;

namespace HabKit.Commands.Physical
{
    [PhysicalCommand("fetch")]
    public class FetchCommand : Command
    {
        [PhysicalArgument("revision", 1, Alias = 'r')]
        public string Revision { get; }

        protected override async Task<HGame> ExecuteAsync(HGame game)
        {
            await Task.Delay(100);

            return game;
        }
    }
}