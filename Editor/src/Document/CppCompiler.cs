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

namespace Petri
{
	public class CppCompiler {
		public CppCompiler(HeadlessDocument doc) {
			_document = doc;
		}

		public string CompileSource(string source, string lib) {
			string cd = System.IO.Directory.GetCurrentDirectory();
			System.IO.Directory.SetCurrentDirectory(System.IO.Directory.GetParent(_document.Path).FullName);
			if(!System.IO.File.Exists(source)) {
				return "Erreur : le fichier \"" + source + "\" n'existe pas. Veuillez générer le code avant de compiler.";
			}

			System.IO.File.SetLastWriteTime(source, DateTime.Now);

			Process p = new Process();

			p.StartInfo.UseShellExecute = false;
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.RedirectStandardError = true;
			p.StartInfo.FileName = _document.Settings.Compiler;
			string s = _document.Settings.CompilerArguments(source, lib);
			if(s.Length < 5000) {
				p.StartInfo.Arguments = s;
			}
			else {
				return "Erreur : l'invocation du compilateur est trop longue (" + s.Length.ToString() + " caractères. Essayez de supprimer des chemins d'inclusion récursifs.";
			}
			p.Start();

			string output = p.StandardOutput.ReadToEnd();
			output += p.StandardError.ReadToEnd();
			p.WaitForExit();

			System.IO.Directory.SetCurrentDirectory(cd);

			return output;
		}

		HeadlessDocument _document;
	}

	public class CompilationErrorPresenter {
		public CompilationErrorPresenter(Document doc, string error) {
			_document = doc;

			_window = new Window(WindowType.Toplevel);
			_window.Title = "Sortie de la compilation de " + doc.Window.Title;

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

		public void Show() {
			_window.ShowAll();
			_window.Present();
			_document.AssociatedWindows.Add(_window);
		}

		public void Hide() {
			_document.AssociatedWindows.Remove(_window);
			_window.Hide();
		}

		Window _window;
		Document _document;
	}
}

