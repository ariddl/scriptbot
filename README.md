# scriptbot
Discord Script Bot - An [RCOS](https://rcos.io/) project.

A scriptable Discord bot fully controlled by you!

# Class overview

ExpressionBuilder
 - Builds whole scripts.
BinaryExpressionBuilder
 - Builds logical expressions (Expression.AndAlso, etc).

IEvent - defines events that can execute scripts
 - Defines the parameters provided to scripts
 - Each script must be compiled with an Event instance
 - Events are initialized with the latest event data (user, text, etc).

EventDispatcher
 - Subscribes the events from Discord.Net
 - Keeps track of actively subscribed scripts
 - Dispatches events to all subscribed scripts

ScriptExecutor
 - Runs and keeps track of a configurable number of concurrent scripts
 - Keeps pool of compiled script instances for performance

Script
 - Stores compiled expression tree
 - Serves as execution context; has a list of pending Tasks that each need to be awaited
 - When scripts call functions in their expressions, we should likely wait for them to finish before continuing script execution
 - We  wrap expression function calls with Script to add their tasks to a list to be awaited
