using System;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordScriptBot.Utility
{
    public static class AtomicConsole
    {
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public static async Task Write(string format, params string[] args)
            => await Wrap(() => Console.Write(format, args));
        public static async Task WriteLine(string format, params string[] args)
            => await Wrap(() => Console.WriteLine(format, args));

        private static async Task Wrap(Action action)
        {
            await _semaphore.WaitAsync();
            action();
            _semaphore.Release();
        }
    }
}
