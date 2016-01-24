/*
 * Copyright (c) 2015 Rémi Saurel
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
using System.Diagnostics;
using Gtk;
using System.Text;

namespace Petri.Editor
{
    /// <summary>
    /// This class is in charge of invoking the a document's compiler program and retrieving stdout and stderr.
    /// </summary>
    public class Compiler
    {
        public Compiler(HeadlessDocument doc)
        {
            _document = doc;
        }

        /// <summary>
        /// Compiles the source. The source file's last modification is set to the current date.
        /// </summary>
        /// <returns>stdout and stderr concatenated in that order.</returns>
        /// <param name="source">The path of the source file to compile.</param>
        /// <param name="lib">The library file to generate.</param>
        public string CompileSource(string source, string lib)
        {
            string cd = System.IO.Directory.GetCurrentDirectory();
            System.IO.Directory.SetCurrentDirectory(System.IO.Directory.GetParent(_document.Path).FullName);
            if(!System.IO.File.Exists(source)) {
                return Configuration.GetLocalized("Error: the file \"{0}\" doesn't exist. Please generate the source code before compiling.",
                                                  source);
            }

            System.IO.File.SetLastWriteTime(source, DateTime.Now);

            Process p = new Process();

            // This snippet removes the debugging environment variable from the child process, as it causes an error when the app is run into a debugger.
            string monoEnv = p.StartInfo.EnvironmentVariables["MONO_ENV_OPTIONS"];
            if(monoEnv != null) {
                monoEnv = System.Text.RegularExpressions.Regex.Replace(monoEnv, "--debugger-agent=transport=dt_socket,address=127.0.0.1:[0-9]{3,5}", "");
                p.StartInfo.EnvironmentVariables["MONO_ENV_OPTIONS"] = monoEnv;
            }

            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.FileName = _document.Settings.Compiler;
            string s = _document.Settings.CompilerArguments(source, lib);
            if(s.Length < 5000) {
                p.StartInfo.Arguments = s;
            }
            else {
                return Configuration.GetLocalized("Error: the compiler invocation is too long ({0}) characters. Try to remove some recursive inclusion paths.",
                                                  s.Length);
            }

            StringBuilder outputBuilder = new StringBuilder();

            p.OutputDataReceived += (object sender, DataReceivedEventArgs e) => {
                if(!String.IsNullOrEmpty(e.Data)) {
                    outputBuilder.Append(e.Data);
                }
            };

            p.Start();

            p.BeginOutputReadLine();

            string err = p.StandardError.ReadToEnd();

            p.WaitForExit();

            System.IO.Directory.SetCurrentDirectory(cd);

            outputBuilder.Append(err);

            return outputBuilder.ToString();
        }

        HeadlessDocument _document;
    }

    public class CompilationErrorPresenter
    {
        public CompilationErrorPresenter(Document doc, string error)
        {
            _document = doc;

            _window = new Window(WindowType.Toplevel);
            _window.Title = Configuration.GetLocalized("{0} compilation output", doc.Window.Title);

            _window.DefaultWidth = 600;
            _window.DefaultHeight = 400;

            _window.SetPosition(WindowPosition.Center);
            int x, y;
            _window.GetPosition(out x, out y);
            _window.Move(x, 2 * y / 3);
            _window.BorderWidth = 15;
            _window.AllowShrink = true;

            ScrolledWindow scrolledWindow = new ScrolledWindow();
            scrolledWindow.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);

            Viewport viewport = new Viewport();

            var tagTable = new TextTagTable();
            var tag = new TextTag("mytag");
            tagTable.Add(tag);
            var buf = new TextBuffer(tagTable);
            tag.Family = "Monospace";


            TextView view = new TextView(buf);
            view.Editable = false;
            view.Buffer.Text = error;
            buf.ApplyTag("mytag", buf.StartIter, buf.EndIter);

            viewport.Add(view);

            view.SizeRequested += (o, args) => {
                viewport.WidthRequest = viewport.Child.Requisition.Width;
                viewport.HeightRequest = viewport.Child.Requisition.Height;
            };

            scrolledWindow.Add(viewport);

            var hbox = new HBox(false, 0);
            hbox.PackStart(scrolledWindow, true, true, 0);
            _window.Add(hbox);

            _window.DeleteEvent += (o, args) => this.Hide();
        }

        public void Show()
        {
            _window.ShowAll();
            _window.Present();
            _document.AssociatedWindows.Add(_window);
        }

        public void Hide()
        {
            _document.AssociatedWindows.Remove(_window);
            _window.Hide();
        }

        Window _window;
        Document _document;
    }
}

