using System;
using System.IO;
using System.Collections.Generic;

using Flazzy;

namespace HabBit.Utilities
{
    public class HBOptions
    {
        /// <summary>
        /// Gets or sets the output directory where all the files will be saved to.
        /// </summary>
        public string OutputDirectory { get; set; }

        /// <summary>
        /// Gets or sets a value that determines whether to download the latest client.
        /// </summary>
        public bool IsFetchingClient { get; set; }

        /// <summary>
        /// Gets or sets a value that determines whether to enable the client's internal debug function.
        /// </summary>
        public bool IsEnablingDebugLogger { get; set; }

        /// <summary>
        /// Get or sets the name of the external function that will be invoked when a 'log(... args)' task is performed in the client.
        /// </summary>
        public string DebugLogFunctionName { get; set; }

        /// <summary>
        /// Gets or sets the remote revion of the client to fetch.
        /// </summary>
        public string RemoteRevision { get; set; }

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
        /// Gets or sets whether to match the provided file of hashes against the current client hashes.
        /// </summary>
        public bool IsMatchingMessages { get; set; }

        /// <summary>
        /// Gets or sets the path of the file containing the hashes to match against the current client hashes.
        /// </summary>
        public FileInfo HashesInfo { get; set; }

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
        public string MessageLogFunctionName { get; set; }

        /// <summary>
        /// Gets or sets a value that determines whether to call an external function every time a message is being sent/received with the array of values as a parameter.
        /// </summary>
        public bool IsInjectingMessageLogger { get; set; }

        public HBOptions(string[] args)
        {
            ClientInfo = new FileInfo(args[0]);
            if (!ClientInfo.Exists)
            {
                ClientInfo = null;
            }

            var valus = new Stack<string>();
            var arguments = new Stack<string>(args);
            while (arguments.Count > 0)
            {
                string argument = arguments.Pop();
                if (!ParseArgument(argument, valus))
                {
                    valus.Push(argument);
                }
            }

            if (string.IsNullOrWhiteSpace(OutputDirectory))
            {
                if (ClientInfo == null)
                {
                    OutputDirectory = Environment.CurrentDirectory;
                }
                else
                {
                    OutputDirectory = ClientInfo.DirectoryName;
                }
            }
            else OutputDirectory = Path.Combine(Environment.CurrentDirectory, OutputDirectory);
        }

        private bool ParseArgument(string argument, Stack<string> values)
        {
            switch (argument)
            {
                #region Argument: /dcrypto
                case "/dcrypto":
                IsDisablingHandshake = true;
                break;
                #endregion

                #region Argument: /fetch
                case "/fetch":
                {
                    if (values.Count > 0)
                    {
                        RemoteRevision = values.Pop();
                    }
                    IsFetchingClient = true;
                    break;
                }
                #endregion

                #region Argument: /clean
                case "/clean":
                IsSanitizing = true;
                break;
                #endregion

                #region Argument: /dump
                case "/dump":
                IsDumpingMessageData = true;
                break;
                #endregion

                #region Argument: /dlog
                case "/dlog":
                {
                    IsEnablingDebugLogger = true;
                    if (values.Count > 0)
                    {
                        DebugLogFunctionName = values.Pop();
                    }
                    break;
                }
                #endregion

                #region Argument: /mlog
                case "/mlog":
                {
                    IsInjectingMessageLogger = true;
                    if (values.Count > 0)
                    {
                        MessageLogFunctionName = values.Pop();
                    }
                    break;
                }
                #endregion

                #region Argument: /kshout
                case "/kshout":
                IsInjectingKeyShouter = true;
                break;
                #endregion

                #region Argument: /dhost
                case "/dhost":
                IsDisablingHostChecks = true;
                break;
                #endregion

                #region Argument /match
                case "/match":
                {
                    IsMatchingMessages = true;
                    HashesInfo = new FileInfo(values.Pop());
                    break;
                }
                #endregion

                #region Argument: /c
                case "/c":
                {
                    var compression = CompressionKind.None;
                    if (values.Count > 0 &&
                        Enum.TryParse(values.Pop(), true, out compression))
                    {
                        Compression = compression;
                    }
                    break;
                }
                #endregion

                #region Argument: /rev
                case "/rev":
                Revision = values.Pop();
                break;
                #endregion

                #region Argument: /rsa
                case "/rsa":
                {
                    if (values.Count >= 2)
                    {
                        string modulus = values.Pop();
                        string exponent = values.Pop();
                        Keys = new HBRSAKeys(modulus, exponent);
                    }
                    else if (values.Count == 1)
                    {
                        int keySize = 1024;
                        int.TryParse(values.Pop(), out keySize);
                        Keys = new HBRSAKeys(keySize);
                    }
                    else Keys = new HBRSAKeys();
                    IsReplacingRSAKeys = true;
                    break;
                }
                #endregion

                #region Argument: /dir
                case "/dir":
                {
                    OutputDirectory = values.Pop();
                    break;
                }
                #endregion

                default: return false;
            }
            return true;
        }
    }
}