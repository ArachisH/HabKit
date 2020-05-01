using System;
using System.IO;
using System.Text;
using System.Linq;
using System.CommandLine;
using System.Security.Cryptography;
using System.CommandLine.Invocation;

using HabKit.Utilities;

using Sulakore.Crypto;
using Sulakore.Habbo.Web;

using Flazzy;
using Flazzy.IO;
using Flazzy.Tags;

namespace HabKit.Commands
{
    public class ClientCommand : RootCommand
    {
        public CompressionKind? Compression { get; set; }

        public ClientCommand()
            : base("Modify and inspect the Habbo client")
        {
            AddArgument(new Argument<FileInfo>("input")
            {
                Description = "Path to the Habbo client file",
            }.ExistingOnly());

            //TODO: Descriptions
            AddOption(new Option<CompressionKind>("--compression"));

            AddOption(new Option<bool>("--disable-crypto"));
            AddOption(new Option<bool>("--disable-host-checks"));

            AddOption(new Option<bool>("--enable-game-center"));
            AddOption(new Option<bool>("--enable-descriptions"));
            AddOption(new Option<bool>("--raw-camera"));
            
            AddOption(new Option<int>("--key-shouter"));
            AddOption(new Option<int>("--endpoint-shouter"));
            AddOption(new Option<string>("--endpoint"));

            AddOption(new Option<string[]>("--rsa"));

            Handler = CommandHandler.Create(typeof(ClientCommand).GetMethod(nameof(HandleClientCommand)));
        }

        public void HandleClientCommand(FileInfo input, CompressionKind compression, DirectoryInfo output,
            bool disableCrypto, bool disableHostChecks, bool enableGameCenter, bool enableDescriptions, bool rawCamera,
            string[] rsa, string endpoint, int keyShouter = -1, int endpointShouter = -1)
        {
            //TODO: Argument.AddValidator(ValidateSymbol<ArgumentResult>)

            HGame clientFile = new HGame(input.FullName);
            Disassemble(clientFile);

            bool clientModified = false;

            if (disableCrypto)
            {
                "Disabling Crypto >> ".Append();
                clientModified |= clientFile.DisableHandshake().WriteResult();
            }

            if (disableHostChecks)
            {
                "Disabling Host Checks >> ".Append();
                clientModified |= clientFile.DisableHostChecks().WriteResult();
            }

            if (enableGameCenter)
            {
                "Enabling Game Center >> ".Append();
                clientModified |= clientFile.EnableGameCenterIcon().WriteResult();
            }

            if (enableDescriptions)
            {
                "Enabling Descriptions >> ".Append();
                clientModified |= EnableDescriptions(clientFile).WriteResult();
            }

            if (rawCamera)
            {
                "Injecting Raw Camera >> ".Append();
                clientModified |= clientFile.InjectRawCamera().WriteResult();
            }

            if (keyShouter != -1)
            {
                ("Injecting Key Shouter[", keyShouter, "] >> ").Append(null, ConsoleColor.Magenta, null);
                clientModified |= clientFile.InjectKeyShouter(keyShouter).WriteResult();
            }

            if (endpointShouter != -1)
            {
                ("Injecting EndPoint Shouter[", endpointShouter, "] >> ").Append(null, ConsoleColor.Magenta, null);
                clientModified |= clientFile.InjectEndPointShouter(endpointShouter).WriteResult();
            }

            if (!string.IsNullOrEmpty(endpoint))
            {
                var addressUri = new Uri("http://" + endpoint);

                ("Injecting EndPoint[", endpoint, "] >> ").Append(null, ConsoleColor.Magenta, null);
                clientModified |= clientFile.InjectEndPoint(addressUri.DnsSafeHost, addressUri.Port).WriteResult();
            }

            //TODO: Finish

            if (clientModified)
            {
                Assemble(clientFile, compression, output);
            }
        }
        private bool EnableDescriptions(HGame game)
        {
            int replaceCount = 0;
            foreach (DefineBinaryDataTag binTag in game.Tags.Where(t => t.Kind == TagKind.DefineBinaryData))
            {
                string nameChunk = Encoding.UTF8.GetString(binTag.Data[47..72]);
                if (nameChunk.StartsWith("name=\"badge_details\""))
                {
                    replaceCount++;
                    binTag.Data = Encoding.UTF8.GetBytes(KResources.ReadEmbeddedData("Badge_Details.xml"));
                }
                else if (nameChunk.StartsWith("name=\"furni_view\""))
                {
                    if (Encoding.UTF8.GetString(binTag.Data[109..145]) != "7093FB25-BA90-F4A0-8830-2C0B7A06AAF6") continue;
        
                    replaceCount++;
                    binTag.Data = Encoding.UTF8.GetBytes(KResources.ReadEmbeddedData("Furni_View.xml"));
                }
                if (replaceCount >= 2) break;
            }
            return game.EnableDescriptions().WriteResult();
        }

        private bool InjectRSAKeys(HGame game, string[] values, DirectoryInfo output)
        {
            string exponent = "3";
            string modulus = "86851dd364d5c5cece3c883171cc6ddc5760779b992482bd1e20dd296888df91b33b936a7b93f06d29e8870f703a216257dec7c81de0058fea4cc5116f75e6efc4e9113513e45357dc3fd43d4efab5963ef178b78bd61e81a14c603b24c8bcce0a12230b320045498edc29282ff0603bc7b7dae8fc1b05b52b2f301a9dc783b7";
            string? privateExponent = "59ae13e243392e89ded305764bdd9e92e4eafa67bb6dac7e1415e8c645b0950bccd26246fd0d4af37145af5fa026c0ec3a94853013eaae5ff1888360f4f9449ee023762ec195dff3f30ca0b08b8c947e3859877b5d7dced5c8715c58b53740b84e11fbc71349a27c31745fcefeeea57cff291099205e230e0c7c27e8e1c0512b";

            switch (values.Length)
            {
                // Use default public key values
                case 0: break;

                // Use the give value as the RSA key size to generate new public keys
                case 1:
                {
                    int keySize = int.Parse(values[0]);
                    using var keyExchange = new HKeyExchange(keySize);
                    
                    RSAParameters rsaKeys = keyExchange.RSA.ExportParameters(true); //TODO: compare
                    modulus = keyExchange.Modulus.ToString("x");
                    exponent = keyExchange.Exponent.ToString("x");
                    privateExponent = keyExchange.PrivateExponent.ToString("x");
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

            string keysPath = Path.Combine(output.FullName, "RSAKeys.txt");
            using (var keysOutput = new StreamWriter(keysPath, false))
            {
                keysOutput.WriteLine("[E]Exponent: " + exponent);
                keysOutput.WriteLine("[N]Modulus: " + modulus);
                keysOutput.Write("[D]Private Exponent: " + privateExponent ?? "null");
            }

            "Injecting RSA Keys >> ".Append();
            return game.InjectRSAKeys(exponent, modulus).WriteResult();
        }

        // TODO: replace binaries

        protected void Assemble(HGame game, CompressionKind? compression, DirectoryInfo outputDirectory)
        {
            ("=====[ ", "Assembling", " ]=====").AppendLine(null, ConsoleColor.Cyan, null);
            string asmdPath = Path.Combine(outputDirectory.FullName, "asmd_" + new FileInfo(game.Location).Name);
            
            using var asmdStream = File.Open(asmdPath, FileMode.Create);
            using var asmdOutput = new FlashWriter(asmdStream);
            
            game.Assemble(asmdOutput, compression ?? game.Compression);
            Console.WriteLine("File Assembled: " + asmdPath);
        }
        protected void Disassemble(HGame game)
        {
            ("=====[ ", "Disassembling", " ]=====").AppendLine(null, ConsoleColor.Cyan, null);
            game.Disassemble(true);

            ("Images: ", game.Tags.Count(t => t.Kind == TagKind.DefineBitsLossless2).ToString("n0")).AppendLine();
            ("Binaries: ", game.Tags.Count(t => t.Kind == TagKind.DefineBinaryData).ToString("n0")).AppendLine();
            ("Incoming Messages: ", game.In.Count().ToString("n0")).AppendLine();
            ("Outgoing Messages: ", game.Out.Count().ToString("n0")).AppendLine();
            ("Revision: ", game.Revision).AppendLine();

            KLogger.EmptyLine();
        }
    }
}