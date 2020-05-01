using System;
using System.IO;
using System.CommandLine;
using System.Threading.Tasks;
using System.CommandLine.Invocation;

using HabKit.Utilities;

using Sulakore.Habbo;
using Sulakore.Habbo.Web;

namespace HabKit.Commands
{
    public class FetchCommand : Command
    {
        private int? _lockedCursorLeftPosition;

        public FetchCommand()
            : base("fetch", "Download client builds from Habbo")
        {
            AddAlias("f");

            AddArgument(new Argument<string>("revision")
            {
                Arity = ArgumentArity.ZeroOrOne
            });

            Handler = CommandHandler.Create<DirectoryInfo, string>(FetchAsync);
        }

        public async Task FetchAsync(DirectoryInfo output, string revision)
        {
            if (string.IsNullOrWhiteSpace(revision))
            {
                revision = await HAPI.GetLatestRevisionAsync(HHotel.Com).ConfigureAwait(false);
            }

            var clientDirectory = output.CreateSubdirectory(Path.Combine("gordon", revision));
            string clientFileName = Path.Combine(clientDirectory.FullName, "Habbo.swf");

            ("Fetching: ", $@"\gordon\{revision}\Habbo.swf", " >> ").Append(null, ConsoleColor.Yellow, null);
            await HAPI.DownloadGameAsync(revision, clientFileName, ReportFetchProgress).ConfigureAwait(false);

            KLogger.ClearLine();
            ("Fetched: ", $@"\gordon\{revision}\Habbo.swf").AppendLine(null, ConsoleColor.Yellow);
            KLogger.EmptyLine();
        }

        private void ReportFetchProgress(double percentage)
        {
            _lockedCursorLeftPosition ??= Console.CursorLeft;
            
            KLogger.ClearLine(leftOffset: (int)_lockedCursorLeftPosition);
            Console.Write($"{percentage:0}%");

            if (percentage >= 100)
            {
                _lockedCursorLeftPosition = null;
            }
        }
    }
}
