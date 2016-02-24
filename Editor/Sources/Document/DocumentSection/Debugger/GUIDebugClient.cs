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

namespace Petri.Editor
{
    public class GUIDebugClient : DebugClient
    {
        public GUIDebugClient(Document doc) : base(doc)
        {
        }

        Document Document {
            get {
                return (Document)_document;
            }
        }

        protected override void NotifyStateChanged() {
            Document.Window.DebugGui.UpdateToolbar();
        }

        protected override void NotifyUnableToLoadDylib() {
            Application.RunOnUIThread(() => {
                MessageDialog d = new MessageDialog(Document.Window,
                                                            DialogFlags.Modal,
                                                            MessageType.Question,
                                                            ButtonsType.None,
                                                            Application.SafeMarkupFromString(Configuration.GetLocalized("Unable to load the dynamic library! Try to compile it again.")));
                d.AddButton(Configuration.GetLocalized("Cancel"), ResponseType.Cancel);
                d.AddButton(Configuration.GetLocalized("Fix"), ResponseType.Apply);

                if((ResponseType)d.Run() == ResponseType.Apply) {
                    d.Destroy();
                    _document.Compile(true);
                    Attach();
                }
                else {
                    d.Destroy();
                }
            });
        }

        protected override void NotifyUnrecoverableError(string message)
        {
            Application.RunOnUIThread(() => {
                MessageDialog d = new MessageDialog(Document.Window,
                                                    DialogFlags.Modal,
                                                    MessageType.Question,
                                                    ButtonsType.None,
                                                    Application.SafeMarkupFromString(message));
                d.AddButton(Configuration.GetLocalized("Cancel"), ResponseType.Cancel);
                d.Run();
                d.Destroy();
            });
        }

        protected override void NotifyStatusMessage(string message) {
            Application.RunOnUIThread(() => {
                Document.Window.DebugGui.Status = message;
            });
        }

        protected override void NotifyEvaluated(string value) {
            Application.RunOnUIThread(() => {
                Document.Window.DebugGui.OnEvaluate(value);
            });
        }

        protected override void NotifyServerError(string message) {
            Application.RunOnUIThread(() => {
                MessageDialog d = new MessageDialog(Document.Window,
                                                                DialogFlags.Modal,
                                                                MessageType.Question,
                                                                ButtonsType.None,
                                                                Application.SafeMarkupFromString(Configuration.GetLocalized("An error occurred in the debugger:") + " " + message));
                d.AddButton(Configuration.GetLocalized("Cancel"), ResponseType.Cancel);
                if(message == "You are trying to run a Petri net that is different from the one which is compiled!") {
                    d.AddButton(Configuration.GetLocalized("Fix and run"),
                                            ResponseType.Apply);
                }
                if((ResponseType)d.Run() == ResponseType.Apply) {
                    ReloadPetri(true);
                }
                d.Destroy();
            });
        }

        protected override void NotifyActiveStatesChanged() {

        }
    }
}

