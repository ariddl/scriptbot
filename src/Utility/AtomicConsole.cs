using System;
using System.Threading;

namespace DiscordScriptBot.Utility
{
    public static class AtomicConsole
    {
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public static void Write(string format, params string[] args)
            => Wrap(() => Console.Write(format, args));
        public static void WriteLine(string format, params string[] args)
            => Wrap(() => Console.WriteLine(format, args));

        private static async void Wrap(Action action)
        {
            await _semaphore.WaitAsync();
            action();
            _semaphore.Release();
        }
    }
}
