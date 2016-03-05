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
using Gtk;

namespace Petri.Editor.CLI.Debugger
{
    public class DebugClient : Petri.Editor.Debugger.DebugClient
    {
        public DebugClient(DebuggableHeadlessDocument doc) : base(doc,
                                                                  doc)
        {
        }

        DebuggableHeadlessDocument Document {
            get {
                return (DebuggableHeadlessDocument)_document;
            }
        }

        protected override void NotifyStateChanged()
        {
            Document.DebugController.NotifyStateChanged("State changed:");
            if(CurrentSessionState == SessionState.Started) {
                Document.DebugController.NotifyStateChanged("Connected");
                if(_debuggable.BaseDebugController.Client.PetriRunning) {
                    Document.DebugController.NotifyStateChanged("Petri net launched");
                    if(_debuggable.BaseDebugController.Client.Pause) {
                        Document.DebugController.NotifyStateChanged("Petri net paused");
                    }
                    else {
                        Document.DebugController.NotifyStateChanged("Petri net running");
                    }
                }
                else {
                    Document.DebugController.NotifyStateChanged("Petri net stopped");
                }
            }
            else {
                Document.DebugController.NotifyStateChanged("Disconnected");
            }
        }

        protected override void NotifyUnableToLoadDylib()
        {
            Console.Error.WriteLine(Configuration.GetLocalized("Unable to load the dynamic library! Try to compile it again."));
            Console.WriteLine("Do you want to try to recompile the petri net and run it? y/n: ");
            var answer = Console.ReadLine();
            if(answer == "y" || answer == "Y") {
                _document.Compile(true);
                Attach();
            }
        }

        protected override void NotifyUnrecoverableError(string message)
        {
            Document.DebugController.NotifyUnrecoverableError("Error: " + message);
        }

        protected override void NotifyStatusMessage(string message)
        {
            Console.WriteLine("Status message incoming: " + message);
        }

        protected override void NotifyEvaluated(string value)
        {
            Console.WriteLine("Result of the last evaluation: " + value);
        }

        protected override void NotifyServerError(string message)
        {
            Console.Error.WriteLine(Configuration.GetLocalized("An error occurred in the debugger:") + " " + message);
            if(message == "You are trying to run a Petri net that is different from the one which is compiled!") {
                Console.WriteLine("Do you want to try to recompile the petri net and run it afterwards? y/n: ");
                var answer = Console.ReadLine();
                if(answer == "y" || answer == "Y") {
                    ReloadPetri(true);
                }
            }
        }

        protected override void NotifyActiveStatesChanged()
        {

        }
    }
}

