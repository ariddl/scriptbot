using DiscordScriptBot.Expression;
using System;

namespace DiscordScriptBot.Script
{
    public static class Test
    {
        private static void BuildTest(bool a, bool b)
        {
            var builder = new ScriptBuilder("test script", $"a: {a} b: {b}");

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

            RunScript(builder.Build());
        }

        private static void BuildTest_NoPop()
        {
            var builder = new ScriptBuilder("no pop", "build w/o popping stack");

            for (int i = 0; i < 5; ++i)
            {
                builder.Condition(IEvaluable.Wrap(() => true));
                builder.ConditionPassed();
            }

            builder.Action(() => Console.WriteLine("Inner-most body"));
            RunScript(builder.Build());
        }

        private static void RunScript(Script script)
        {
            Console.WriteLine($"Running script '{script.Name}' ({script.Description})");
            script.Run();
            Console.WriteLine("=============");
        }

        public static void RunTests()
        {
            BuildTest(true, true);
            BuildTest(true, false);
            BuildTest(false, false);
            BuildTest_NoPop();
        }
    }
}
