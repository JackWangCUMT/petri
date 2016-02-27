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
    public class CLIDebugController : DebugController
    {
        public CLIDebugController(DebuggableHeadlessDocument doc) : base(doc, new CLIDebugClient(doc, doc))
        {
            Console.CancelKeyPress += InterruptHandler;
        }

        public int Debug() {
            while(true) {
                try {
                    Prompt();
                    string line = TryReadLine();
                    if(line != null) {
                        if(line == "q") {
                            return 0;
                        }
                    }
                }
                catch(Exception e) {
                    Console.WriteLine("An error occurred in the debugger loop: " + e.Message);
                }
            }

            return 0;
        }

        void Prompt() {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("(petri) ");
            Console.ResetColor();
        }
            
        void InterruptHandler(object sender, ConsoleCancelEventArgs args) {
            Console.WriteLine("^C");
            args.Cancel = true;
            _interruptFlag = true;
        }

        string TryReadLine() {
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
    }
}

