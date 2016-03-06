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

namespace Petri.Editor.CLI.Debugger
{
    public delegate void DebuggerActionDel(string args);

    public class DebuggerAction
    {
        public DebuggerAction(DebuggerActionDel action,
                              string description,
                              string help,
                              string syntax,
                              string invocation,
                              params string[] aliases)
        {
            Description = description;
            Action = action;

            var list = new List<string>();
            list.Add(invocation);
            list.AddRange(aliases);

            Invocations = list;

            Help = help;
            Syntax = syntax;
        }

        public string Description {
            get;
            private set;
        }

        public void Execute(string args)
        {
            Action(args.Trim());
        }

        public DebuggerActionDel Action {
            get;
            private set;
        }

        public IReadOnlyList<string> Invocations {
            get;
            private set;
        }

        public string Help {
            get;
            private set;
        }

        public string Syntax {
            get;
            private set;
        }

        public void PrintHelp(string invocation)
        {
            Console.WriteLine(Help);
            Console.WriteLine();
            Console.Write("Syntax: ");
            Console.WriteLine(Syntax);

            if(invocation != Invocations[0]) {
                Console.WriteLine("\n'{0}' is an alias for '{1}'.", invocation, Invocations[0]);
            }
        }
    }
}

