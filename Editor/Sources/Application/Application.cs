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
        /*
         * The following string constants are the possible console output when invoked in compiler mode.
         * The string are used in the units tests as well.
         */
        internal static readonly string HelpString = "Usage: mono Petri.exe [--generate|-g] [--compile|-c] [--run|-r] [--clean|-k] [--arch|-a (32|64)] [--verbose|-v] [--] \"Path/To/Document.petri\"";

        internal static readonly string MissingPetriDocument = "The path to the Petri document must be specified as the last program argument!";
        internal static readonly string MissingGenerateOrCompileOrRunOrClean = "Must specify one or more of \"--generate\", \"--compile\", \"clean\", and \"--run\"!";

        internal static readonly string WrongArchitecture = "Wrong architecture specified!";
        internal static readonly string MissingArchitecture = "Missing architecture value!";

        internal static readonly int ArgumentError = 4;
        internal static readonly int CompilationFailure = 64;
        internal static readonly int UnexpectedError = 124;

        public static readonly int MaxRecentElements = 10;

        /// <summary>
        /// Prints the application's usage when invoked from a terminal, whether requested by the user with a help flag, or when a wrong flag is passed as an argument.
        /// </summary>
        /// <returns>The expected return code for the application</returns>
        /// <param name="returnCode">The return code that the function must return. If ≠ 0, then the output is done on stderr. Otherwise, the output is made on stdout.</param>
        private static int PrintUsage(int returnCode)
        {
            if(returnCode == 0) {
                Console.WriteLine(HelpString);
            }
            else {
                Console.Error.WriteLine(HelpString);
            }
            return returnCode;
        }

        /// <summary>
        /// Returns whether the argument is a short CLI option.
        /// </summary>
        /// <returns><c>true</c> if opt is a short option; otherwise, <c>false</c>.</returns>
        /// <param name="opt">Option.</param>
        static bool IsShortOption(string opt)
        {
            return System.Text.RegularExpressions.Regex.Match(opt, "^-[gcrkv]+$").Success;
        }

        /// <summary>
        /// The entry point of the program. Manages boths CLI and GUI, depending on whether arguments were given or not.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <returns>The exit code that is given to the operating system after the program ends.</returns>
        public static int Main(string[] args)
        {
            if(args.Length > 0) {
                bool generate = false;
                bool compile = false;
                bool run = false;
                bool clean = false;
                bool verbose = false;
                int arch = 0;
                var used = new bool[args.Length];
                used.Initialize();

                if(args[0] == "--help" || args[0] == "-h") {
                    return PrintUsage(0);
                }

                for(int i = 0; i < args.Length; ++i) {
                    // A getopt-like options/file separator that allows the hypothetical processing of petri net files named "--arch" or "--compile" and so on.
                    if(args[i] == "--") {
                        used[i] = true;
                        break;
                    }
                    else if(IsShortOption(args[i])) {
                        int count = 0;
                        if(args[i].Contains("v")) {
                            verbose = true;
                            ++count;
                        }
                        if(args[i].Contains("g")) {
                            generate = true;
                            ++count;
                        }
                        if(args[i].Contains("c")) {
                            compile = true;
                            ++count;
                        }
                        if(args[i].Contains("r")) {
                            run = true;
                            ++count;
                        }
                        if(args[i].Contains("k")) {
                            clean = true;
                            ++count;
                        }

                        // The argument is used if all of the short options have been consumed.
                        used[i] = count == (args[i].Length - 1);
                    }
                    else if(args[i] == "--arch" || args[i] == "-a") {
                        if(i < args.Length - 1) {
                            if(int.TryParse(args[i + 1], out arch)) {
                                if(arch == 32 || arch == 64) {
                                    Configuration.Arch = arch;
                                    Configuration.Save();
                                }
                                else {
                                    Console.Error.WriteLine(WrongArchitecture);
                                    return PrintUsage(ArgumentError);
                                }
                                used[i] = used[i + 1] = true;
                                ++i;
                            }
                            else {
                                Console.Error.WriteLine(WrongArchitecture);
                                return PrintUsage(ArgumentError);
                            }
                        }
                        else {
                            Console.Error.WriteLine(MissingArchitecture);
                            return PrintUsage(ArgumentError);
                        }
                    }
                    else if(args[i] == "--verbose") {
                        verbose = true;
                        used[i] = true;
                    }
                    else if(args[i] == "--generate") {
                        generate = true;
                        used[i] = true;
                    }
                    else if(args[i] == "--compile") {
                        compile = true;
                        used[i] = true;
                    }
                    else if(args[i] == "--run") {
                        run = true;
                        used[i] = true;
                    }
                    else if(args[i] == "--clean") {
                        clean = true;
                        used[i] = true;
                    }
                }
                for(int i = 0; i < args.Length - 1; ++i) {
                    if(!used[i]) {
                        Console.Error.WriteLine("Invalid argument \"" + args[i] + "\"");
                        return PrintUsage(ArgumentError);
                    }
                }
                if(used[args.Length - 1]) {
                    // Did not specify document path
                    Console.Error.WriteLine(MissingPetriDocument);
                    return PrintUsage(ArgumentError);
                }

                string path = args[args.Length - 1];

                if(!compile && !generate && !run && !clean) {
                    Console.Error.WriteLine(MissingGenerateOrCompileOrRunOrClean);
                    return PrintUsage(ArgumentError);
                }

                try {
                    HeadlessDocument document = new HeadlessDocument(path);
                    document.Load();
                    if(verbose) {
                        Console.WriteLine("Processing petri net \"" + document.Settings.Name + "\"…");
                    }

                    string sourcePath = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Directory.GetParent(document.Path).FullName,
                                                                                          document.Settings.RelativeSourcePath));

                    string libPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Directory.GetParent(document.Path).FullName,
                                                                                       document.Settings.RelativeLibPath));

                    if(clean) {
                        Console.Write("Cleaning artifacts of petri net \"" + document.Settings.Name + "\"… ");
                        System.IO.File.Delete(sourcePath);
                        System.IO.File.Delete(libPath);
                        Console.WriteLine("Done.");
                    }

                    bool forceGeneration = false, forceCompilation = false;
                    if(!generate && (compile || run)) {
                        if(!System.IO.File.Exists(sourcePath)
                           || System.IO.File.GetLastWriteTime(sourcePath) < System.IO.File.GetLastWriteTime(document.Path)) {
                            generate = true;
                            forceGeneration = true;
                        }
                        else if(verbose) {
                            Console.WriteLine("The previously generated " + document.Settings.LanguageName() + " code is up to date, no need for code generation.");
                        }
                    }

                    if(generate) {
                        if(forceGeneration && verbose) {
                            Console.WriteLine("The previously generated " + document.Settings.LanguageName() + " code is outdated or nonexistent, generating new code…");
                        }
                        document.GenerateCodeDontAsk();
                        document.Save();
                        if(verbose) {
                            Console.WriteLine("Successfully generated the " + document.Settings.LanguageName() + " code.");
                        }
                    }

                    if(!compile && run) {
                        if(!System.IO.File.Exists(libPath) || System.IO.File.GetLastWriteTime(libPath) < System.IO.File.GetLastWriteTime(sourcePath)) {
                            compile = true;
                            forceCompilation = true;
                        }
                        else if(verbose) {
                            Console.WriteLine("The previously compiled library is up to date, no need for recompilation.");
                        }
                    }

                    if(compile) {
                        if(forceCompilation && verbose) {
                            Console.WriteLine("The previously compiled library is outdated or nonexistent, compiling…");
                        }
                        bool res = document.Compile(false);
                        if(!res) {
                            Console.Error.WriteLine("Compilation failed, aborting!");
                            return CompilationFailure;
                        }
                        else if(verbose) {
                            Console.WriteLine("Compilation successful!");
                        }
                    }

                    if(run) {
                        RunDocument(document, verbose);
                    }
                }
                catch(Exception e) {
                    Console.Error.WriteLine("An exception occurred: " + e + " " + e.Message);
                    return UnexpectedError;
                }

                return 0;
            }
            else {
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

                Gtk.Application.Run();

                return 0;
            }
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

        /// <summary>
        /// Runs the petri net described by the document. It must have been already compiled.
        /// </summary>
        /// <param name="doc">The document to run.</param>
        /// <param name="verbose">Whether some additional info is to be ouput upon execution.</param>
        static void RunDocument(HeadlessDocument doc, bool verbose)
        {
            if(verbose) {
                Console.WriteLine("Preparing for the petri net's exection…\n");
                Console.Write("Loading the assembly… ");
            }
            var proxy = new GeneratedDynamicLibProxy(doc.Settings.Language, System.IO.Directory.GetParent(doc.Path).FullName,
                                                     doc.Settings.RelativeLibPath,
                                                     doc.Settings.Name);            
            Petri.Runtime.GeneratedDynamicLib dylib = proxy.Load();

            if(dylib == null) {
                return;
            }
            if(verbose) {
                Console.WriteLine("Assembly loaded.");
                Console.Write("Extracting the dynamic library… ");
            }

            var dynamicLib = dylib.Lib;

            if(verbose) {
                Console.WriteLine("OK.");
                Console.Write("Creating the petri net… ");
            }
            Petri.Runtime.PetriNet pn = dynamicLib.Create();
            if(verbose) {
                Console.WriteLine("OK.");
                Console.WriteLine("Ready to go! The application will automatically close when/if the petri net execution completes.\n");
            }
            pn.Run();
            pn.Join();

            if(verbose) {
                Console.Write("\nExecution complete. Unloading the library… ");
            }
            proxy.Unload();
            if(verbose) {
                Console.WriteLine("Done, will now exit.");
            }
        }

        static List<Document> _documents = new List<Document>();
        static HashSet<Entity> _clipboard = new HashSet<Entity>();
    }
}
