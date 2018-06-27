using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using HabKit.Utilities;
using HabKit.Commands.Foundation;

namespace HabKit
{
    public class Program
    {
        private static readonly Dictionary<string, Type> _commandTypes;

        private const ConsoleColor LOGO_COLOR = ConsoleColor.DarkRed;

        [KitArgument(KitPermissions.None, "output", 'o')]
        public static string OutputDirectory { get; private set; } = Environment.CurrentDirectory;

        static Program()
        {
            _commandTypes = new Dictionary<string, Type>();

            Type[] assemblyTypes = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type assemblyType in assemblyTypes)
            {
                var kitCommandAtt = assemblyType.GetCustomAttribute<KitCommandAttribute>();
                if (kitCommandAtt == null) continue;

                _commandTypes.Add(kitCommandAtt.Name, assemblyType);
            }
        }
        public Program(Queue<string> arguments)
        {
            this.PopulateMembers(arguments);
        }

        public static void Main(string[] args)
        {
            try
            {
                KitLogger.SetCursorVisibility(false);
                var arguments = new Queue<string>(args);

                var app = new Program(arguments);
                Directory.CreateDirectory(OutputDirectory);

                app.RunAsync(arguments).GetAwaiter().GetResult();
            }
            finally { KitLogger.SetCursorVisibility(true); }
        }

        private Task RunAsync(Queue<string> arguments)
        {
            KitLogger.WriteLine();
            "   ██╗  ██╗ █████╗ ██████╗ ██╗  ██╗██╗████████╗".WriteLine(LOGO_COLOR);
            "   ██║  ██║██╔══██╗██╔══██╗██║ ██╔╝██║╚══██╔══╝".WriteLine(LOGO_COLOR);
            "   ███████║███████║██████╔╝█████╔╝ ██║   ██║".WriteLine(LOGO_COLOR);
            "   ██╔══██║██╔══██║██╔══██╗██╔═██╗ ██║   ██║".WriteLine(LOGO_COLOR);
            "   ██║  ██║██║  ██║██████╔╝██║  ██╗██║   ██║".WriteLine(LOGO_COLOR);
            "   ╚═╝  ╚═╝╚═╝  ╚═╝╚═════╝ ╚═╝  ╚═╝╚═╝   ╚═╝".WriteLine(LOGO_COLOR);
            KitLogger.WriteLine();

            ("Version: ", FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion).WriteLine(null, ConsoleColor.White);
            ("Output Directory: ", OutputDirectory).WriteLine(null, ConsoleColor.White);
            KitLogger.WriteLine();

            Type commandType = _commandTypes[arguments.Dequeue()];
            var command = (KitCommand)Activator.CreateInstance(commandType, arguments);

            return command.ExecuteAsync();
        }
    }
}