using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
                    (Options.LoopbackPort > 0) ||
                    (Options.KeyShouterId >= 0) ||
                    Options.IsDisablingHandshake ||
                    Options.IsDisablingHostChecks ||
                    Options.IsEnablingDebugLogger ||
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
            if (IsModifying || IsExtracting)
            {
                Disassemble();
            }
            if (IsModifying)
            {
                Modify();
            }
            if (IsExtracting)
            {
                Extract();
            }
            if (IsModifying)
            {
                Assemble();
            }
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
                Game.Sanitize(Sanitization.All);
                ConsoleEx.WriteLineFinished();
            }

            if (Options.LoopbackPort > 0)
            {
                Console.Write("Injecting Loopback Endpoint...");
                Game.InjectLoopbackEndpoint(Options.LoopbackPort).WriteLineResult();
            }

            if (Options.KeyShouterId >= 0)
            {
                Console.Write("Injecting Key Shouter...");
                Game.InjectKeyShouter(Options.KeyShouterId).WriteLineResult();
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

            if (Options.IsEnablingDebugLogger)
            {
                Console.Write("Injecting Message Logger");
                if (!string.IsNullOrWhiteSpace(Options.DebugLogFunctionName))
                {
                    Console.Write($"({Options.DebugLogFunctionName})");
                }
                Console.Write("...");
                Game.EnableDebugLogger(Options.DebugLogFunctionName).WriteLineResult();
            }

            if (Options.IsInjectingMessageLogger)
            {
                Console.Write("Injecting Message Logger");
                if (!string.IsNullOrWhiteSpace(Options.MessageLogFunctionName))
                {
                    Console.Write($"({Options.MessageLogFunctionName})");
                }
                Console.Write("...");
                Game.InjectMessageLogger(Options.MessageLogFunctionName).WriteLineResult();
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

            Console.Write("Generating Message Hashes...");
            Game.GenerateMessageHashes();
            ConsoleEx.WriteLineFinished();

            if (Options.IsMatchingMessages)
            {
                using (var compareGame = new HGame(Options.CompareInfo.FullName))
                {
                    Console.Write("Preparing Hash Comparison...");
                    compareGame.Disassemble();
                    compareGame.GenerateMessageHashes();
                    ConsoleEx.WriteLineFinished();

                    Console.Write($"Matching Outgoing Messages({compareGame.Revision})...");
                    Tuple<int, int> outResult = ReplaceHeaders(Options.ClientHeadersInfo, compareGame.OutMessages, compareGame.Revision);

                    Console.Write($" | Matches: {outResult.Item1}/{outResult.Item2}");
                    ConsoleEx.WriteLineFinished();

                    Console.Write($"Matching Incoming Messages({compareGame.Revision})...");
                    Tuple<int, int> inResult = ReplaceHeaders(Options.ServerHeadersInfo, compareGame.InMessages, compareGame.Revision);

                    Console.Write($" | Matches: {inResult.Item1}/{inResult.Item2}");
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
                .FirstOrDefault(t => t.Kind == TagKind.ProductInfo);

            Console.WriteLine($"Outgoing Messages: {Game.OutMessages.Count:n0}");
            Console.WriteLine($"Incoming Messages: {Game.InMessages.Count:n0}");
            Console.WriteLine("Compilation Date: {0}", (productInfo?.CompilationDate.ToString() ?? "?"));
            Console.WriteLine("Revision: " + Game.Revision);
        }

        private void WriteMessage(StreamWriter output, MessageItem message)
        {
            ASInstance instance = message.Class.Instance;

            string name = instance.QName.Name;
            string constructorSig = instance.Constructor.ToAS3(true);

            output.Write($"[{message.Id}, {message.MD5}] = {name}{constructorSig}");
            if (!message.IsOutgoing && message.Parser != null)
            {
                output.Write($"[Parser: {message.Parser.Instance.QName.Name}]");
            }
            output.WriteLine();
        }
        private void WriteMessages(StreamWriter output, string title, IDictionary<ushort, MessageItem> messages)
        {
            var deadMessages = new SortedDictionary<ushort, MessageItem>();
            var hashCollisions = new Dictionary<string, SortedList<ushort, MessageItem>>();
            foreach (MessageItem message in messages.Values)
            {
                if (message.References.Count == 0)
                {
                    deadMessages.Add(message.Id, message);
                    continue;
                }

                string md5 = message.MD5;
                SortedList<ushort, MessageItem> hashes = null;
                if (!hashCollisions.TryGetValue(md5, out hashes))
                {
                    hashes = new SortedList<ushort, MessageItem>();
                    hashCollisions.Add(md5, hashes);
                }
                hashes.Add(message.Id, message);
            }

            string[] keys = hashCollisions.Keys.ToArray();
            foreach (string hash in keys)
            {
                if (hashCollisions[hash].Count > 1) continue;
                hashCollisions.Remove(hash);
            }

            foreach (MessageItem message in messages.Values)
            {
                string md5 = message.MD5;
                if (hashCollisions.ContainsKey(md5)) continue;
                if (message.References.Count == 0) continue;

                output.Write(title);
                WriteMessage(output, message);
            }

            if (hashCollisions.Count > 0)
            {
                output.WriteLine();
                output.WriteLine($"// {title} Message Hash Collisions");
                foreach (SortedList<ushort, MessageItem> hashes in hashCollisions.Values)
                {
                    if (hashes.Count < 2) continue;
                    foreach (MessageItem message in hashes.Values)
                    {
                        output.Write(title);
                        output.Write($"[Collisions: {hashes.Count}]");
                        WriteMessage(output, message);
                    }
                }
            }

            output.WriteLine();
            output.WriteLine($"// {title} Dead Messages");
            foreach (MessageItem message in deadMessages.Values)
            {
                output.Write(title);
                output.Write("[Dead]");
                WriteMessage(output, message);
            }
        }

        private Tuple<int, int> ReplaceHeaders(FileInfo file, IDictionary<ushort, MessageItem> messages, string revision)
        {
            int totalMatches = 0;
            int totalValidAttempts = 0; // If no message exist, or is an invalid header, do not count towards total attempts. (Not my fault no matches are found, bruh)
            string copyPath = Path.Combine(Options.OutputDirectory, file.Name);
            using (var fileStream = File.OpenRead(file.FullName))
            using (var fileOutput = new StreamReader(fileStream))
            using (var replaceStream = File.Open(copyPath, FileMode.Create))
            using (var replaceOutput = new StreamWriter(replaceStream))
            {
                replaceOutput.WriteLine("// Current: " + Game.Revision);
                replaceOutput.WriteLine("// Previous: " + revision);
                while (!fileOutput.EndOfStream)
                {
                    string line = fileOutput.ReadLine();
                    if (line.Contains("//"))
                    {
                        line = Regex.Replace(line, "//(.*?)$", string.Empty);
                        if (string.IsNullOrWhiteSpace(line)) continue;
                    }

                    line = line.TrimEnd();
                    Match declaration = Regex.Match(line, @"(?<start>(.*?))(?<header>[+-]?[0-9]\d*(\.\d+)?)\b(?<end>[^\r|$]*)");
                    if (declaration.Success)
                    {
                        ushort prevHeader = 0;
                        var suffix = string.Empty;
                        MessageItem prevMessage = null;
                        List<MessageItem> group = null;

                        string end = declaration.Groups["end"].Value;
                        string start = declaration.Groups["start"].Value;
                        string headerString = declaration.Groups["header"].Value;

                        totalValidAttempts++;
                        if (!ushort.TryParse(headerString, out prevHeader))
                        {
                            totalValidAttempts--;
                            suffix = " //! Invalid Header";
                        }
                        else if (!messages.TryGetValue(prevHeader, out prevMessage))
                        {
                            totalValidAttempts--;
                            headerString = "-1";
                            suffix = $" //! Unknown Message({prevHeader})";
                        }
                        else if (!Game.Messages.TryGetValue(prevMessage.MD5, out group))
                        {
                            headerString = "-1";
                            suffix = $" //! Zero Matches({prevHeader})";
                        }
                        else if (group.Count > 1)
                        {
                            // Too risky to set a header from one of the possible messages, set as invalid.
                            // Maybe one day, we'll do a seperate type of scan for these, to check similarities.
                            headerString = "-1";
                            suffix = $" //! Duplicate Matches({prevHeader})";
                        }
                        else
                        {
                            MessageItem message = group[0];
                            headerString = message.Id.ToString();

                            totalMatches++;
                            suffix = (" // " + prevHeader);
                        }
                        line = $"{start}{headerString}{end}{suffix}";
                    }
                    replaceOutput.WriteLine(line);
                }
            }
            return Tuple.Create(totalMatches, totalValidAttempts);
        }
    }
}