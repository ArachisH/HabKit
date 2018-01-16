using System.Threading.Tasks;

using Sulakore.Habbo;

namespace HabKit.Commands
{
    public abstract class Command
    {
        protected virtual void Execute(ref HGame game) { }
        protected virtual Task<HGame> ExecuteAsync(HGame game) => null;

        public void Run(ref HGame game)
        {
            Task<HGame> executeTask = ExecuteAsync(game);
            if (executeTask != null)
            {
                game = (executeTask.Result ?? game);
            }
            else Execute(ref game);
        }
    }
}