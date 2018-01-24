using HabKit.Utilities;

using System;
using System.Reflection;
using System.Threading.Tasks;

namespace HabKit
{
    public class Program
    {
        private const ConsoleColor LOGO_COLOR = ConsoleColor.DarkRed;

        public KitOptions Options { get; }

        public Program(string[] args)
        {
            Options = new KitOptions(args);
        }
        public static void Main(string[] args)
        {
            try
            {
                Console.CursorVisible = false;
                new Program(args).RunAsync().Wait();
            }
            finally { Console.CursorVisible = true; }
        }

        private async Task RunAsync()
        {
            WriteLogo();

            await Options.ApplyAsync().ConfigureAwait(false);

            Console.ReadLine();
        }

        private void WriteLogo()
        {
            Logger.AppendLine();
            Logger.AppendLine();

            "   ██╗  ██╗ █████╗ ██████╗ ██╗  ██╗██╗████████╗".AppendLine(LOGO_COLOR);
            "   ██║  ██║██╔══██╗██╔══██╗██║ ██╔╝██║╚══██╔══╝".AppendLine(LOGO_COLOR);
            "   ███████║███████║██████╔╝█████╔╝ ██║   ██║".AppendLine(LOGO_COLOR);
            "   ██╔══██║██╔══██║██╔══██╗██╔═██╗ ██║   ██║".AppendLine(LOGO_COLOR);
            "   ██║  ██║██║  ██║██████╔╝██║  ██╗██║   ██║".AppendLine(LOGO_COLOR);
            "   ╚═╝  ╚═╝╚═╝  ╚═╝╚═════╝ ╚═╝  ╚═╝╚═╝   ╚═╝".Append(LOGO_COLOR);
            new object[] { "\r\n   [", "https://www.GitHub.com/ArachisH/HabKit", "]" }.Append(LOGO_COLOR, null, LOGO_COLOR); // HMMMMMMMMMMMMMMMM
            new object[] { " [", GetVersion(), "]" }.Append(LOGO_COLOR, null, LOGO_COLOR);

            Logger.AppendLine();
            Logger.AppendLine();
        }
        private string GetVersion()
        {
            string version = ("v" + Assembly.GetExecutingAssembly().GetName().Version.ToString());
            while (version.EndsWith(".0"))
            {
                version = version.Substring(0, version.Length - 2);
            }
            return version;
        }
    }
}