using DiscordScriptBot.Expression;
using System;

namespace DiscordScriptBot.Script
{
    public static class Test
    {
        private static void BuildTest(bool a, bool b)
        {
            var builder = new ScriptBuilder("test script", "this is a test script");

            // main script body
            builder.Action(() => Console.WriteLine("script body"));
            builder.Action(() => Console.WriteLine("script body 2"));
            builder.Condition(IEvaluable.Wrap(() => a));

            // inside outer condition (if pass)
            builder.ConditionPassed();
            builder.Condition(IEvaluable.Wrap(() => b));

            // inside inner condition (if pass)
            builder.ConditionPassed();
            builder.Action(() => Console.WriteLine("Inner condition passed body"));
            builder.Pop(); // leave inner condition passed

            // inside inner condition (if fail)
            builder.ConditionFailed();
            builder.Action(() => Console.WriteLine("Inner condition failed"));
            builder.Pop(); // leave inner condition failed
            builder.Pop(); // leave inner condition

            // back inside outer condition (if pass)
            builder.Action(() => Console.WriteLine("Outer condition passed body"));
            builder.Pop(); // leave outer condition passed

            // inside outer condition (if fail)
            builder.ConditionFailed();
            builder.Action(() => Console.WriteLine("Outer condition failed body"));
            builder.Pop(); // leave outer condition failed
            builder.Pop(); // leave outer condition

            // back inside main script body
            builder.Action(() => Console.WriteLine("Everything done"));

            var script = builder.Build();
            script.Run();

            Console.WriteLine("=============");
        }

        public static void RunTests()
        {
            BuildTest(true, true);
            BuildTest(true, false);
            BuildTest(false, false);
        }
    }
}
