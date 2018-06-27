using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using HabKit.Utilities;
using HabKit.Commands.Foundation;

using Flazzy.Tags;

using Sulakore.Habbo;
using Sulakore.Habbo.Web;

namespace HabKit.Commands
{
    [KitCommand("client")]
    public class ClientCommand : KitCommand, IDisposable
    {
        private int? _lockedCursorLeftPosition;

        [KitArgument(0)]
        public HGame Game { get; set; }

        public ClientCommand(Queue<string> arguments)
            : base(arguments)
        { }

        #region Command Methods
        [KitArgument(KitPermissions.Modify, "disable-crypto")]
        private void DisableCrypto()
        { }

        [KitArgument(KitPermissions.Modify, "disable-host-checks")]
        private void DisableHostChecks()
        {
            ("Disabling Host Checks > ").Write();
            Game.DisableHostChecks().WriteResult();

            //KitLogger.ClearLine(leftOffset: Console.CursorLeft - 3);
            //("true").WriteLine();
        }

        [KitArgument(KitPermissions.Modify, "enable-game-center")]
        private void EnableGameCenter()
        { }

        [KitArgument(KitPermissions.None, "fetch", 'f')]
        public async Task FetchAsync(string revision = null)
        {
            if (string.IsNullOrWhiteSpace(revision))
            {
                revision = await HAPI.GetLatestRevisionAsync(HHotel.Com).ConfigureAwait(false);
            }

            string fileName = Path.Combine(Program.OutputDirectory, "gordon", revision, "Habbo.swf");
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));

            ("Fetching Client > ", $@"\gordon\{revision}\Habbo.swf", " | ").Write(null, ConsoleColor.Yellow, null);
            await HAPI.DownloadGameAsync(revision, fileName, ReportFetchProgress).ConfigureAwait(false);
            KitLogger.WriteLine();
            KitLogger.WriteLine();

            Game = new HGame(fileName);
            ProcessGame(Game);
        }

        [KitArgument(KitPermissions.Modify, "inject-key-shouter")]
        private void InjectKeyShouter(int messageId = 4001)
        { }

        [KitArgument(KitPermissions.Modify, "inject-raw-camera")]
        private void InjectRawCamera()
        { }

        [KitArgument(KitPermissions.Modify, "inject-rsa")]
        private void InjectRSAKeys(params string[] values)
        { }

        [KitArgument(KitPermissions.Modify, "replace-binaries")]
        private void ReplaceBinaries()
        { }

        [KitArgument(KitPermissions.Modify, "replace-images")]
        private void ReplaceImages()
        { }
        #endregion

        //public override async Task ExecuteAsync()
        //{
        //    if (Game != null)
        //    {
        //        ProcessGame(Game);
        //    }

        //    await base.ExecuteAsync().ConfigureAwait(false);
        //    //using (var asmdStream = File.Create(Path.Combine(Program.OutputDirectory, "asmd_Habbo.swf")))
        //    //{
        //    //    Game.CopyTo(asmdStream, Flazzy.CompressionKind.ZLIB);
        //    //}
        //}

        private void ProcessGame(HGame game)
        {
            ("=====[ ", "Disassembling", " ]=====").WriteLine(null, ConsoleColor.Cyan, null);
            KitLogger.WriteLine();
            Game.Disassemble(true);

            ("Images: ", Game.Tags.Count(t => t.Kind == TagKind.DefineBitsLossless2).ToString("n0")).WriteLine(null, ConsoleColor.White);
            ("Binary Data: ", Game.Tags.Count(t => t.Kind == TagKind.DefineBinaryData).ToString("n0")).WriteLine(null, ConsoleColor.White);
            ("Incoming Message: ", Game.InMessages.Count.ToString("n0")).WriteLine(null, ConsoleColor.White);
            ("Outgoing Message: ", Game.OutMessages.Count.ToString("n0")).WriteLine(null, ConsoleColor.White);
            ("Revision: ", Game.Revision).WriteLine(null, ConsoleColor.White);

            KitLogger.WriteLine();
        }
        private void ReportFetchProgress(double percentage)
        {
            if (_lockedCursorLeftPosition == null)
            {
                _lockedCursorLeftPosition = Console.CursorLeft;
            }

            KitLogger.ClearLine(leftOffset: (int)_lockedCursorLeftPosition);
            Console.Write($"{percentage:0}%");

            if (percentage >= 100)
            {
                _lockedCursorLeftPosition = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Game?.Dispose();
            }
        }
    }
}