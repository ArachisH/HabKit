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
using Sulakore.Network;

namespace HabKit.Commands
{
    [KitCommand("client")]
    public class ClientCommand : KitCommand, IDisposable
    {
        private int? _lockedCursorLeftPosition;

        [KitArgument(0)]
        public HGame Game { get; set; }

        //[KitArgument(KitAction.None, "fetch", 'f')]
        public HHotel FetchHotelTarget { get; set; } = HHotel.Com;

        public ClientCommand(Queue<string> arguments)
            : base(arguments)
        { }

        [KitArgument(KitAction.None, "fetch", 'f')]
        public async Task FetchAsync(string revision = null)
        {
            if (string.IsNullOrWhiteSpace(revision))
            {
                revision = await HAPI.GetLatestRevisionAsync(HHotel.Com).ConfigureAwait(false);
            }

            string fileName = Path.Combine(Program.OutputDirectory, "gordon", revision, "Habbo.swf");
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));

            ("Fetching: ", $@"\gordon\{revision}\Habbo.swf", " >> ").Append(null, ConsoleColor.Yellow, null);
            await HAPI.DownloadGameAsync(revision, fileName, ReportFetchProgress).ConfigureAwait(false);

            KLogger.ClearLine();
            ("Fetched: ", $@"\gordon\{revision}\Habbo.swf").AppendLine(null, ConsoleColor.Yellow);
            KLogger.EmptyLine();

            Game = new HGame(fileName);
            Disassemble();
        }

        #region Command Methods
        [KitArgument(KitAction.Modify, "disable-crypto")]
        private bool DisableCrypto()
        {
            "Disabling Crypto >> ".Write();
            return Game.DisableHandshake().WriteResult();
        }

        [KitArgument(KitAction.Modify, "disable-host-checks")]
        private bool DisableHostChecks()
        {
            "Disabling Host Checks >> ".Write();
            return Game.DisableHostChecks().WriteResult();
        }

        [KitArgument(KitAction.Modify, "enable-game-center")]
        private bool EnableGameCenter()
        {
            "Enabling Game Center >> ".Write();
            return Game.EnableGameCenterIcon().WriteResult();
        }

        [KitArgument(KitAction.Modify, "inject-key-shouter")]
        private bool InjectKeyShouter(int messageId = 4001)
        {
            ("Injecting Key Shouter[", messageId, "] >> ").Write(null, ConsoleColor.Magenta, null);
            return Game.InjectKeyShouter(messageId).WriteResult();
        }

        [KitArgument(KitAction.Modify, "inject-raw-camera")]
        private bool InjectRawCamera()
        {
            "Injecting Raw Camera >> ".Write();
            return Game.InjectRawCamera().WriteResult();
        }

        [KitArgument(KitAction.Modify, "inject-rsa")]
        private bool InjectRSAKeys(params string[] values)
        {
            "Injecting RSA Keys >> ".Write();
            throw new NotSupportedException();
        }

        [KitArgument(KitAction.Modify, "replace-binaries")]
        private void ReplaceBinaries()
        {
            "Replacing Binaries >> ".Write();
            throw new NotSupportedException();
        }

        [KitArgument(KitAction.Modify, "replace-images")]
        private void ReplaceImages()
        {
            "Replacing Images >> ".Write();
            throw new NotSupportedException();
        }
        #endregion

        public override async Task<bool> ExecuteAsync()
        {
            if (Game == null)
            {
            }

            Disassemble();

            bool modifed = await base.ExecuteAsync().ConfigureAwait(false);
            if (modifed)
            {
                Assemble();
            }
            return modifed;
            //using (var asmdStream = File.Create(Path.Combine(Program.OutputDirectory, "asmd_Habbo.swf")))
            //{
            //    Game.CopyTo(asmdStream, Flazzy.CompressionKind.ZLIB);
            //}
        }

        protected void Assemble()
        { }
        protected void Disassemble()
        {
            ("=====[ ", "Disassembling", " ]=====").AppendLine(null, ConsoleColor.Cyan, null);
            Game.Disassemble(true);

            ("Images: ", Game.Tags.Count(t => t.Kind == TagKind.DefineBitsLossless2).ToString("n0")).AppendLine();
            ("Binaries: ", Game.Tags.Count(t => t.Kind == TagKind.DefineBinaryData).ToString("n0")).AppendLine();
            ("Incoming Messages: ", Game.InMessages.Count.ToString("n0")).AppendLine();
            ("Outgoing Messages: ", Game.OutMessages.Count.ToString("n0")).AppendLine();
            ("Revision: ", Game.Revision).AppendLine();

            KLogger.EmptyLine();
        }

        private void ReportFetchProgress(double percentage)
        {
            if (_lockedCursorLeftPosition == null)
            {
                _lockedCursorLeftPosition = Console.CursorLeft;
            }

            KLogger.ClearLine(leftOffset: (int)_lockedCursorLeftPosition);
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