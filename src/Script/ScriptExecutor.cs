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
        public sealed class CompiledScript
        {
            public Func<bool> CompiledFunc { get; set; }
            public ScriptExecutionContext ExecContext { get; set; }
        }

        private Config _config;
        private DiscordSocketClient _client;
        private InterfaceManager _interface;
        private ScriptManager _scriptManager;
        private Dictionary<string, Queue<CompiledScript>> _scriptPool;
        private ConcurrentQueue<(string, IEventInstance)> _execQueue;
        private SemaphoreSlim _semaphore;
        private List<Task> _tasks;
        private bool _stop;

        public ScriptExecutor(Config config, DiscordSocketClient client, InterfaceManager @interface,
                              ScriptManager scriptManager)
        {
            _config = config;
            _client = client;
            _interface = @interface;
            _scriptManager = scriptManager;
            _scriptPool = new Dictionary<string, Queue<CompiledScript>>();
            _execQueue = new ConcurrentQueue<(string, IEventInstance)>();
            _semaphore = new SemaphoreSlim(1, 1);
            _tasks = new List<Task>();
            _stop = false;

            for (int i = 0; i < config.Tasks; ++i)
                _tasks.Add(RunTask());

            
        }

        public void Load()
        {
            foreach (IScriptMeta script in _scriptManager.GetScripts(true))
            {
                _scriptPool.Add(script.Name, new Queue<CompiledScript>());
                for (int i = 0; i < _config.ScriptPoolSize; ++i)
                    _scriptPool[script.Name].Enqueue(CompileScript(script.Tree, script));

                Console.WriteLine($"Compiled script: {script.Name}");
                if (!EventDispatcher.Instance.SubscribeScript(script.EventTrigger, script.Name))
                    Console.WriteLine($"Failed to SubscribeScript on dispatcher: {script.EventTrigger}");
            }
        }

        public CompiledScript CompileScript(IExpression tree, IScriptMeta meta)
        {
            var execCtx = new ScriptExecutionContext();
            execCtx.Init(EventDispatcher.Instance.GetEventInstance(meta.EventTrigger));
            var buildCtx = new BuildContext
            {
                Discord = _client,
                Guild = null, // TODO
                Interface = _interface,
                ExecContext = execCtx
            };

            var body = tree.Build(buildCtx);
            if (buildCtx.Errors.Count != 0)
            {
                Console.WriteLine($"CompileScript: {buildCtx.Errors.Count} errors!");
                foreach (string error in buildCtx.Errors)
                    Console.WriteLine($" - Msg: {error}");
                throw new Exception();
            }

            var compiled = new CompiledScript
            {
                CompiledFunc = Expression.Lambda<Func<bool>>(body).Compile(),
                ExecContext = execCtx
            };
            return compiled;
        }

        public void EnqueueExecute(string script, IEventInstance instance)
            => _execQueue.Enqueue((script, instance));

        private async Task RunTask()
        {
            var pending = new Queue<(string, IEventInstance)>();
            while (!_stop)
            {
                (string script, IEventInstance @event) item;
                while (_execQueue.TryDequeue(out item))
                {
                    var compiled = await TryGetCompiledScript(item.script);
                    if (compiled != null)
                    {
                        await RunScript(compiled, item.@event);
                        await ReturnCompiledScript(item.script, compiled);
                    }
                    else
                        pending.Enqueue(item);
                }
                while (pending.TryDequeue(out item))
                    _execQueue.Enqueue(item);

                // TODO: Better notification system.
                await Task.Delay(25);
            }
        }

        private async Task RunScript(CompiledScript script, IEventInstance @event)
        {
            await AtomicConsole.WriteLine("RunScript");
            script.ExecContext.Init(@event);
            script.CompiledFunc();
            await script.ExecContext.AwaitCompletion();
        }

        private async Task<CompiledScript> TryGetCompiledScript(string name)
        {
            CompiledScript result = null;
            await _semaphore.WaitAsync();
            if (_scriptPool.ContainsKey(name) && _scriptPool[name].Count > 0)
                result = _scriptPool[name].Dequeue();
            _semaphore.Release();
            return result;
        }

        private async Task ReturnCompiledScript(string name, CompiledScript script)
        {
            await _semaphore.WaitAsync();
            _scriptPool[name].Enqueue(script);
            _semaphore.Release();
        }

        public async Task Stop()
        {
            _stop = true;
            foreach (Task t in _tasks)
                await t;
        }
    }
}
