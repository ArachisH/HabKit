using System;
using System.IO;
using System.Collections.Generic;

using Flazzy;

namespace HabBit.Utilities
{
    public class HBOptions
    {
        /// <summary>
        /// Gets or sets a value that determines whether to download the latest client.
        /// </summary>
        public bool IsFetchingClient { get; set; }

        /// <summary>
        /// Gets or sets the file info of the file to be process.
        /// </summary>
        public FileInfo ClientInfo { get; set; }

        /// <summary>
        /// Get or sets the compression kind to use on the client after it has been modified.
        /// </summary>
        public CompressionKind? Compression { get; set; }

        /// <summary>
        /// Gets or sets the client's revision value found in the Outgoing[4000] message class handler.
        /// </summary>
        public string Revision { get; set; }

        /// <summary>
        /// Gets or sets a value that determines whether to disable certain methods in the client to allow it to run from any host.
        /// </summary>
        public bool IsDisablingHostChecks { get; set; }

        /// <summary>
        /// Gets or sets a value that determines whether to dump Outgoing/Incoming message data to a text file(Header, SHA1, Constructor Signature).
        /// </summary>
        public bool IsDumpingMessageData { get; set; }

        /// <summary>
        /// Gets or sets whether to disable the handshake process being initiated by the client.
        /// </summary>
        public bool IsDisablingHandshake { get; set; }

        /// <summary>
        /// Gets or sets whether to sanitize the client by deobfuscating methods, and renaming invalid identifiers.
        /// </summary>
        public bool IsSanitizing { get; set; }

        /// <summary>
        /// Gets or sets the public/private RSA Keys.
        /// </summary>
        public HBRSAKeys Keys { get; set; }

        /// <summary>
        /// Gets or sets a value that determines whether to override the client's internal public RSA keys.
        /// </summary>
        public bool IsReplacingRSAKeys { get; set; }

        /// <summary>
        /// Gets or sets a value that determines whether the client will be forced to publicly share the DH(RC4 Stream Cypher Key) private key to any connected parties. 
        /// </summary>
        public bool IsInjectingKeyShouter { get; set; }

        /// <summary>
        /// Get or sets the name of the external function that will be called with the array of values being sent/received.
        /// </summary>
        public string LoggerFunctionName { get; set; }

        /// <summary>
        /// Gets or sets a value that determines whether to call an external function every time a message is being sent/received with the array of values as a parameter.
        /// </summary>
        public bool IsInjectingMessageLogger { get; set; }

        public HBOptions(string[] args)
        {
            ClientInfo = new FileInfo(args[0]);
            var valStack = new Stack<string>();
            var argStack = new Stack<string>(args);
            while (argStack.Count > 0)
            {
                string argName = argStack.Pop();
                switch (argName)
                {
                    default:
                    valStack.Push(argName);
                    break;

                    case "/dcrypto":
                    IsDisablingHandshake = true;
                    break;

                    case "/clean":
                    IsSanitizing = true;
                    break;

                    case "/dump":
                    IsDumpingMessageData = true;
                    break;

                    case "/log":
                    IsInjectingMessageLogger = true;
                    if (valStack.Count > 0)
                    {
                        LoggerFunctionName = valStack.Pop();
                    }
                    break;

                    case "/kshout":
                    IsInjectingKeyShouter = true;
                    break;

                    case "/dhost":
                    IsDisablingHostChecks = true;
                    break;

                    case "/c":
                    {
                        var compression = CompressionKind.None;
                        if (valStack.Count > 0 &&
                            Enum.TryParse(valStack.Pop(), true, out compression))
                        {
                            Compression = compression;
                        }
                        break;
                    }
                    case "/rev":
                    {
                        Revision = valStack.Pop();
                        break;
                    }
                    case "/rsa":
                    {
                        if (valStack.Count > 0)
                        {
                            if (valStack.Count >= 2)
                            {
                                var Modulus = valStack.Pop();
                                var Exponent = valStack.Pop();

                                Keys = new HBRSAKeys(Modulus, Exponent);
                            }
                            else
                            {
                                int keySize = 1024;
                                int.TryParse(valStack.Pop(), out keySize);
                                Keys = new HBRSAKeys(keySize);
                            }
                        }
                        else Keys = new HBRSAKeys();
                        IsReplacingRSAKeys = true;
                        break;
                    }
                }
            }
        }
    }
}