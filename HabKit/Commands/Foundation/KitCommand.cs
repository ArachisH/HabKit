using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

using HabKit.Utilities;

namespace HabKit.Commands.Foundation
{
    public abstract class KitCommand
    {
        private readonly KitPermissions _permissions;
        private readonly Dictionary<KitPermissions, List<(MethodInfo, object[])>> _methods;

        public KitCommand(Queue<string> arguments)
        {
            _methods = new Dictionary<KitPermissions, List<(MethodInfo, object[])>>();

            this.PopulateMembers(arguments, out List<(MethodInfo, object[])> methods);
            foreach ((MethodInfo method, object[] values) item in methods)
            {
                var kitArgumentAtt = item.method.GetCustomAttribute<KitArgumentAttribute>();
                if (!_methods.TryGetValue(kitArgumentAtt.Permissions, out List<(MethodInfo, object[])> group))
                {
                    group = new List<(MethodInfo, object[])>();
                    _methods.Add(kitArgumentAtt.Permissions, group);
                }
                group.Add(item);
                _permissions |= kitArgumentAtt.Permissions;
            }
        }

        public async Task ExecuteAsync()
        {
            foreach (KitPermissions permission in _methods.Keys)
            {

            }

            if (_methods.TryGetValue(KitPermissions.None, out List<(MethodInfo, object[])> methods))
            {

            }

            //foreach ((MethodInfo method, object[] values) in _methods)
            //{
            //    object result = method.Invoke(this, values);
            //    if (result is Task resultTask)
            //    {
            //        await resultTask.ConfigureAwait(false);
            //    }
            //}
        }
        protected virtual async Task PrepareAsync(KitPermissions requestedPermission)
        {

        }
    }
}