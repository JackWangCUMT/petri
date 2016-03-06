/*
 * Copyright (c) 2016 Rémi Saurel
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Threading;

namespace Petri.Editor.CLI.Debugger
{
    public class DebugController : Petri.Editor.Debugger.DebugController
    {
        public DebugController(DebuggableHeadlessDocument doc) : base(doc,
                                                                      new DebugClient(doc))
        {
            _inputManager = new InputManager();

            _actions.Add(new DebuggerAction(Exit,
                                            "Quit",
                                            "Quit out of the petri net debugger.",
                                            "quit",
                                            "quit",
                                            "exit",
                                            "q"));
            _actions.Add(new DebuggerAction(PrintHelp,
                                            "Show help",
                                            "Show a list of all debugger commands, or give details about specific commands.",
                                            "help [<cmd-name>]",
                                            "help",
                                            "h"));
            _actions.Add(new DebuggerAction(Run,
                                            "Runs the petri net",
                                            "Runs the petri net.",
                                            "run",
                                            "run",
                                            "r"));
            _actions.Add(new DebuggerAction(Attach,
                                            "Attach to the debug server",
                                            "Attaches this client to an active debug server.",
                                            "attach",
                                            "attach",
                                            "at"));
            _actions.Add(new DebuggerAction(Detach,
                                            "Detach from the debug server",
                                            "Detaches this client from its debug server.",
                                            "detach",
                                            "detach",
                                            "de"));

            foreach(var action in _actions) {
                _maxHelpWidth = Math.Max(_maxHelpWidth, action.Invocations[0].Length);
                _actionsMapping[action.Invocations[0]] = action;

                for(int i = 1; i < action.Invocations.Count; ++i) {
                    _maxHelpAliasWidth = Math.Max(_maxHelpAliasWidth, action.Invocations[i].Length);
                    _actionsMapping[action.Invocations[i]] = action;
                }
            }
        }

        public int Debug()
        {
            if(Document != null) {
                Console.WriteLine("Current petri net set to '{0}'", Document.Settings.Name);
                if(Document.Settings.RunInEditor) {
                    Console.WriteLine("The petri net is set to run directly in the debugger.");
                }
                else {
                    Console.WriteLine("The petri net is set to run in an external application.");
                }
            }

            while(_alive) {
                try {
                    string line = _inputManager.TryReadCommand();
                    if(line != null) {
                        ParseCommand(line);
                    }
                }
                catch(Exception e) {
                    Console.WriteLine("An error occurred in the debugger loop: " + e.GetType() + " - " + e.Message);
                }
            }

            return _returnCode;
        }

        public void NotifyStateChanged(string newState) {
            Console.WriteLine(newState);
        }

        public void NotifyUnrecoverableError(string error) {
            Console.WriteLine(error);
        }

        void ParseCommand(string line)
        {
            string command, args;
            int index = line.IndexOfAny(new char[]{ ' ', '\t' });
            if(index == -1) {
                command = line;
                args = "";
            }
            else {
                command = line.Substring(0, index);
                args = line.Substring(index + 1);
            }
            try {
                var action = _actionsMapping[command];
                action.Execute(args);
            }
            catch(KeyNotFoundException) {
                Console.WriteLine("error: Unrecognized command '{0}'.", command);
            }
        }

        string GetCommandName(string line)
        {
            string command;
            int index = line.IndexOfAny(new char[]{ ' ', '\t' });
            if(index == -1) {
                command = line;
            }
            else {
                command = line.Substring(0, index);
            }

            return command;
        }

        DebuggerAction GetCommandFromName(string line)
        {
            string command = GetCommandName(line);

            try {
                return _actionsMapping[command];
            }
            catch(KeyNotFoundException) {
                return null;
            }
        }

        void Exit(string args)
        {
            Client.Detach();

            _returnCode = 0;
            _alive = false;
        }

        void PrintHelp(string args)
        {
            if(args != "") {
                var cmd = GetCommandName(args);
                var action = GetCommandFromName(cmd);

                if(action == null) {
                    Console.WriteLine("error: '{0}' is not a known command.", cmd);
                    Console.WriteLine("Try 'help' to see a current list of commands.");
                }
                else {
                    action.PrintHelp(cmd);
                }
            }
            else {
                Console.WriteLine("Debugger commands:");
                foreach(DebuggerAction a in _actions) {
                    Console.WriteLine("  " + a.Invocations[0].PadRight(_maxHelpWidth) + " -- " + a.Description);
                }

                Console.WriteLine();
                Console.WriteLine("Command aliases:");
                foreach(DebuggerAction a in _actions) {
                    for(int i = 1; i < a.Invocations.Count; ++i) {
                        Console.WriteLine("  " + a.Invocations[i].PadRight(_maxHelpAliasWidth) + " -- (" + a.Invocations[0] + ") " + a.Description);
                    }
                }
            }
        }

        void Attach(string args)
        {
            Client.Attach();
            WaitFor(o => {
                return Client.CurrentSessionState != DebugClient.SessionState.Starting;
            });
        }

        void Detach(string args)
        {
            Client.Detach();
            WaitFor(o => {
                return Client.CurrentSessionState == DebugClient.SessionState.Stopped;
            });
        }
            
        void Run(string args)
        {
            if(Client.CurrentSessionState != DebugClient.SessionState.Started) {
                NotifyUnrecoverableError("The debugger must be attached to the debug server to be able to run the petri net.\nTry 'attach'.");
            }
            else {
                Client.StartPetri();
                WaitFor(o => {
                    return Client.CurrentPetriState == DebugClient.PetriState.Stopped || Client.CurrentPetriState == DebugClient.PetriState.Paused;
                });
            }
        }

        static void WaitFor(Predicate<object> pred) {
            while(!pred.Invoke(null)) {
                Thread.Sleep(50);
            }
        }

        int _returnCode = 0;
        volatile bool _alive = true;

        List<DebuggerAction> _actions = new List<DebuggerAction>();
        Dictionary<string, DebuggerAction> _actionsMapping = new Dictionary<string, DebuggerAction>();
        int _maxHelpWidth = 0, _maxHelpAliasWidth = 0;

        InputManager _inputManager;
    }
}

