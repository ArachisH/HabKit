using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;

using HabKit.Utilities;
using HabKit.Properties;
using HabKit.Commands.Foundation;

using Flazzy;
using Flazzy.IO;
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

        [KitArgument(KitAction.Modify, "compression", 'c')]
        public CompressionKind? Compression { get; set; }

        public ClientCommand(Queue<string> arguments)
            : base(arguments)
        { }

        #region Command Methods
#pragma warning disable IDE0051 // Commands are used at runtime
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

        [KitArgument(KitAction.Modify, "disable-crypto")]
        private bool DisableCrypto()
        {
            "Disabling Crypto >> ".Append();
            return Game.DisableHandshake().WriteResult();
        }

        [KitArgument(KitAction.Modify, "disable-host-checks")]
        private bool DisableHostChecks()
        {
            "Disabling Host Checks >> ".Append();
            return Game.DisableHostChecks().WriteResult();
        }

        [KitArgument(KitAction.Modify, "enable-game-center")]
        private bool EnableGameCenter()
        {
            "Enabling Game Center >> ".Append();
            return Game.EnableGameCenterIcon().WriteResult();
        }

        [KitArgument(KitAction.Modify, "enable-descriptions")]
        private bool EnableDescriptions()
        {
            "Enabling Descriptions >> ".Append();

            int replaceCount = 0;
            foreach (DefineBinaryDataTag binTag in Game.Tags.Where(t => t.Kind == TagKind.DefineBinaryData))
            {
                string nameChunk = Encoding.UTF8.GetString(binTag.Data, 47, 25);
                if (nameChunk.StartsWith("name=\"badge_details\""))
                {
                    replaceCount++;
                    binTag.Data = Encoding.UTF8.GetBytes(Resources.Badge_Details);
                }
                else if (nameChunk.StartsWith("name=\"furni_view\""))
                {
                    if (Encoding.UTF8.GetString(binTag.Data, 109, 36) != "7093FB25-BA90-F4A0-8830-2C0B7A06AAF6") continue;

                    replaceCount++;
                    binTag.Data = Encoding.UTF8.GetBytes(Resources.Furni_View);
                }
                if (replaceCount >= 2) break;
            }
            return Game.EnableDescriptions().WriteResult();
        }

        [KitArgument(KitAction.Modify, "inject-key-shouter")]
        private bool InjectKeyShouter(int messageId = 4001)
        {
            ("Injecting Key Shouter[", messageId, "] >> ").Append(null, ConsoleColor.Magenta, null);
            return Game.InjectKeyShouter(messageId).WriteResult();
        }

        [KitArgument(KitAction.Modify, "inject-endpoint-shouter")]
        private bool InjectEndPointShouter(int messageId = 4000)
        {
            ("Injecting EndPoint Shouter[", messageId, "] >> ").Append(null, ConsoleColor.Magenta, null);
            return Game.InjectEndPointShouter(messageId).WriteResult();
        }

        [KitArgument(KitAction.Modify, "inject-endpoint")]
        private bool InjectEndPoint(string address)
        {
            var addressUri = new Uri("http://" + address);

            ("Injecting EndPoint[", address, "] >> ").Append(null, ConsoleColor.Magenta, null);
            return Game.InjectEndPoint(addressUri.DnsSafeHost, addressUri.Port).WriteResult();
        }

        [KitArgument(KitAction.Modify, "inject-raw-camera")]
        private bool InjectRawCamera()
        {
            "Injecting Raw Camera >> ".Append();
            return Game.InjectRawCamera().WriteResult();
        }

        [KitArgument(KitAction.Modify, "inject-rsa")]
        private bool InjectRSAKeys(string[] values)
        {
            string exponent = "3";
            string modulus = "86851dd364d5c5cece3c883171cc6ddc5760779b992482bd1e20dd296888df91b33b936a7b93f06d29e8870f703a216257dec7c81de0058fea4cc5116f75e6efc4e9113513e45357dc3fd43d4efab5963ef178b78bd61e81a14c603b24c8bcce0a12230b320045498edc29282ff0603bc7b7dae8fc1b05b52b2f301a9dc783b7";
            string privateExponent = "59ae13e243392e89ded305764bdd9e92e4eafa67bb6dac7e1415e8c645b0950bccd26246fd0d4af37145af5fa026c0ec3a94853013eaae5ff1888360f4f9449ee023762ec195dff3f30ca0b08b8c947e3859877b5d7dced5c8715c58b53740b84e11fbc71349a27c31745fcefeeea57cff291099205e230e0c7c27e8e1c0512b";

            switch (values.Length)
            {
                // Use default public key values
                case 0: break;

                // Use the give value as the RSA key size to generate new public keys
                case 1:
                {
                    int keySize = int.Parse(values[0]);
                    using (var rsa = new RSACryptoServiceProvider(keySize))
                    {
                        RSAParameters rsaKeys = rsa.ExportParameters(true);
                        modulus = ToHex(rsaKeys.Modulus);
                        exponent = ToHex(rsaKeys.Exponent);
                        privateExponent = ToHex(rsaKeys.D);
                    }
                    break;
                }

                // Use the given values as the public keys
                case 2:
                {
                    exponent = values[0];
                    modulus = values[1];
                    privateExponent = null;
                    break;
                }
            }

            string keysPath = Path.Combine(Program.OutputDirectory, "RSAKeys.txt");
            using (var keysOutput = new StreamWriter(keysPath, false))
            {
                keysOutput.WriteLine("[E]Exponent: " + exponent);
                keysOutput.WriteLine("[N]Modulus: " + modulus);
                keysOutput.Write("[D]Private Exponent: " + privateExponent);
            }

            "Injecting RSA Keys >> ".Append();
            return Game.InjectRSAKeys(exponent, modulus).WriteResult();
        }

        [KitArgument(KitAction.Modify, "replace-binaries")]
        private void ReplaceBinaries()
        {
            throw new NotSupportedException();
        }

        [KitArgument(KitAction.Modify, "replace-images")]
        private void ReplaceImages()
        {
            throw new NotSupportedException();
        }
#pragma warning restore IDE0051 // Commands are used at runtime
        #endregion

        public override async Task<bool> ExecuteAsync()
        {
            if (Game != null)
            {
                Disassemble();
            }

            bool modifed = await base.ExecuteAsync().ConfigureAwait(false);
            if (modifed || (Compression != null && Compression != Game.Compression))
            {
                KLogger.EmptyLine();
                Assemble();
            }

            return modifed;
        }

        protected void Assemble()
        {
            ("=====[ ", "Assembling", " ]=====").AppendLine(null, ConsoleColor.Cyan, null);
            string asmdPath = Path.Combine(Program.OutputDirectory, "asmd_" + new FileInfo(Game.Location).Name);
            using (var asmdStream = File.Open(asmdPath, FileMode.Create))
            using (var asmdOutput = new FlashWriter(asmdStream))
            {
                Game.Assemble(asmdOutput, Compression ?? Game.Compression);
                Console.WriteLine("File Assembled: " + asmdPath);
            }
        }
        protected void Disassemble()
        {
            ("=====[ ", "Disassembling", " ]=====").AppendLine(null, ConsoleColor.Cyan, null);
            Game.Disassemble(true);

            ("Images: ", Game.Tags.Count(t => t.Kind == TagKind.DefineBitsLossless2).ToString("n0")).AppendLine();
            ("Binaries: ", Game.Tags.Count(t => t.Kind == TagKind.DefineBinaryData).ToString("n0")).AppendLine();
            ("Incoming Messages: ", Game.In.Count().ToString("n0")).AppendLine();
            ("Outgoing Messages: ", Game.Out.Count().ToString("n0")).AppendLine();
            ("Revision: ", Game.Revision).AppendLine();

            KLogger.EmptyLine();
        }

        private string ToHex(byte[] data)
        {
            return new BigInteger(ReverseNull(data)).ToString("x");
        }
        private byte[] ReverseNull(byte[] data)
        {
            bool isNegative = false;
            int newSize = data.Length;
            if (data[0] > 127)
            {
                newSize += 1;
                isNegative = true;
            }

            var reversed = new byte[newSize];
            for (int i = 0; i < data.Length; i++)
            {
                reversed[i] = data[data.Length - (i + 1)];
            }
            if (isNegative)
            {
                reversed[reversed.Length - 1] = 0;
            }
            return reversed;
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