# ScriptBot
### A Discord bot fully controlled by you!

An [RCOS](https://rcos.io/) project.

License: MIT

Check out the [Wiki](https://github.com/ariddl/scriptbot/wiki) to get started!

## Usage

### Script management
* List current scripts: `/script describe`
* Show script information: `/script describe [name]`
* Enable script: `/script enable [name]`
* Disable script: `/script disable [name]`

### Script building

* Start building a script: `/scriptbuild new [name] [event] [optional description]`
* Cancel or start over: `/scriptbuild cancel`
* Finish building: `/scriptbuild done [enable]`
  * If enable is specified, the script will be enabled right away.

#### API listing

* To view the currently supported events: `/interface describe event [name]`
  * If no event name is specified, a list of events will be displayed.
* To view the currently supported objects/wrappers: `/interface describe object [name]`
  * If no object name is specified, a list of objects will be displayed.

#### Branching and Logical Expressions (if/and/or/elif)

* If: `/if (class) (function) [parameters] [then]`
  * If `then` is specified at the end of any logical expression, you will end that condition and enter the "action block", where the bot will expect new `/action` commands, or another `/if` to be started.
* Or: `/or (class) (function) [parameters] [then]`
* And: `/and (class) (function) [parameters] [then]`
* Else: `/else`
  * You may only use this while inside of an if statement's main action block. This will enter the "if condition false" action block.
* Then: `/then`
  * Ends the current logical expression. Note that it may also be used at the end of `/if`, `/and`, `/or`, etc.
* End: `/end`
  * Ends the current if block and returns to the outer-scope action block. The order should go: `/if` -> `/then` -> `/end` for a complete if statement.

## Example Script

The following script will delete all messages containing "apple" and/or "orange", disregarding case sensitivity. For a more detailed breakdown of this script, head over to the [Examples wiki page.](https://github.com/ariddl/scriptbot/wiki/Examples)

```
/scriptbuild new deleteApplesOranges messageReceived [optional description]
/action message text.lower
/if message text.contains "apple"
/or message text.contains "orange" then
/action message delete
/action textChannel[moderationlog] sendText "Message deleted."
/scriptbuild done enable
```
