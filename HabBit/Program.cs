﻿using System;
using System.IO;
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
        public HGame Game { get; }
        public HBOptions Options { get; }

        public bool IsModifying
        {
            get
            {
                return (
                    Options.IsSanitizing ||
                    Options.IsReplacingRSAKeys ||
                    Options.IsDisablingHandshake ||
                    Options.IsDisablingHostChecks ||
                    !string.IsNullOrWhiteSpace(Options.Revision
                    ));
            }
        }
        public bool IsExtracting
        {
            get { return Options.IsDumpingMessageData; }
        }
        public string OutputDirectoryName { get; private set; }

        public Program(string[] args)
        {
            Options = new HBOptions(args);
            Game = new HGame(Options.ClientInfo.FullName);

            if (Options.Compression == null)
            {
                Options.Compression = Game.Compression;
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

            Console.WriteLine();
        }
        private void Extract()
        {
            ConsoleEx.WriteLineTitle("Extracting");

            Console.Write("Generating Message Profiles...");
            Game.GenerateMessageProfiles();
            ConsoleEx.WriteLineFinished();

            Console.WriteLine();
        }
        private void Assemble()
        {
            ConsoleEx.WriteLineTitle("Assembling");

            string asmdPath = Path.Combine(OutputDirectoryName, Options.ClientInfo.Name);
            using (var asmdStream = File.Open(asmdPath, FileMode.Create))
            using (var asmdOutput = new FlashWriter(asmdStream))
            {
                Game.Assemble(asmdOutput, (CompressionKind)Options.Compression);
                Console.WriteLine("File Assembled: " + asmdPath);
            }

            if (Options.IsReplacingRSAKeys)
            {
                string keysPath = Path.Combine(Path.Combine(OutputDirectoryName, "RSAKeys.txt"));
                using (var keysStream = File.Open(keysPath, FileMode.Create))
                using (var keysOutput = new StreamWriter(keysStream))
                {
                    keysOutput.WriteLine("[E]Exponent: " + Options.Keys.Exponent);
                    keysOutput.WriteLine("[N]Modulus: " + Options.Keys.Modulus);
                    keysOutput.Write("[D]Private Exponent: " + Options.Keys.PrivateExponent);
                    Console.WriteLine("RSA Keys Saved: " + keysPath);
                }
            }

            if (Options.IsDumpingMessageData)
            {
                string msgsPath = Path.Combine(OutputDirectoryName, "Messages.txt");
                using (var msgsStream = File.Open(msgsPath, FileMode.Create))
                using (var msgsOutput = new StreamWriter(msgsStream))
                {
                    msgsOutput.WriteLine("// Outgoing Messages | " + Game.OutMessages.Count.ToString("n0"));
                    WriteMessages(msgsOutput, "Outgoing", Game.OutMessages);

                    msgsOutput.WriteLine();

                    msgsOutput.WriteLine("// Incoming Messages | " + Game.OutMessages.Count.ToString("n0"));
                    WriteMessages(msgsOutput, "Incoming", Game.InMessages);
                    
                    Console.WriteLine("Messages Saved: " + msgsPath);
                }
            }

            Console.WriteLine();
        }
        private void Disassemble()
        {
            ConsoleEx.WriteLineTitle("Disassembling");
            Game.Disassemble();

            OutputDirectoryName = Path.Combine(Options.ClientInfo.DirectoryName, Game.Revision);
            Directory.CreateDirectory(OutputDirectoryName);

            var productInfo = (ProductInfoTag)Game.Tags
                .First(t => t.Kind == TagKind.ProductInfo);

            Console.WriteLine($"Outgoing Messages: {Game.OutMessages.Count:n0}");
            Console.WriteLine($"Incoming Messages: {Game.InMessages.Count:n0}");
            Console.WriteLine("Compilation Date: {0}", productInfo.CompilationDate);
            Console.WriteLine("Revision: " + Game.Revision);

            Console.WriteLine();
        }

        private void WriteMessage(StreamWriter output, HGame.MessageItem message)
        {
            ASInstance instance = message.Class.Instance;

            string name = instance.QName.Name;
            string sha1 = message.Profile.SHA1;
            string constructorSig = instance.Constructor.ToAS3(true);

            output.WriteLine($"[{message.Header}, {sha1}] = {name}{constructorSig}");
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

                string sha1 = message.Profile.SHA1;
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
                string sha1 = message.Profile.SHA1;
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