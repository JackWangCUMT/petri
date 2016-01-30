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
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Newtonsoft.Json;

[assembly: InternalsVisibleTo("Test")]

namespace Petri.Editor
{
    public class Application
    {
        /// <summary>
        /// The max number of recent elements in the File menu.
        /// </summary>
        public static readonly int MaxRecentElements = 10;

        /// <summary>
        /// The entry point of the program. Manages boths CLI and GUI, depending on whether arguments were given or not.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <returns>The exit code that is given to the operating system after the program ends.</returns>
        public static int Main(string[] args)
        {
            if(args.Length > 0) {
                return CLI.CLIMain(args);
           }
            else {
                return GUIMain(null);
            }
        }

        /// <summary>
        /// The entry point of the program when in GUI mode.
        /// </summary>
        /// <returns>The return code of the program.</returns>
        /// <param name="docsToOpen">An optional list of documents to open at launch. May be <c>null</c>.</param>
        public static int GUIMain(string[] docsToOpen)
        {
            Gtk.Application.Init();

            MainWindow.InitGUI();

            RecentDocumentEntry[] entries = JsonConvert.DeserializeObject<RecentDocumentEntry[]>(Configuration.RecentDocuments);
            var result = new SortedList<DateTime, string>(Comparer<DateTime>.Create((date1,
                                                                                     date2) => date2.CompareTo(date1)));
            if(entries != null) {
                foreach(var entry in entries) {
                    if(entry != null) {
                        result.Add(DateTime.Parse(entry.date), entry.path);
                    }
                }
            }
            RecentDocuments = result;
            TrimRecentDocuments();

            var document = new Document("");
            AddDocument(document);

            if(docsToOpen != null) {
                foreach(string path in docsToOpen) {
                    OpenDocument(path);
                }
            }

            Gtk.Application.Run();

            return 0;
        }

        /// <summary>
        /// Closes and asks for save for each unsaved document. If one of the save dialog is cancelled, then the exit is cancelled.
        /// </summary>
        /// <returns>Whether the app termination should continue.</returns>
        public static bool OnExit()
        {
            while(_documents.Count > 0) {
                if(!_documents[_documents.Count - 1].CloseAndConfirm()) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Closes every open document (asks saving if necessary), saves the app's configuration and exits the program.
        /// </summary>
        public static void SaveAndQuit()
        {
            bool exit = OnExit();
            if(!exit) {
                return;
            }

            Configuration.Save();
            Gtk.Application.Quit();
        }

        /// <summary>
        /// Takes a string and escapes it appropriately for usage in different GTK widgets that parses markup language.
        /// </summary>
        /// <returns>The escaped string.</returns>
        /// <param name="s">The string to escape.</param>
        public static String SafeMarkupFromString(string s)
        {
            return System.Web.HttpUtility.HtmlEncode(s).Replace("{", "{{").Replace("}", "}}");
        }

        /// <summary>
        /// Entry validation delegate.
        /// </summary>
        public delegate void EntryValidationDel(Gtk.Entry e, params object[] args);

        /// <summary>
        /// Registers the specified delegate to be triggered when the Entry is changed (if <c>change == true</c>)
        /// or validated (if <c>change == false</c>; the validation is when the widget loses its focus or the Enter key is pressed).
        /// </summary>
        /// <param name="e">The Entry widget.</param>
        /// <param name="change">If set to <c>true</c> then the delegate will be invoked on each text change in the entry. Otherwise, only on end of input.</param>
        /// <param name="del">The delegate that will be invoked.</param>
        /// <param name="p">The arguments list that will be passed to the delegate upon invocation.</param>
        public static void RegisterValidation(Gtk.Entry e,
                                              bool change,
                                              EntryValidationDel del,
                                              params object[] p)
        {
            if(change) {
                e.Changed += (obj, eventInfo) => {
                    del(e, p);
                };
            }
            else {
                e.FocusOutEvent += (obj, eventInfo) => {
                    del(e, p);
                };
                e.Activated += (o, args) => {
                    del(e, p);
                };
            }
        }

        /// <summary>
        /// Adds a document to the list of opened documents.
        /// </summary>
        /// <param name="doc">The document to register.</param>
        public static void AddDocument(Document doc)
        {
            _documents.Add(doc);
        }

        /// <summary>
        /// Removes a document from the list of opened documents.
        /// </summary>
        /// <param name="doc">The document to unregister.</param>
        public static void RemoveDocument(Document doc)
        {
            _documents.Remove(doc);
            if(_documents.Count == 0) {
                Application.SaveAndQuit();
            }
        }

        /// <summary>
        /// The list of opened documents
        /// </summary>
        /// <value>The documents.</value>
        public static IReadOnlyList<Document> Documents {
            get {
                return _documents;
            }
        }

        /// <summary>
        /// Opens a document through the file chooser dialog.
        /// </summary>
        public static void OpenDocument()
        {
            // Prevents 2 dialogs from being opened at the same time
            if(!_opening) {
                try {
                    _opening = true;
                    var fc = new Gtk.FileChooserDialog(Configuration.GetLocalized("Open Petri Net…"), null,
                                                       Gtk.FileChooserAction.Open,
                                                       new object[] {Configuration.GetLocalized("Cancel"), Gtk.ResponseType.Cancel,
                        Configuration.GetLocalized("Open"), Gtk.ResponseType.Accept
                    });

                    var filter = new Gtk.FileFilter();
                    filter.AddPattern("*.petri");
                    fc.AddFilter(filter);

                    if(fc.Run() == (int)Gtk.ResponseType.Accept) {
                        string filename = fc.Filename;
                        fc.Destroy();
                        OpenDocument(filename);
                    }
                    else {
                        fc.Destroy();
                    }
                }
                finally {
                    _opening = false;
                }
            }
        }

        /// <summary>
        /// Opens a document by its path. If the document is already opened, then its window is brought to front.
        /// </summary>
        /// <param name="filename">Filename.</param>
        public static void OpenDocument(string filename)
        {
            int index = RecentDocuments.IndexOfValue(filename);
            if(index != -1) {
                RecentDocuments.RemoveAt(index);
            }

            foreach(var d in _documents) {
                if(d.Path == filename) {
                    RecentDocuments.Add(DateTime.UtcNow, filename);
                    UpdateRecentDocuments();
                    d.Window.Present();
                    return;
                }
            }

            // Reuse last blank document which was created (typically the first one created on program launch)
            if(_documents.Count > 0 && _documents[_documents.Count - 1].Blank) {
                _documents[_documents.Count - 1].Path = filename;
                _documents[_documents.Count - 1].Restore();
                _documents[_documents.Count - 1].Window.Present();
            }
            else {
                var doc = new Document(filename);
                Application.AddDocument(doc);
            }

            RecentDocuments.Add(DateTime.UtcNow, filename);
            UpdateRecentDocuments();
        }

        /// <summary>
        /// Gets or sets the application's clipboard's content.
        /// </summary>
        /// <value>The clipboard.</value>
        public static HashSet<Entity> Clipboard {
            get {
                return _clipboard;
            }
            set {
                _clipboard = value;
            }
        }

        /// <summary>
        /// The number of times a group of entity which have been copied have been pasted. This is intended to suffix new the entities with an increasing number.
        /// </summary>
        /// <value>The paste count.</value>
        public static int PasteCount {
            get;
            set;
        }

        /// <summary>
        /// Recent document entry.
        /// </summary>
        private class RecentDocumentEntry
        {
            public string date { get; set; }

            public string path { get; set; }
        }

        /// <summary>
        /// Remove the documents that do not exist anymore on the filesystem.
        /// </summary>
        public static void TrimRecentDocuments()
        {
            var recentDocuments = RecentDocuments;
            var dict = new SortedList<DateTime, string>(recentDocuments);

            foreach(var doc in dict) {
                if(!System.IO.File.Exists(doc.Value)) {
                    recentDocuments.Remove(doc.Key);
                }
            }
        }

        /// <summary>
        /// Updates the recent documents menu items in the opened documents.
        /// </summary>
        public static void UpdateRecentDocuments()
        {
            while(RecentDocuments.Count > MaxRecentElements) {
                RecentDocuments.Remove(RecentDocuments.Keys[RecentDocuments.Keys.Count - 1]);
            }
            var entries = new RecentDocumentEntry[RecentDocuments.Count];
            int i = 0;
            foreach(var v in RecentDocuments) {
                var entry = new RecentDocumentEntry();
                entry.date = v.Key.ToString();
                entry.path = v.Value;
                entries[i++] = entry;
            }

            Configuration.RecentDocuments = JsonConvert.SerializeObject(entries);

            foreach(var doc in Documents) {
                doc.Window.UpdateRecentDocuments();
            }
        }

        /// <summary>
        /// Gets the list of recent documents, indexed by their last opening date (UTC).
        /// </summary>
        /// <value>The recent documents.</value>
        public static SortedList<DateTime, string> RecentDocuments {
            get;
            private set;
        }

        static List<Document> _documents = new List<Document>();
        static HashSet<Entity> _clipboard = new HashSet<Entity>();
        static bool _opening = false;
    }
}
