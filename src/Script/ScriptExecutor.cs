using Discord.WebSocket;
using DiscordScriptBot.Builder;
using DiscordScriptBot.Event;
using DiscordScriptBot.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using static DiscordScriptBot.Script.ScriptManager;

namespace DiscordScriptBot.Script
{
    public class ScriptExecutor
    {
        private class CompiledScript
        {
            public IScriptMeta Meta { get; set; }
            public Func<bool> CompiledFunc { get; set; }
            public ScriptExecutionContext ExecContext { get; set; }
        }

        private Config _config;
        private DiscordSocketClient _client;
        private ScriptInterface _interface;
        private Dictionary<string, Queue<CompiledScript>> _scriptPool;
        private ConcurrentQueue<(string, object[])> _execQueue;
        private SemaphoreSlim _semaphore;
        private List<Task> _tasks;
        private bool _stop;

        public ScriptExecutor(Config config, DiscordSocketClient client, ScriptInterface @interface)
        {
            _config = config;
            _client = client;
            _interface = @interface;
            _scriptPool = new Dictionary<string, Queue<CompiledScript>>();
            _execQueue = new ConcurrentQueue<(string, object[])>();
            _semaphore = new SemaphoreSlim(1, 1);
            _tasks = new List<Task>();
            _stop = false;

            for (int i = 0; i < _config.Tasks; ++i)
                _tasks.Add(RunTask());
        }

        public bool AddScript(IScriptMeta meta, IExpression tree)
        {
            for (int i = 0; i < _config.ScriptPoolSize; ++i)
            {
                var compiled = CompileScript(tree, meta);
                if (compiled == null)
                    return false;

                if (!_scriptPool.ContainsKey(meta.Name))
                    _scriptPool.Add(meta.Name, new Queue<CompiledScript>());
                _scriptPool[meta.Name].Enqueue(compiled);
            }
            Console.WriteLine($"Compiled script: {meta.Name}");
            return true;
        }

        public void RemoveScript(string name) => Task.Run(async () =>
        {
            await _semaphore.WaitAsync();
            if (_scriptPool.ContainsKey(name))
                _scriptPool.Remove(name);
            _semaphore.Release();
        });

        private CompiledScript CompileScript(IExpression tree, IScriptMeta meta)
        {
            var @event = EventDispatcher.CreateEventInstance(meta.EventTrigger);
            var ctx = new BuildContext
            {
                Discord = _client,
                Guild = _client.GetGuild(meta.Guild),
                Interface = _interface,
                ExecContext = new ScriptExecutionContext(@event)
            };

            var body = tree.Build(ctx);
            if (ctx.Errors.Count != 0)
            {
                Console.WriteLine($"CompileScript: {ctx.Errors.Count} errors!");
                foreach (string error in ctx.Errors)
                    Console.WriteLine($" - Msg: {error}");
                Console.WriteLine($"CompileScript: {meta.Name} compilation failed.");
                return null;
            }

            return new CompiledScript
            {
                Meta = meta,
                CompiledFunc = Expression.Lambda<Func<bool>>(body).Compile(),
                ExecContext = ctx.ExecContext
            };
        }

        public void EnqueueExecute(string script, params object[] @params)
            => _execQueue.Enqueue((script, @params));

        private async Task RunTask()
        {
            var pending = new Queue<(string, object[])>();
            while (!_stop)
            {
                (string script, object[] @params) item;
                while (_execQueue.TryDequeue(out item))
                {
                    var (compiled, valid) = await TryGetCompiledScript(item.script);
                    if (compiled != null)
                    {
                        await RunScript(compiled, item.@params);
                        await ReturnCompiledScript(item.script, compiled);
                    }
                    else if (valid)
                        pending.Enqueue(item);
                }
                while (pending.TryDequeue(out item))
                    _execQueue.Enqueue(item);

                // TODO: Better notification system.
                await Task.Delay(25);
            }
        }

        private async Task RunScript(CompiledScript script, object[] @params)
        {
            await ExecLog($"Running script: {script.Meta.Name}");
            script.ExecContext.Init(@params);
            script.CompiledFunc();
            await script.ExecContext.AwaitCompletion();
            await ExecLog($"Finished script: {script.Meta.Name}");
        }

        private async Task<(CompiledScript, bool)> TryGetCompiledScript(string name)
        {
            CompiledScript result = null;
            bool valid;
            await _semaphore.WaitAsync();
            if ((valid = _scriptPool.ContainsKey(name)) && _scriptPool[name].Count > 0)
                result = _scriptPool[name].Dequeue();
            _semaphore.Release();
            return (result, valid);
        }

        private async Task ReturnCompiledScript(string name, CompiledScript script)
        {
            await _semaphore.WaitAsync();
            _scriptPool[name].Enqueue(script);
            _semaphore.Release();
        }

        private async Task ExecLog(string msg)
            => await (_config.LogExecution ? AtomicConsole.WriteLine(msg) : Task.CompletedTask);

        public void Stop()
        {
            _stop = true;
            Task.WaitAll(_tasks.ToArray());
        }
    }
}
