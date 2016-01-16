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
using Gtk;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Collections;
using Newtonsoft.Json;
using IgeMacIntegration;

[assembly: InternalsVisibleTo("Test")]

namespace Petri.Editor
{
    public class MainClass
    {
        internal static readonly string HelpString = "Usage: mono Petri.exe [--generate|-g] [--compile|-c] [--arch|-a (32|64)] [--verbose|-v] [--] \"Path/To/Document.petri\"";

        internal static readonly string MissingPetriDocument = "The path to the Petri document must be specified as the last program argument!";
        internal static readonly string MissingGenerateOrCompile = "Must specify \"--generate\" and/or \"--compile\"!";

        internal static readonly string WrongArchitecture = "Wrong architecture specified!";
        internal static readonly string MissingArchitecture = "Missing architecture value!";

        internal static readonly int ArgumentError = 4;
        internal static readonly int CompilationFailure = 64;
        internal static readonly int UnexpectedError = 124;

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

        public static int Main(string[] args)
        {
            if(args.Length > 0) {
                bool generate = false;
                bool compile = false;
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
                    else if(args[i] == "--verbose" || args[i] == "-v") {
                        verbose = true;
                        used[i] = true;
                    }
                    else if(args[i] == "--generate" || args[i] == "-g") {
                        generate = true;
                        used[i] = true;
                    }
                    else if(args[i] == "--compile" || args[i] == "-c") {
                        compile = true;
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

                if(!compile && !generate) {
                    Console.Error.WriteLine(MissingGenerateOrCompile);
                    return PrintUsage(ArgumentError);
                }

                try {
                    HeadlessDocument document = new HeadlessDocument(path);
                    document.Load();
                    if(verbose) {
                        Console.WriteLine("Processing Petri net " + document.Settings.Name + "…");
                    }

                    string cppPath = System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Directory.GetParent(document.Path).FullName,
                                                                                   document.Settings.SourceOutputPath),
                                                            document.Settings.Name) + ".cpp";

                    bool forceGeneration = false;
                    if(!generate && compile) {
                        if(!System.IO.File.Exists(cppPath)
                           || System.IO.File.GetLastWriteTime(cppPath) < System.IO.File.GetLastWriteTime(document.Path)) {
                            generate = true;
                            forceGeneration = true;
                        }
                        else if(verbose) {
                            Console.WriteLine("Previously generated C++ code is up to date, no need for code generation");
                        }
                    }

                    if(generate) {
                        if(forceGeneration && verbose) {
                            Console.WriteLine("Previously generated C++ code is outdated or nonexistent, generating new code…");
                        }
                        document.SaveCppDontAsk();
                        document.Save();
                        if(verbose) {
                            Console.WriteLine("Successfully generated C++ code");
                        }
                    }
                    if(compile) {
                        string dylibPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Directory.GetParent(document.Path).FullName,
                                                                                             System.IO.Path.Combine(document.Settings.LibOutputPath,
                                                                                                                    document.Settings.Name + ".so")));
                        if(!System.IO.File.Exists(dylibPath) || System.IO.File.GetLastWriteTime(dylibPath) < System.IO.File.GetLastWriteTime(cppPath)) {
                            if(verbose) {
                                Console.WriteLine("Compiling the C++ code…");
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
                        else if(verbose) {
                            Console.WriteLine("Previously compiled dylib is up to date, no need for recompilation");
                        }
                    }
                }
                catch(Exception e) {
                    Console.Error.WriteLine("An exception occurred: " + e.Message);
                    return UnexpectedError;
                }

                return 0;
            }
            else {
                Application.Init();

                MainWindow.InitGUI();

                if(Configuration.RunningPlatform == Platform.Mac) {
                    MonoDevelop.MacInterop.ApplicationEvents.Quit += delegate (object sender,
                                                                               MonoDevelop.MacInterop.ApplicationQuitEventArgs e) {
                        MainClass.SaveAndQuit();
                        // If we get here, the user has cancelled the action
                        e.UserCancelled = true;
                        e.Handled = true;
                    };

                    MonoDevelop.MacInterop.ApplicationEvents.OpenDocument += delegate (object sender,
                                                                                       MonoDevelop.MacInterop.ApplicationDocumentEventArgs e) {
                        foreach(var pair in e.Documents) {
                            MainClass.OpenDocument(pair.Key);
                        }

                        e.Handled = true;
                    };

                    IgeMacMenu.GlobalKeyHandlerEnabled = true;
                }

                var document = new Document("");
                AddDocument(document);

                Application.Run();

                return 0;
            }
        }

        public static bool OnExit()
        {
            while(_documents.Count > 0) {
                if(!_documents[_documents.Count - 1].CloseAndConfirm()) {
                    return false;
                }
            }

            return true;
        }

        public static void SaveAndQuit()
        {
            bool exit = OnExit();
            if(!exit) {
                return;
            }

            Configuration.Save();
            Application.Quit();
        }

        public static String SafeMarkupFromString(string s)
        {
            return System.Web.HttpUtility.HtmlEncode(s).Replace("{", "{{").Replace("}", "}}");
        }

        public delegate void EntryValDel(Gtk.Entry e, params object[] args);

        public static void RegisterValidation(Gtk.Entry e,
                                              bool change,
                                              EntryValDel a,
                                              params object[] p)
        {
            if(change) {
                e.Changed += (obj, eventInfo) => {
                    a(e, p);
                };
            }
            else {
                e.FocusOutEvent += (obj, eventInfo) => {
                    a(e, p);
                };
                e.Activated += (o, args) => {
                    a(e, p);
                };
            }
        }

        public static void AddDocument(Document doc)
        {
            _documents.Add(doc);
        }

        public static void RemoveDocument(Document doc)
        {
            _documents.Remove(doc);
            if(_documents.Count == 0) {
                MainClass.SaveAndQuit();
            }
        }

        public static IReadOnlyList<Document> Documents {
            get {
                return _documents;
            }
        }

        public static void OpenDocument()
        {
            var fc = new Gtk.FileChooserDialog(Configuration.GetLocalized("Open Petri Net…"), null,
                                               FileChooserAction.Open,
                                               new object[] {Configuration.GetLocalized("Cancel"), ResponseType.Cancel,
                Configuration.GetLocalized("Open"), ResponseType.Accept
            });

            var filter = new FileFilter();
            filter.AddPattern("*.petri");
            fc.AddFilter(filter);

            if(fc.Run() == (int)ResponseType.Accept) {
                string filename = fc.Filename;
                fc.Destroy();
                OpenDocument(filename);
            }
            else {
                fc.Destroy();
            }
        }

        public static void OpenDocument(string filename)
        {
            foreach(var d in _documents) {
                if(d.Path == filename) {
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
                MainClass.AddDocument(doc);
            }
        }

        public static HashSet<Entity> Clipboard {
            get {
                return _clipboard;
            }
            set {
                _clipboard = value;
            }
        }

        public static int PasteCount {
            get;
            set;
        }

        static List<Document> _documents = new List<Document>();
        static HashSet<Entity> _clipboard = new HashSet<Entity>();
    }
}
