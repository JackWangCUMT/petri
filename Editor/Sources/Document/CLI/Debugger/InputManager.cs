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
    public class InputManager
    {
        public InputManager()
        {
            Console.CancelKeyPress += InterruptHandler;

            _commandHistory.AddLast("");
            _historyPtr = _commandHistory.Last;
        }

        public string TryReadCommand()
        {
            Prompt();

            while(!_interruptFlag) {
                if(Console.KeyAvailable) {
                    var key = Console.ReadKey(true);
                    if(key.Key == ConsoleKey.UpArrow) {
                        Console.Write(new string('\b', Console.CursorLeft - PromptString.Length));

                        _historyPtr.Value = _lastHistoryValue;
                        if(_historyPtr.Previous != null) {
                            _historyPtr = _historyPtr.Previous;
                        }
                        else {
                            Console.Write('\a');
                        }

                        _lastHistoryValue = _historyPtr.Value;
                        Console.Write(_historyPtr.Value);
                    }
                    else if(key.Key == ConsoleKey.DownArrow) {
                        Console.Write(new string('\b', Console.CursorLeft - PromptString.Length));

                        _historyPtr.Value = _lastHistoryValue;
                        if(_historyPtr.Next != null) {
                            _historyPtr = _historyPtr.Next;
                        }
                        else {
                            Console.Write('\a');
                        }

                        _lastHistoryValue = _historyPtr.Value;
                        Console.Write(_historyPtr.Value);
                    }
                    else {
                        if(key.Key == ConsoleKey.Enter) {
                            Console.Write(key.KeyChar);

                            if(_historyPtr.Value.Length == 0) {
                                return null;
                            }
                            else {
                                var command = _historyPtr.Value;
                                if(_historyPtr != _commandHistory.Last) {
                                    _historyPtr.Value = _lastHistoryValue;
                                    _commandHistory.Last.Value = command;
                                }
                                _commandHistory.AddLast("");
                                _historyPtr = _commandHistory.Last;
                                _lastHistoryValue = "";

                                return command;
                            }
                        }
                        else if(key.Key == ConsoleKey.Backspace && _historyPtr.Value.Length > 0) {
                            _historyPtr.Value = _historyPtr.Value.Remove(_historyPtr.Value.Length - 1,
                                                                         1);
                            Console.Write("\b");
                        }
                        else if(key.KeyChar != 0) {
                            _historyPtr.Value = _historyPtr.Value + key.KeyChar;
                            Console.Write(key.KeyChar);
                        }
                    }
                }
                else {
                    System.Threading.Thread.Sleep(50);
                }
            }

            _commandHistory.Last.Value = "";
            _historyPtr = _commandHistory.Last;
            _lastHistoryValue = "";

            _interruptFlag = false;
            return null;
        }

        void Prompt()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(PromptString);
            Console.ResetColor();
        }

        static string PromptString {
            get { return "(petri) "; }
        }

        void InterruptHandler(object sender, ConsoleCancelEventArgs args)
        {
            Console.WriteLine("^C");
            args.Cancel = true;
            _interruptFlag = true;
        }

        LinkedList<string> _commandHistory = new LinkedList<string>();
        LinkedListNode<string> _historyPtr;
        string _lastHistoryValue = "";
        volatile bool _interruptFlag = false;
    }
}

