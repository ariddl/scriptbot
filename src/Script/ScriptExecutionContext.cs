using DiscordScriptBot.Event;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordScriptBot.Script
{
    public class ScriptExecutionContext
    {
        private IEventInstance _event;
        private Queue<Func<Task>> _taskQueue;

        public ScriptExecutionContext(IEventInstance @event)
        {
            _event = @event;
            _taskQueue = new Queue<Func<Task>>();
        }

        public void Init(params object[] @params) => _event.Init(@params);
        public object GetParam(string name) => _event.GetParam(name);

        public void EnqueueTask(Func<Task> t) => _taskQueue.Enqueue(t);

        public async Task AwaitCompletion()
        {
            // Scripts may or may not call actions that return Tasks. These
            // actions are likely Discord.net calls (like Kick), which require
            // async-awaits. We ensure these are ran in-order by queuing them
            // up here by wrapping their calls with lambdas to call EnqueueTask
            // instead of calling them directly (see CallExpression).
            while (_taskQueue.Count > 0)
                await _taskQueue.Dequeue()();
        }
    }
}
