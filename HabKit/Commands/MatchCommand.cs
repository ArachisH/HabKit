using System.IO;
using System.Collections.Generic;

namespace HabKit.Commands
{
    public class MatchCommand : Command
    {
        public bool MinimalComments { get; set; }
        public string Pattern { get; set; } = @"(?<start>(.*?)[^""])(?<id>[+-]?[0-9]\d*(\.\d+)?)\b(?<end>[^\r|$]*)";

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
                    case "-p":
                    {
                        Pattern = parameters.Dequeue()
                            .Replace(@"^<", @"<")
                            .Replace(@"^^", @"^")
                            .Replace(@"^>", @">");

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