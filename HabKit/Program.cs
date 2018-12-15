using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using HabKit.Commands;
using HabKit.Utilities;
using HabKit.Commands.Foundation;

namespace HabKit
{
    public class Program
    {
        private static readonly Type _defaultCommandType;
        private static readonly Dictionary<string, Type> _commandTypes;

        private const ConsoleColor LOGO_COLOR = ConsoleColor.DarkRed;

        [KitArgument(KitAction.None, "output", 'o')]
        public static string OutputDirectory { get; private set; } = Environment.CurrentDirectory;

        static Program()
        {
            _defaultCommandType = typeof(ClientCommand);
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
                KLogger.SetCursorVisibility(false);
                var arguments = new Queue<string>(args);

                var app = new Program(arguments);
                Directory.CreateDirectory(OutputDirectory);

                app.RunAsync(arguments).GetAwaiter().GetResult();
            }
            finally { KLogger.SetCursorVisibility(true); }
        }

        private Task RunAsync(Queue<string> arguments)
        {
            if (arguments.Count == 0) return Task.CompletedTask;

            KLogger.EmptyLine();
            "   ██╗  ██╗ █████╗ ██████╗ ██╗  ██╗██╗████████╗".AppendLine(LOGO_COLOR);
            "   ██║  ██║██╔══██╗██╔══██╗██║ ██╔╝██║╚══██╔══╝".AppendLine(LOGO_COLOR);
            "   ███████║███████║██████╔╝█████╔╝ ██║   ██║".AppendLine(LOGO_COLOR);
            "   ██╔══██║██╔══██║██╔══██╗██╔═██╗ ██║   ██║".AppendLine(LOGO_COLOR);
            "   ██║  ██║██║  ██║██████╔╝██║  ██╗██║   ██║".AppendLine(LOGO_COLOR);
            "   ╚═╝  ╚═╝╚═╝  ╚═╝╚═════╝ ╚═╝  ╚═╝╚═╝   ╚═╝".AppendLine(LOGO_COLOR);
            KLogger.EmptyLine();

            ("Version: ", FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion).AppendLine();
            ("Output Directory: ", OutputDirectory).AppendLine();
            KLogger.EmptyLine();

            string argument = arguments.Peek();
            if (_commandTypes.TryGetValue(argument, out Type commandType))
            {
                arguments.Dequeue();
            }
            else commandType = _defaultCommandType;

            var command = (KitCommand)Activator.CreateInstance(commandType, arguments);
            return command.ExecuteAsync();
        }
    }
}