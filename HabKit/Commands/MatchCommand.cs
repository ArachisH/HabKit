using System.IO;
using System.Collections.Generic;

namespace HabKit.Commands
{
    public class MatchCommand : Command
    {
        public bool MinimalComments { get; set; }
        public bool IsOutputtingHashes { get; set; }

        public int IdentifierIndex { get; set; }
        public string Pattern { get; set; } = @"(?<![a-zA-Z_-])-?[^\s\D][0-9]{0,3}(?=\D*$)";

        public FileInfo PreviousGameInfo { get; set; }
        public FileInfo ClientHeadersInfo { get; set; }
        public FileInfo ServerHeadersInfo { get; set; }

        public override void Populate(Queue<string> parameters)
        {
            while (parameters.Count > 0)
            {
                string parameter = parameters.Dequeue();
                switch (parameter)
                {
                    case "-ii":
                    {
                        if (ushort.TryParse(parameters.Dequeue(), out ushort identifierIndex))
                        {
                            IdentifierIndex = identifierIndex;
                        }
                        break;
                    }
                    case "-h":
                    {
                        IsOutputtingHashes = true;
                        break;
                    }
                    case "-mc":
                    {
                        MinimalComments = true;
                        break;
                    }
                    default:
                    {
                        if (PreviousGameInfo == null)
                        {
                            PreviousGameInfo = new FileInfo(parameter);
                        }
                        else if (ClientHeadersInfo == null)
                        {
                            ClientHeadersInfo = new FileInfo(parameter);
                        }
                        else if (ServerHeadersInfo == null)
                        {
                            ServerHeadersInfo = new FileInfo(parameter);
                        }
                        break;
                    }
                }
            }
        }
    }
}