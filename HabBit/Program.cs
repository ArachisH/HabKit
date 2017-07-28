using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using HabBit.Habbo;
using HabBit.Commands;
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

        private const string CHROME_USER_AGENT =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";

        public HGame Game { get; set; }
        public HBOptions Options { get; }

        public Program(string[] args)
        {
            Options = HBOptions.Parse(args);
            if (string.IsNullOrWhiteSpace(Options.FetchRevision))
            {
                Game = new HGame(Options.GameInfo.FullName);
                if (Options.Compression == null)
                {
                    Options.Compression = Game.Compression;
                }
            }
            if (string.IsNullOrWhiteSpace(Options.OutputDirectory))
            {
                if (Options.GameInfo == null)
                {
                    Options.OutputDirectory = Environment.CurrentDirectory;
                }
                else
                {
                    Options.OutputDirectory = Options.GameInfo.DirectoryName;
                }
            }
            else
            {
                Options.OutputDirectory = Path.Combine(
                    Environment.CurrentDirectory, Options.OutputDirectory);
            }
        }
        private void Compare(string[] args)
        {
            using (var game_1 = new HGame(args[0]))
            using (var game_2 = new HGame(args[1]))
            {
                game_1.Disassemble();
                game_2.Disassemble();

                var matchedHashes = new List<string>();
                var oldUnmatched = new Dictionary<string, List<ASMethod>>();
                var unmatchedMethods = new Dictionary<string, List<ASMethod>>();
                foreach (ASMethod method in game_1.ABCFiles[0].Methods)
                {
                    using (var hasher = new MessageHasher(false))
                    {
                        hasher.Write(method);
                        string hash = hasher.GenerateMD5Hash();

                        List<ASMethod> methods = null;
                        if (!unmatchedMethods.TryGetValue(hash, out methods))
                        {
                            methods = new List<ASMethod>();
                            unmatchedMethods.Add(hash, methods);
                        }
                        methods.Add(method);
                    }
                }

                foreach (ASMethod method in game_2.ABCFiles[0].Methods)
                {
                    using (var hasher = new MessageHasher(false))
                    {
                        hasher.Write(method);
                        string hash = hasher.GenerateMD5Hash();

                        if (unmatchedMethods.ContainsKey(hash))
                        {
                            matchedHashes.Add(hash);
                            unmatchedMethods.Remove(hash);
                        }
                        else if (!matchedHashes.Contains(hash))
                        {
                            List<ASMethod> methods = null;
                            if (!oldUnmatched.TryGetValue(hash, out methods))
                            {
                                methods = new List<ASMethod>();
                                oldUnmatched.Add(hash, methods);
                            }
                            methods.Add(method);
                        }
                    }
                }

                var changes = string.Empty;
                foreach (string hash in unmatchedMethods.Keys)
                {
                    changes += $"[{hash}]\r\n{{\r\n";
                    foreach (ASMethod method in unmatchedMethods[hash])
                    {
                        changes += $"    {(method.Container?.QName.Name ?? "Anonymous")}\r\n";
                        changes += $"    {method.ToAS3()}\r\n\r\n";
                    }
                    changes += $"}}\r\n";
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
            if (!string.IsNullOrWhiteSpace(Options.FetchRevision))
            {
                ConsoleEx.WriteLineTitle("Fetching");
                Fetch();
            }
            if (Options.Actions.HasFlag(CommandActions.Disassemble))
            {
                ConsoleEx.WriteLineTitle("Disassembling");
                Disassemble();

                if (Options.Actions.HasFlag(CommandActions.Modify))
                {
                    ConsoleEx.WriteLineTitle("Modifying");
                    Modify();
                }

                // Perform this right after modification, in case the '/clean', and '/dump' command combination is present.
                if (Options.Actions.HasFlag(CommandActions.Extract))
                {
                    ConsoleEx.WriteLineTitle("Extracting");
                    Extract();
                }

                if (Options.Actions.HasFlag(CommandActions.Assemble))
                {
                    ConsoleEx.WriteLineTitle("Assembling");
                    Assemble();
                }
            }
        }
        private void Fetch()
        {
            var flashClientUrl = string.Empty;
            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.UserAgent] = CHROME_USER_AGENT;
                using (var gameDataStream = new StreamReader(client.OpenRead(EXTERNAL_VARIABLES_URL)))
                {
                    while (!gameDataStream.EndOfStream)
                    {
                        string line = gameDataStream.ReadLine();
                        if (!line.StartsWith("flash.client.url")) continue;

                        int urlStart = (line.IndexOf('=') + 1);
                        flashClientUrl = ("http:" + line.Substring(urlStart) + "Habbo.swf");

                        int revisionStart = (line.IndexOf("gordon/") + 7);
                        string revision = line.Substring(revisionStart, (line.Length - revisionStart) - 1);

                        if (Options.FetchRevision == "?")
                        {
                            Options.FetchRevision = revision;
                        }
                        else
                        {
                            flashClientUrl = flashClientUrl.Replace(
                                revision, Options.FetchRevision);
                        }
                        break;
                    }
                }

                var remoteUri = new Uri(flashClientUrl);
                Options.GameInfo = new FileInfo(Path.Combine(Options.OutputDirectory, remoteUri.LocalPath.Substring(8)));
                Options.OutputDirectory = Directory.CreateDirectory(Options.GameInfo.DirectoryName).FullName;

                Console.Write($"Downloading Client({Options.FetchRevision})...");
                client.DownloadFile(remoteUri, Options.GameInfo.FullName);
                ConsoleEx.WriteLineFinished();
            }

            Game = new HGame(Options.GameInfo.FullName);
            if (Options.Compression == null)
            {
                Options.Compression = Game.Compression;
            }
        }
        private void Modify()
        {
            if (Options.BinRepInfo != null)
            {
                var toReplaceIds = string.Join(", ",
                    Options.BinRepInfo.Replacements.Keys.Select(k => k.ToString()));

                Console.Write($"Replacing Binary Data({toReplaceIds})...");
                foreach (DefineBinaryDataTag defBinData in Game.Tags
                    .Where(t => t.Kind == TagKind.DefineBinaryData))
                {
                    byte[] data = null;
                    if (Options.BinRepInfo.Replacements.TryGetValue(defBinData.Id, out data))
                    {
                        defBinData.Data = data;
                        Options.BinRepInfo.Replacements.Remove(defBinData.Id);
                    }
                }
                if (Options.BinRepInfo.Replacements.Count > 0)
                {
                    var failedReplaceIds = string.Join(", ",
                        Options.BinRepInfo.Replacements.Keys.Select(k => k.ToString()));

                    Console.Write($" | Data Replace Failed: Ids({failedReplaceIds})");
                }
                ConsoleEx.WriteLineFinished();
            }

            if (Options.CleanInfo != null)
            {
                Console.Write($"Sanitizing({Options.CleanInfo.Sanitizations})...");
                Game.Sanitize(Options.CleanInfo.Sanitizations);
                ConsoleEx.WriteLineFinished();
            }

            if (Options.HardEPInfo != null)
            {
                Console.Write("Injecting Endpoint...");
                Game.InjectEndPoint(Options.HardEPInfo.Address.Host, Options.HardEPInfo.Address.Port).WriteLineResult();
            }

            if (Options.KeyShouterId != null)
            {
                Console.Write($"Injecting RC4 Key Shouter(Message ID: {Options.KeyShouterId})...");
                Game.InjectKeyShouter((int)Options.KeyShouterId).WriteLineResult();
            }

            if (Options.IsDisablingHandshake)
            {
                Console.Write("Disabling Handshake...");
                Game.DisableHandshake().WriteLineResult();
            }

            if (Options.RSAInfo != null)
            {
                Console.Write("Replacing RSA Keys...");
                Game.ReplaceRSAKeys(Options.RSAInfo.Exponent, Options.RSAInfo.Modulus).WriteLineResult();
            }

            if (Options.IsDisablingHostChecks)
            {
                Console.Write("Disabling Host Checks...");
                Game.DisableHostChecks().WriteLineResult();
            }

            if (Options.IsEnablingAvatarTags)
            {
                Console.Write("Enabling Avatar Tags...");
                Game.EnableAvatarTags().WriteLineResult();
            }

            if (Options.IsEnablingDescriptions)
            {
                Console.Write("Enabling Badge Descriptions...");
                Game.EnableDescriptions().WriteLineResult();
            }

            if (Options.IsInjectingRawCamera)
            {
                Console.Write("Injecting Raw Camera...");
                Game.InjectRawCamera().WriteLineResult();
            }

            if (!string.IsNullOrWhiteSpace(Options.DebugLogger))
            {
                Console.Write($"Injecting Debug Logger(\"{Options.DebugLogger}\")...");
                Game.InjectDebugLogger(Options.DebugLogger).WriteLineResult();
            }

            if (!string.IsNullOrWhiteSpace(Options.MessageLogger))
            {
                Console.Write($"Injecting Message Logger(\"{Options.MessageLogger}\")...");
                Game.InjectMessageLogger(Options.MessageLogger).WriteLineResult();
            }

            if (!string.IsNullOrWhiteSpace(Options.Revision))
            {
                ConsoleEx.WriteLineChanged("Internal Revision Updated", Game.Revision, Options.Revision);
                Game.Revision = Options.Revision;
            }
        }
        private void Extract()
        {
            if (Options.IsDumpingMessageData || Options.MatchInfo != null)
            {
                Console.Write("Generating Message Hashes...");
                Game.GenerateMessageHashes();
                ConsoleEx.WriteLineFinished();
            }

            if (Options.IsExtractingEndPoint)
            {
                Console.Write("Extracting End Point...");
                Tuple<string, int?> endPoint = Game.ExtractEndPoint();

                string endPointPath = Path.Combine(Options.OutputDirectory, "EndPoint.txt");
                using (var endPointOutput = new StreamWriter(endPointPath, false))
                {
                    endPointOutput.Write($"{endPoint.Item1}:{endPoint.Item2}");
                }
                ConsoleEx.WriteLineFinished();
            }

            if (Options.IsDumpingMessageData)
            {
                string msgsPath = Path.Combine(Options.OutputDirectory, "Messages.txt");
                using (var msgsOutput = new StreamWriter(msgsPath, false))
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

            if (Options.MatchInfo != null)
            {
                MatchCommand matchInfo = Options.MatchInfo;
                using (var previousGame = new HGame(matchInfo.PreviousGameInfo.FullName))
                {
                    Console.Write("Preparing Hash Comparison...");
                    previousGame.Disassemble();
                    previousGame.GenerateMessageHashes();
                    ConsoleEx.WriteLineFinished();

                    Console.Write("Matching Outgoing Messages...");
                    ReplaceHeaders(matchInfo.ClientHeadersInfo, previousGame.OutMessages, previousGame.Revision);
                    ConsoleEx.WriteLineFinished();

                    Console.Write("Matching Incoming Messages...");
                    ReplaceHeaders(matchInfo.ServerHeadersInfo, previousGame.InMessages, previousGame.Revision);
                    ConsoleEx.WriteLineFinished();
                }
            }
        }
        private void Assemble()
        {
            string asmdPath = Path.Combine(Options.OutputDirectory, ("asmd_" + Options.GameInfo.Name));
            using (var asmdStream = File.Open(asmdPath, FileMode.Create))
            using (var asmdOutput = new FlashWriter(asmdStream))
            {
                Game.Assemble(asmdOutput, (CompressionKind)Options.Compression);
                Console.WriteLine("File Assembled: " + asmdPath);
            }

            if (Options.RSAInfo != null)
            {
                string keysPath = Path.Combine(Options.OutputDirectory, "RSAKeys.txt");
                using (var keysOutput = new StreamWriter(keysPath, false))
                {
                    keysOutput.WriteLine("[E]Exponent: " + Options.RSAInfo.Exponent);
                    keysOutput.WriteLine("[N]Modulus: " + Options.RSAInfo.Modulus);
                    keysOutput.Write("[D]Private Exponent: " + Options.RSAInfo.PrivateExponent);
                    Console.WriteLine("RSA Keys Saved: " + keysPath);
                }
            }
        }
        private void Disassemble()
        {
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

            output.Write($"[{message.Id}, {message.Hash}] = {name}{constructorSig}");
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
                SortedList<ushort, MessageItem> hashes = null;
                if (!hashCollisions.TryGetValue(message.Hash, out hashes))
                {
                    hashes = new SortedList<ushort, MessageItem>();
                    hashCollisions.Add(message.Hash, hashes);
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
                if (hashCollisions.ContainsKey(message.Hash)) continue;
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

        private void ReplaceHeaders(FileInfo file, IDictionary<ushort, MessageItem> previousMessages, string revision)
        {
            int totalMatches = 0, matchAttempts = 0;
            using (var fileOutput = new StreamReader(file.FullName))
            using (var replaceOutput = new StreamWriter(Path.Combine(Options.OutputDirectory, file.Name), false))
            {
                if (!Options.MatchInfo.MinimalComments)
                {
                    replaceOutput.WriteLine("// Current: " + Game.Revision);
                    replaceOutput.WriteLine("// Previous: " + revision);
                }
                while (!fileOutput.EndOfStream)
                {
                    string line = fileOutput.ReadLine();

                    int possibleCommentIndex = line.IndexOf("//");
                    if (possibleCommentIndex != -1)
                    {
                        line = Regex.Replace(line, "([a-z]|[A-Z]|\\s)//(.*?)$", string.Empty, RegexOptions.RightToLeft);
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        if (possibleCommentIndex >= line.Length)
                        {
                            line = line.TrimEnd();
                        }
                    }

                    Match declaration = Regex.Match(line, Options.MatchInfo.Pattern);
                    if (declaration.Success)
                    {
                        ushort prevHeader = 0;
                        bool isCritical = false;
                        var suffix = string.Empty;
                        MessageItem previousMessage = null;
                        List<MessageItem> similarMessages = null;

                        string end = declaration.Groups["end"].Value;
                        string start = declaration.Groups["start"].Value;
                        string headerString = declaration.Groups["id"].Value;

                        matchAttempts++;
                        if (!ushort.TryParse(headerString, out prevHeader))
                        {
                            matchAttempts--;
                            isCritical = true;
                            suffix = " //! Invalid Header";
                        }
                        else if (!previousMessages.TryGetValue(prevHeader, out previousMessage))
                        {
                            matchAttempts--;
                            isCritical = true;
                            headerString = "-1";
                            suffix = $" //! Unknown Message({prevHeader})";
                        }
                        else if (!Game.Messages.TryGetValue(previousMessage.Hash, out similarMessages))
                        {
                            isCritical = true;
                            headerString = "-1";
                            suffix = $" //! Zero Matches({prevHeader})";
                        }
                        else if (similarMessages.Count > 1)
                        {
                            headerString = "-1";
                            suffix = $" //! Duplicate Matches({prevHeader})";
                            foreach (MessageItem similarMessage in similarMessages)
                            {
                                if (previousMessage.Class.QName.Name == similarMessage.Class.QName.Name)
                                {
                                    totalMatches++;
                                    suffix = (" // " + prevHeader);
                                    headerString = similarMessage.Id.ToString();
                                    break;
                                }
                                else
                                {
                                    int previousClassRankTotal = 0;
                                    foreach (MessageReference reference in previousMessage.References)
                                    {
                                        previousClassRankTotal += reference.ClassRank;
                                    }

                                    int similarClassRankTotal = 0;
                                    foreach (MessageReference similarReference in similarMessage.References)
                                    {
                                        similarClassRankTotal += similarReference.ClassRank;
                                    }

                                    if (previousClassRankTotal == similarClassRankTotal)
                                    {
                                        totalMatches++;
                                        suffix = (" // " + prevHeader);
                                        headerString = similarMessage.Id.ToString();
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            totalMatches++;
                            suffix = (" // " + prevHeader);
                            headerString = similarMessages[0].Id.ToString();
                        }
                        if (!isCritical && Options.MatchInfo.MinimalComments)
                        {
                            suffix = null;
                        }
                        line = $"{start}{headerString}{end}{suffix}";
                        line = line.Replace(revision, Game.Revision);
                    }
                    replaceOutput.WriteLine(line);
                }
            }
            Console.Write($" | Matches: {totalMatches}/{matchAttempts}");
        }
    }
}