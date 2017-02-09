using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using HabBit.Habbo;
using HabBit.Utilities;

using Flazzy;
using Flazzy.IO;
using Flazzy.ABC;
using Flazzy.Tags;

namespace HabBit
{
    public class Program
    {
        private const string EXTERNAL_VARIABLES_URL =
            "https://www.habbo.com/gamedata/external_variables";

        private const string FLASH_CLIENT_URL_FORMAT =
            "http://habboo-a.akamaihd.net/gordon/{0}/Habbo.swf";

        private const string CHROME_USER_AGENT =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";

        public HGame Game { get; set; }
        public HBOptions Options { get; }

        public bool IsModifying
        {
            get
            {
                return (Options.IsSanitizing ||
                    Options.IsReplacingRSAKeys ||
                    Options.IsDisablingHandshake ||
                    Options.IsDisablingHostChecks ||
                    Options.IsInjectingKeyShouter ||
                    Options.IsInjectingMessageLogger ||
                    !string.IsNullOrWhiteSpace(Options.Revision));
            }
        }
        public bool IsExtracting
        {
            get
            {
                return (Options.IsDumpingMessageData ||
                    Options.IsMatchingMessages);
            }
        }

        public Program(string[] args)
        {
            Options = new HBOptions(args);
            if (!Options.IsFetchingClient)
            {
                Game = new HGame(Options.ClientInfo.FullName);
                if (Options.Compression == null)
                {
                    Options.Compression = Game.Compression;
                }
            }
        }
        public static void Main(string[] args)
        {
            try
            {
                Console.CursorVisible = false;

                Version asmVersion = Assembly.GetExecutingAssembly().GetName().Version;
                Console.Title = ("HabBit v" + asmVersion);

                new Program(args).Run();
            }
            finally { Console.CursorVisible = true; }
        }

        private void Run()
        {
            if (Options.IsFetchingClient)
            {
                Fetch();
            }

            Disassemble();
            if (IsModifying)
            {
                Modify();
            }
            if (IsExtracting)
            {
                Extract();
            }
            Assemble();
        }
        private void Fetch()
        {
            string title = "Fetching Client";
            bool isRevisionKnown = !(string.IsNullOrWhiteSpace(Options.RemoteRevision));

            if (isRevisionKnown)
            {
                title += $"({Options.RemoteRevision})";
            }
            ConsoleEx.WriteLineTitle("Fetching Client");

            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.UserAgent] = CHROME_USER_AGENT;
                if (!isRevisionKnown)
                {
                    using (var gameDataStream = new StreamReader(client.OpenRead(EXTERNAL_VARIABLES_URL)))
                    {
                        while (!gameDataStream.EndOfStream)
                        {
                            // Maybe we can use some of this other stuff later, ignore for now though.
                            string line = gameDataStream.ReadLine();
                            if (!line.StartsWith("flash.client.url")) continue;

                            int revisionStart = (line.IndexOf("gordon/") + 7);
                            Options.RemoteRevision = line.Substring(revisionStart, (line.Length - revisionStart) - 1);
                            break;
                        }
                    }
                }

                var remoteUri = new Uri(string.Format(FLASH_CLIENT_URL_FORMAT, Options.RemoteRevision));
                Options.ClientInfo = new FileInfo(Path.Combine(Options.OutputDirectory, remoteUri.LocalPath.Substring(8)));
                Options.OutputDirectory = Directory.CreateDirectory(Options.ClientInfo.DirectoryName).FullName;

                Console.Write($"Downloading Client({Options.RemoteRevision})...");
                client.DownloadFile(remoteUri, Options.ClientInfo.FullName);
                ConsoleEx.WriteLineFinished();
            }

            Game = new HGame(Options.ClientInfo.FullName);
            if (Options.Compression == null)
            {
                Options.Compression = Game.Compression;
            }
        }
        private void Modify()
        {
            ConsoleEx.WriteLineTitle("Modifying");

            if (Options.IsSanitizing)
            {
                Console.Write("Sanitizing...");
                Game.Sanitize();
                ConsoleEx.WriteLineFinished();
            }

            if (Options.IsInjectingKeyShouter)
            {
                Console.Write("Injecting Key Shouter...");
                Game.InjectKeyShouter().WriteLineResult();
            }
            else if (Options.IsDisablingHandshake)
            {
                Console.Write("Disabling Handshake...");
                Game.DisableHandshake().WriteLineResult();
            }
            else if (Options.IsReplacingRSAKeys)
            {
                Console.Write("Replacing RSA Keys...");
                Game.ReplaceRSAKeys(Options.Keys.Exponent, Options.Keys.Modulus).WriteLineResult();
            }

            if (Options.IsDisablingHostChecks)
            {
                Console.Write("Disabling Host Checks...");
                Game.DisableHostChecks().WriteLineResult();
            }

            if (Options.IsInjectingMessageLogger)
            {
                Console.Write("Injecting Message Logger");
                if (!string.IsNullOrWhiteSpace(Options.LoggerFunctionName))
                {
                    Console.Write($"({Options.LoggerFunctionName})");
                }
                Console.Write("...");
                Game.InjectMessageLogger(Options.LoggerFunctionName).WriteLineResult();
            }

            if (!string.IsNullOrWhiteSpace(Options.Revision))
            {
                string oldRevision = Game.Revision;

                Game.Revision = Options.Revision;
                ConsoleEx.WriteLineChanged("Revision Changed", oldRevision, Game.Revision);
            }
        }
        private void Extract()
        {
            ConsoleEx.WriteLineTitle("Extracting");

            Console.Write("Generating Message Profiles...");
            Game.GenerateMessageHashes();
            ConsoleEx.WriteLineFinished();

            if (Options.IsMatchingMessages)
            {
                using (var hashesStream = File.OpenRead(Options.HashesInfo.FullName))
                using (var hashesInput = new BinaryReader(hashesStream))
                {
                    int messageCount = hashesInput.ReadInt32();
                    string revision = hashesInput.ReadString();

                    int duplicateMatches = 0;
                    int unmatchedMessages = 0;
                    Console.Write($"Matching Messages({revision})...");
                    for (int i = 0; i < messageCount; i++)
                    {
                        bool isOutgoing = hashesInput.ReadBoolean();
                        ushort header = hashesInput.ReadUInt16();
                        string sha1 = hashesInput.ReadString();

                        var message = new HGame.MessageItem(header, isOutgoing);
                        message.SHA1 = sha1;

                        List<HGame.MessageItem> messages = null;
                        if (Game.Messages.TryGetValue(sha1, out messages))
                        {
                            if (messages.Count > 1)
                            {
                                duplicateMatches++;
                            }
                        }
                        else unmatchedMessages++;
                    }
                    ConsoleEx.WriteLineFinished();
                }
            }
        }
        private void Assemble()
        {
            ConsoleEx.WriteLineTitle("Assembling");

            string asmdPath = Path.Combine(Options.OutputDirectory, ("asmd_" + Options.ClientInfo.Name));
            using (var asmdStream = File.Open(asmdPath, FileMode.Create))
            using (var asmdOutput = new FlashWriter(asmdStream))
            {
                Game.Assemble(asmdOutput, (CompressionKind)Options.Compression);
                Console.WriteLine("File Assembled: " + asmdPath);
            }

            #region Storing RSA Keys
            if (Options.IsReplacingRSAKeys)
            {
                string keysPath = Path.Combine(Options.OutputDirectory, "RSAKeys.txt");
                using (var keysStream = File.Open(keysPath, FileMode.Create))
                using (var keysOutput = new StreamWriter(keysStream))
                {
                    keysOutput.WriteLine("[E]Exponent: " + Options.Keys.Exponent);
                    keysOutput.WriteLine("[N]Modulus: " + Options.Keys.Modulus);
                    keysOutput.Write("[D]Private Exponent: " + Options.Keys.PrivateExponent);
                    Console.WriteLine("RSA Keys Saved: " + keysPath);
                }
            }
            #endregion

            #region Storing Message Data
            if (Options.IsDumpingMessageData)
            {
                string msgsPath = Path.Combine(Options.OutputDirectory, "Messages.txt");
                using (var msgsStream = File.Open(msgsPath, FileMode.Create))
                using (var msgsOutput = new StreamWriter(msgsStream))
                {
                    msgsOutput.WriteLine("// " + Game.Revision);
                    msgsOutput.WriteLine();

                    msgsOutput.WriteLine("// Outgoing Messages | " + Game.OutMessages.Count.ToString("n0"));
                    WriteMessages(msgsOutput, "Outgoing", Game.OutMessages);

                    msgsOutput.WriteLine();

                    msgsOutput.WriteLine("// Incoming Messages | " + Game.OutMessages.Count.ToString("n0"));
                    WriteMessages(msgsOutput, "Incoming", Game.InMessages);

                    Console.WriteLine("Messages Saved: " + msgsPath);
                }

                string hashesPath = Path.Combine(Options.OutputDirectory, "Messages.hshs");
                using (var hashesStream = File.Open(hashesPath, FileMode.Create))
                using (var hashesOutput = new BinaryWriter(hashesStream))
                {
                    hashesStream.Position += 4;
                    hashesOutput.Write(Game.Revision);

                    int count = 0;
                    foreach (KeyValuePair<ushort, HGame.MessageItem> messagePair in
                        Game.OutMessages.Concat(Game.InMessages))
                    {
                        count++;
                        ushort header = messagePair.Key;
                        HGame.MessageItem message = messagePair.Value;

                        hashesOutput.Write(message.IsOutgoing);
                        hashesOutput.Write(header);
                        hashesOutput.Write(message.SHA1);
                    }

                    hashesStream.Position = 0;
                    hashesOutput.Write(count);
                    Console.WriteLine("Message Hashes Saved: " + hashesPath);
                }
            }
            #endregion
        }
        private void Disassemble()
        {
            ConsoleEx.WriteLineTitle("Disassembling");
            Game.Disassemble();

            if (!Options.OutputDirectory.EndsWith(Game.Revision))
            {
                string directoryName = Path.Combine(Options.OutputDirectory, Game.Revision);
                Options.OutputDirectory = Directory.CreateDirectory(directoryName).FullName;
            }

            var productInfo = (ProductInfoTag)Game.Tags
                .First(t => t.Kind == TagKind.ProductInfo);

            Console.WriteLine($"Outgoing Messages: {Game.OutMessages.Count:n0}");
            Console.WriteLine($"Incoming Messages: {Game.InMessages.Count:n0}");
            Console.WriteLine("Compilation Date: {0}", productInfo.CompilationDate);
            Console.WriteLine("Revision: " + Game.Revision);
        }

        private void WriteMessage(StreamWriter output, HGame.MessageItem message)
        {
            ASInstance instance = message.Class.Instance;

            string name = instance.QName.Name;
            string constructorSig = instance.Constructor.ToAS3(true);

            output.Write($"[{message.Header}, {message.SHA1}] = {name}{constructorSig}");
            if (!message.IsOutgoing)
            {
                output.Write($"[Parser: {message.Parser.Instance.QName.Name}]");
            }
            output.WriteLine();
        }
        private void WriteMessages(StreamWriter output, string title, IDictionary<ushort, HGame.MessageItem> messages)
        {
            var deadMessages = new SortedDictionary<ushort, HGame.MessageItem>();
            var hashCollisions = new Dictionary<string, SortedList<ushort, HGame.MessageItem>>();
            foreach (HGame.MessageItem message in messages.Values)
            {
                if (message.References.Count == 0)
                {
                    deadMessages.Add(message.Header, message);
                    continue;
                }

                string sha1 = message.SHA1;
                SortedList<ushort, HGame.MessageItem> hashes = null;
                if (!hashCollisions.TryGetValue(sha1, out hashes))
                {
                    hashes = new SortedList<ushort, HGame.MessageItem>();
                    hashCollisions.Add(sha1, hashes);
                }
                hashes.Add(message.Header, message);
            }

            string[] keys = hashCollisions.Keys.ToArray();
            foreach (string hash in keys)
            {
                if (hashCollisions[hash].Count > 1) continue;
                hashCollisions.Remove(hash);
            }

            foreach (HGame.MessageItem message in messages.Values)
            {
                string sha1 = message.SHA1;
                if (hashCollisions.ContainsKey(sha1)) continue;
                if (message.References.Count == 0) continue;

                output.Write(title);
                WriteMessage(output, message);
            }

            if (hashCollisions.Count > 0)
            {
                output.WriteLine();
                output.WriteLine($"// {title} Message Hash Collisions");
                foreach (SortedList<ushort, HGame.MessageItem> hashes in hashCollisions.Values)
                {
                    if (hashes.Count < 2) continue;
                    foreach (HGame.MessageItem message in hashes.Values)
                    {
                        output.Write(title);
                        output.Write($"[Collisions: {hashes.Count}]");
                        WriteMessage(output, message);
                    }
                }
            }

            output.WriteLine();
            output.WriteLine($"// {title} Dead Messages");
            foreach (HGame.MessageItem message in deadMessages.Values)
            {
                output.Write(title);
                output.Write("[Dead]");
                WriteMessage(output, message);
            }
        }
    }
}