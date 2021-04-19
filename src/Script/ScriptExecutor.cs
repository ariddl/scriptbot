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

            // Begin the tasks responsible for executing scripts.
            for (int i = 0; i < _config.Tasks; ++i)
                _tasks.Add(RunTask());
        }

        public bool AddScript(IScriptMeta meta, IExpression tree)
        {
            // We must compile the script as many times as we need to fill our
            // configured pool size, which effectively serves as the number of
            // concurrent instances the bot will support of this script type.
            for (int i = 0; i < _config.ScriptPoolSize; ++i)
            {
                // Try to compile the script. If compilation fails, we know
                // future compilations will also fail, and we should just exit.
                var compiled = CompileScript(tree, meta);
                if (compiled == null)
                    return false;
                
                // Add the compiled instance to the pool for this script type.
                if (!_scriptPool.ContainsKey(meta.Name))
                    _scriptPool.Add(meta.Name, new Queue<CompiledScript>());
                _scriptPool[meta.Name].Enqueue(compiled);
            }

            Console.WriteLine($"Compiled script: {meta.Name}");
            return true;
        }

        public void RemoveScript(string name) => Task.Run(async () =>
        {
            // Disable this script type by removing its pool of instances.
            await _semaphore.WaitAsync();
            if (_scriptPool.ContainsKey(name))
                _scriptPool.Remove(name);
            _semaphore.Release();
        });

        private CompiledScript CompileScript(IExpression tree, IScriptMeta meta)
        {
            // Each instance of a compiled script requires its own event instance.
            // This is because the compiled expression tree must keep the same
            // references to fields/etc to avoid recompilation, which is slow!
            // Each compiled script instance also has its own execution context,
            // which (as the name suggests) is used when the instance is being
            // ran. It holds the event instance and tracks the tasks that the
            // script ends up running, and awaits them for completion.
            var @event = EventDispatcher.CreateEventInstance(meta.EventTrigger);
            var ctx = new BuildContext
            {
                Discord = _client,
                Guild = _client.GetGuild(meta.Guild),
                Interface = _interface,
                ExecContext = new ScriptExecutionContext(@event)
            };

            // Attempt to generate the equivalent LINQ expression tree from our
            // custom IExpression-based tree structures. After building, check
            // for any errors and print them out if we there are any.
            var body = tree.Build(ctx);
            if (ctx.Errors.Count != 0)
            {
                Console.WriteLine($"CompileScript: {ctx.Errors.Count} errors!");
                foreach (string error in ctx.Errors)
                    Console.WriteLine($" - Msg: {error}");
                Console.WriteLine($"CompileScript: {meta.Name} compilation failed.");
                return null;
            }

            // Return the compiled script instance with the compiled lambda
            // and execution context specific to the instance.
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
            // Queue of events that cannot currently be ran due to empty pool.
            var pending = new Queue<(string, object[])>();

            // Main task loop
            while (!_stop)
            {
                // Get the next script to run with the corresponding parameters.
                (string script, object[] @params) item;
                while (_execQueue.TryDequeue(out item))
                {
                    // Attempt to get an instance of the script we need to run.
                    var (compiled, valid) = await TryGetCompiledScript(item.script);
                    if (compiled != null)
                    {
                        // Got a compiled instance of this script, run it.
                        await RunScript(compiled, item.@params);

                        // Return this compiled instance for future executions.
                        await ReturnCompiledScript(item.script, compiled);
                    }

                    // There are too many instances of this script currently running.
                    // Queue it up if the script is still enabled (indicated by the
                    // valid flag provided by TryGetCompiledScript).
                    else if (valid)
                        pending.Enqueue(item);
                }

                // Scripts that weren't ran were temporarily moved out of the main queue
                // in an attempt to find scripts that we could run. Queue the pending
                // scripts back into the main queue. Note: this of course assumes that we
                // don't care about the order in which scripts are executed! (TODO: Priority).
                while (pending.TryDequeue(out item))
                    _execQueue.Enqueue(item);

                // TODO: Better notification system.
                await Task.Delay(25);
            }
        }

        private async Task RunScript(CompiledScript script, object[] @params)
        {
            await ExecLog($"Running script: {script.Meta.Name}");

            // Initialize the script with the event parameters, call the compiled function,
            // function, and then await any asynchronous tasks that the script has started.
            script.ExecContext.Init(@params);
            script.CompiledFunc();
            await script.ExecContext.AwaitCompletion();

            await ExecLog($"Finished script: {script.Meta.Name}");
        }

        private async Task<(CompiledScript, bool)> TryGetCompiledScript(string name)
        {
            // Each script has a pool so that scripts of the same type can run concurrently.
            // If there are too many of the same script running at once, the pool may not
            // be able to return an instance, in which case future executions will be stalled
            // until the instances currently running start completing, and their instances
            // returned to the pool via ReturnCompiledScript.
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
            // Return the compiled script back to the queue corresponding to the script name.
            await _semaphore.WaitAsync();
            _scriptPool[name].Enqueue(script);
            _semaphore.Release();
        }

        // Execution logging for debugging (configurable by user).
        private async Task ExecLog(string msg)
            => await (_config.LogExecution ? AtomicConsole.WriteLine(msg) : Task.CompletedTask);

        public void Stop()
        {
            // Set the stop flag to stop the running tasks and await their exit.
            _stop = true;
            Task.WaitAll(_tasks.ToArray());
        }
    }
}
