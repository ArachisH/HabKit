using System;
using System.Reflection;
using System.Threading.Tasks;

using HabKit.Utilities;

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

            await Task.Delay(100);

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
            "   ╚═╝  ╚═╝╚═╝  ╚═╝╚═════╝ ╚═╝  ╚═╝╚═╝   ╚═╝ Version: ".Append(LOGO_COLOR);
            Assembly.GetExecutingAssembly().GetName().Version.ToString(3).AppendLine();

            Logger.AppendLine();
            Logger.AppendLine();
        }
    }
}