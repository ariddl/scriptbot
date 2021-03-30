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

        public ScriptExecutionContext() => _taskQueue = new Queue<Func<Task>>();

        public void Init(IEventInstance @event) => _event = @event;

        public object GetParam(string name) => _event.GetParam(name);

        public void EnqueueTask(Func<Task> t) => _taskQueue.Enqueue(t);

        public async Task AwaitCompletion()
        {
            while (_taskQueue.Count > 0)
                await _taskQueue.Dequeue()();
        }
    }
}
