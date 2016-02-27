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

namespace Petri.Editor
{
    public delegate void DebuggerActionDel();

    public class DebuggerAction
    {
        public DebuggerAction(DebuggerActionDel action,
                              string description,
                              string invocation,
                              params string[] aliases)
        {
            Description = description;
            Action = action;

            var list = new List<string>();
            list.Add(invocation);
            list.AddRange(aliases);

            Invocations = list;
        }

        public string Description {
            get;
            private set;
        }

        public void Execute()
        {
            Action();
        }

        public DebuggerActionDel Action {
            get;
            private set;
        }

        public IReadOnlyList<string> Invocations {
            get;
            private set;
        }
    }

    public class CLIDebugController : DebugController
    {
        public CLIDebugController(DebuggableHeadlessDocument doc) : base(doc,
                                                                         new CLIDebugClient(doc,
                                                                                            doc))
        {
            Console.CancelKeyPress += InterruptHandler;

            _actions.Add(new DebuggerAction(Exit, "Quit", "quit", "exit", "q"));
            _actions.Add(new DebuggerAction(PrintHelp, "Show help", "help", "h"));

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
            while(_alive) {
                try {
                    Prompt();
                    string line = TryReadLine();
                    if(line != null) {
                        try {
                            var action = _actionsMapping[line];
                            action.Execute();
                        }
                        catch(KeyNotFoundException) {
                            Console.WriteLine("Unrecognized command '{0}'.", line);
                        }
                    }
                }
                catch(Exception e) {
                    Console.WriteLine("An error occurred in the debugger loop: " + e.GetType() + " - " + e.Message);
                }
            }

            return _returnCode;
        }

        void Prompt()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("(petri) ");
            Console.ResetColor();
        }

        void Exit()
        {
            _returnCode = 0;
            _alive = false;
        }

        void PrintHelp()
        {
            Console.WriteLine("Debugger commands:");
            foreach(DebuggerAction a in _actions) {
                Console.WriteLine("  " + a.Invocations[0].PadRight(_maxHelpWidth) + " -- " + a.Description);
            }

            Console.WriteLine();
            Console.WriteLine("Current command aliases:");
            foreach(DebuggerAction a in _actions) {
                for(int i = 1; i < a.Invocations.Count; ++i) {
                    Console.WriteLine("  " + a.Invocations[i].PadRight(_maxHelpAliasWidth) + " -- (" + a.Invocations[0] + ") " + a.Description);
                }
            }
        }

        void InterruptHandler(object sender, ConsoleCancelEventArgs args)
        {
            Console.WriteLine("^C");
            args.Cancel = true;
            _interruptFlag = true;
        }

        string TryReadLine()
        {
            var buf = new System.Text.StringBuilder();
            while(!_interruptFlag) {
                if(Console.KeyAvailable) {
                    var key = Console.ReadKey(true);
                    if(key.Key == ConsoleKey.Enter) {
                        Console.Write(key.KeyChar);
                        return buf.ToString();
                    }
                    else if(key.Key == ConsoleKey.Backspace && buf.Length > 0) {
                        buf.Remove(buf.Length - 1, 1);
                        Console.Write("\b");
                    }
                    else if(key.KeyChar != 0) {
                        buf.Append(key.KeyChar);
                        Console.Write(key.KeyChar);
                    }
                }
                else {
                    System.Threading.Thread.Sleep(50);
                }
            }

            _interruptFlag = false;
            return null;
        }

        volatile bool _interruptFlag = false;
        int _returnCode = 0;
        bool _alive = true;

        List<DebuggerAction> _actions = new List<DebuggerAction>();
        Dictionary<string, DebuggerAction> _actionsMapping = new Dictionary<string, DebuggerAction>();
        int _maxHelpWidth = 0, _maxHelpAliasWidth = 0;
    }
}

