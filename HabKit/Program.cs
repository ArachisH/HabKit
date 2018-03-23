using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

using HabKit.Utilities;
using HabKit.Commands.Foundation;

namespace HabKit
{
    public class Program
    {
        private static readonly Dictionary<string, Type> _commandTypes;

        [KitArgument("output", "o")]
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
        public static void Main(string[] args)
        {
            var arguments = new Queue<string>(args);

            var app = new Program();
            app.PopulateMembers(arguments);
            app.RunAsync(arguments).GetAwaiter().GetResult();
        }

        private async Task RunAsync(Queue<string> arguments)
        {
            Type commandType = _commandTypes[arguments.Dequeue()];
            var command = Activator.CreateInstance(commandType);

            command.PopulateMembers(arguments, out List<(MethodInfo, object[])> methods);
            foreach ((MethodInfo method, object[] values) in methods)
            {
                object result = method.Invoke(command, values);
                if (result is Task resultTask)
                {
                    await resultTask.ConfigureAwait(false);
                    Type genericType = result.GetType().GenericTypeArguments.FirstOrDefault();
                    if (genericType != null)
                    {
                        var resultProperty = typeof(Task<>).MakeGenericType(genericType).GetProperty("Result");
                        result = resultProperty.GetValue(resultTask);
                    }
                }
            }
        }
    }
}