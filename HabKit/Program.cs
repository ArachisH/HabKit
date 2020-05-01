using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.CommandLine;
using System.Threading.Tasks;
using System.CommandLine.Parsing;

using HabKit.Commands;
using HabKit.Utilities;

namespace HabKit
{
    public class Program
    {
        private const ConsoleColor LOGO_COLOR = ConsoleColor.DarkRed;

        public static async Task<int> Main(string[] args)
        {
            Console.Title = nameof(HabKit);
            try
            {
                KLogger.SetCursorVisibility(false);

                KLogger.EmptyLine();
                "   ██╗  ██╗ █████╗ ██████╗ ██╗  ██╗██╗████████╗".AppendLine(LOGO_COLOR);
                "   ██║  ██║██╔══██╗██╔══██╗██║ ██╔╝██║╚══██╔══╝".AppendLine(LOGO_COLOR);
                "   ███████║███████║██████╔╝█████╔╝ ██║   ██║".AppendLine(LOGO_COLOR);
                "   ██╔══██║██╔══██║██╔══██╗██╔═██╗ ██║   ██║".AppendLine(LOGO_COLOR);
                "   ██║  ██║██║  ██║██████╔╝██║  ██╗██║   ██║".AppendLine(LOGO_COLOR);
                "   ╚═╝  ╚═╝╚═╝  ╚═╝╚═════╝ ╚═╝  ╚═╝╚═╝   ╚═╝".AppendLine(LOGO_COLOR);
                KLogger.EmptyLine();
                ("Version: ", FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion).AppendLine();
                KLogger.EmptyLine();

                var rootCommand = new ClientCommand
                {
                    new FetchCommand(),
                    //new CleanCommand(),
                    //new MatchCommand(),
                    //new DumpCommand(),
                };

                var outputOption = new Option<DirectoryInfo>("--output",
                    getDefaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory()))
                {
                    Description = "Directory for the output files. Current directory will be used if none is specified"
                };
                rootCommand.AddGlobalOption(outputOption);

                rootCommand.Description = "Habbo Hotel Multi-Purpose Kit";

                return await rootCommand.InvokeAsync(args);
            }
            finally { KLogger.SetCursorVisibility(true); }
        }
    }
}