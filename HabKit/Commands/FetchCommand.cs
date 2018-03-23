using System.IO;
using System.Threading.Tasks;

using HabKit.Commands.Foundation;

using Sulakore.Habbo;
using Sulakore.Habbo.Web;

namespace HabKit.Commands
{
    [KitCommand("fetch")]
    public class FetchCommand
    {
        [KitArgument("game", "g")]
        public static async Task<HGame> FetchGameAsync(string revision = null)
        {
            if (string.IsNullOrWhiteSpace(revision))
            {
                revision = await HAPI.GetLatestRevisionAsync(HHotel.Com).ConfigureAwait(false);
            }

            string fileName = Path.Combine(Program.OutputDirectory, "gordon", revision, "Habbo.swf");
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));

            await HAPI.DownloadGameAsync(revision, fileName).ConfigureAwait(false);
            return new HGame(fileName);
        }
    }
}