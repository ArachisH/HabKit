using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

using HabKit.Utilities;

namespace HabKit.Commands.Foundation
{
    public abstract class KitCommand
    {
        private readonly SortedDictionary<KitAction, List<(MethodInfo, object[])>> _methods;

        public KitCommand(Queue<string> arguments)
        {
            _methods = new SortedDictionary<KitAction, List<(MethodInfo, object[])>>();

            this.PopulateMembers(arguments, out List<(MethodInfo, object[])> methods);
            foreach ((MethodInfo method, object[] values) item in methods)
            {
                var kitArgumentAtt = item.method.GetCustomAttribute<KitArgumentAttribute>();
                if (!_methods.TryGetValue(kitArgumentAtt.Action, out List<(MethodInfo, object[])> group))
                {
                    group = new List<(MethodInfo, object[])>();
                    _methods.Add(kitArgumentAtt.Action, group);
                }

                group.Add(item);
            }
        }

        public virtual async Task<bool> ExecuteAsync()
        {
            bool modified = false;
            foreach (KitAction action in _methods.Keys)
            {
                WriteActionTitle(action);
                foreach ((MethodInfo method, object[] values) in _methods[action])
                {
                    try
                    {
                        object result = method.Invoke(this, values);
                        if (result is Task resultTask)
                        {
                            await resultTask.ConfigureAwait(false);
                        }
                        else if (result is bool resultBoolean && action == KitAction.Modify)
                        {
                            modified = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine();
                        (ex.InnerException ?? ex).WriteLine(ConsoleColor.Red);
                        Console.WriteLine();
                    }
                }
            }
            return modified;
        }

        private void WriteActionTitle(KitAction action)
        {
            switch (action)
            {
                case KitAction.Modify:
                ("=====[ ", "Modifying", " ]=====").WriteLine(null, ConsoleColor.Cyan, null);
                break;
                case KitAction.Inspect:
                ("=====[ ", "Inspecting", " ]=====").WriteLine(null, ConsoleColor.Cyan, null);
                break;
            }
        }
    }
}