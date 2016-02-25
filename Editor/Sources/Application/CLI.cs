using System;

namespace Petri.Editor
{
    public class CLI
    {
        /*
         * The following string constants are the possible console output when invoked in compiler mode.
         * The string are used in the units tests as well.
         */
        internal static readonly string HelpString = "Usage: mono Petri.exe [--generate|-g] [--compile|-c] [--run|-r] [--clean|-k] [--arch|-a (32|64)] [--verbose|-v] [--open|-o] [--debug|-d] [--] \"Path/To/Document.petri\"";

        internal static readonly string MissingPetriDocument = "The path to the Petri document must be specified as the last program argument!";
        internal static readonly string MissingGenerateOrCompileOrRunOrClean = "Must specify one or more of \"--generate\", \"--compile\", \"clean\", and \"--run\"!";

        internal static readonly string WrongArchitecture = "Wrong architecture specified!";
        internal static readonly string MissingArchitecture = "Missing architecture value!";

        internal static readonly int ArgumentError = 4;
        internal static readonly int CompilationFailure = 32;
        internal static readonly int RunFailure = 64;
        internal static readonly int UnexpectedError = 124;

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
        /// The entry point of the program when in command line mode.
        /// </summary>
        /// <returns>The return code of the program.</returns>
        /// <param name="args">Arguments.</param>
        public static int CLIMain(string[] args)
        {
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

            if(args[0] == "--open" || args[0] == "-o") {
                if(args.Length < 2) {
                    return PrintUsage(ArgumentError);
                }

                string[] docs = new string[args.Length - 1];

                for(int i = 1; i < args.Length; ++i) {
                    // Registering the full path of the document, so that we cannot open twice the same document later.
                    docs[i - 1] = System.IO.Path.GetFullPath(args[i]);
                }
                return Application.GUIMain(docs);
            }
            else if(args[0] == "--debug" || args[0] == "-d") {
                if(args.Length != 2) {
                    return PrintUsage(ArgumentError);
                }

                DebuggableHeadlessDocument document;
                try {
                    document = new DebuggableHeadlessDocument(args[1]);
                    document.Load();
                }
                catch(Exception e) {
                    Console.Error.WriteLine("Could not load the document: " + e.Message);
                    return RunFailure;
                }

                document.Debug();
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
                if(run) {
                    document.Settings.RunInEditor = true;
                }

                if(verbose) {
                    Console.WriteLine("Processing petri net \"" + document.Settings.Name + "\"…");
                }

                string sourcePath = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Directory.GetParent(document.Path).FullName,
                                                                                      document.Settings.RelativeSourcePath));

                string libPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Directory.GetParent(document.Path).FullName,
                                                                                   document.Settings.RelativeLibPath));

                if(clean) {
                    if(verbose) {
                        Console.Write("Cleaning artifacts of petri net \"" + document.Settings.Name + "\"… ");
                    }
                    System.IO.File.Delete(sourcePath);
                    System.IO.File.Delete(libPath);
                    if(document.Settings.Language == Code.Language.C || document.Settings.Language == Code.Language.Cpp) {
                        string headerPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Directory.GetParent(document.Path).FullName,
                                                                                              document.Settings.RelativeSourceHeaderPath));
                        System.IO.File.Delete(headerPath);
                    }
                    if(verbose) {
                        Console.WriteLine("Done.");
                    }
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
                    System.IO.File.SetLastWriteTime(path, DateTime.Now);
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
                    int result = RunDocument(document, verbose, true);
                    if(result != 0) {
                        Console.Error.WriteLine("The petri net could not be run because of an error.");
                    }

                    return result;
                }
            }
            catch(Exception e) {
                Console.Error.WriteLine("An exception occurred: " + e + " " + e.Message);
                return UnexpectedError;
            }

            return 0;
        }

        /// <summary>
        /// Runs the petri net described by the document. It must have been already compiled.
        /// </summary>
        /// <param name="doc">The document to run.</param>
        /// <param name="verbose">Whether some additional info is to be ouput upon execution.</param>
        static int RunDocument(HeadlessDocument doc, bool verbose, bool allowRetry)
        {
            if(verbose) {
                Console.WriteLine("Preparing for the petri net's exection…\n");
                Console.Write("Loading the assembly… ");
            }
            var proxy = new GeneratedDynamicLibProxy(doc.Settings.Language, System.IO.Directory.GetParent(doc.Path).FullName,
                                                     doc.Settings.RelativeLibPath,
                                                     doc.Settings.Name);            
            var dylib = proxy.Load<Runtime.GeneratedDynamicLib>();

            if(dylib == null) {
                if(allowRetry) {
                    Console.Write("Could not load the dynamic lib. Attempting recompilation… ");
                    bool res = doc.Compile(false);
                    if(!res) {
                        throw new Exception("Compilation failure!");
                    }
                    Console.WriteLine("Done.");
                    Console.WriteLine("Attempting execution again…");
                    return RunDocument(doc, verbose, false);
                }

                return RunFailure;
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

            return 0;
        }

    }
}

