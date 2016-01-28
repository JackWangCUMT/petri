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
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Petri.Editor
{
    public class DocumentSettings
    {
        /// <summary>
        /// Creates the settings of a document with the provided serialized values. If the XML element is null, then a default set of values is assigned to the document.
        /// </summary>
        /// <returns>The settings.</returns>
        /// <param name="doc">Document.</param>
        /// <param name="node">The XML element containing the values.</param>
        public static DocumentSettings CreateSettings(HeadlessDocument doc, XElement node)
        {
            return new DocumentSettings(doc, node);
        }

        /// <summary>
        /// Gets the default settings for a new document.
        /// </summary>
        /// <returns>The default settings.</returns>
        /// <param name="doc">Document.</param>
        public static DocumentSettings GetDefaultSettings(HeadlessDocument doc)
        {
            return new DocumentSettings(doc, null);
        }

        /// <summary>
        /// An XML element that may be used for serializing the current settings.
        /// </summary>
        /// <returns>The xml.</returns>
        public XElement GetXml()
        {
            var elem = new XElement("Settings");
            elem.SetAttributeValue("Name", Name);
            elem.SetAttributeValue("Enum", Enum.ToString());
            elem.SetAttributeValue("SourceOutputPath", RelativeSourceOutputPath);
            elem.SetAttributeValue("LibOutputPath", RelativeLibOutputPath);
            elem.SetAttributeValue("Hostname", Hostname);
            elem.SetAttributeValue("Port", Port.ToString());
            elem.SetAttributeValue("Language", Language.ToString());
            elem.SetAttributeValue("RunInEditor", RunInEditor.ToString());

            var node = new XElement("Compiler");
            node.SetAttributeValue("Invocation", Compiler);
            foreach(var f in CompilerFlags) {
                var e = new XElement("Flag");
                e.SetAttributeValue("Value", f);
                node.Add(e);
            }
            elem.Add(node);

            node = new XElement("IncludePaths");
            foreach(var p in IncludePaths) {
                var e = new XElement("IncludePath");
                e.SetAttributeValue("Path", p.Item1);
                e.SetAttributeValue("Recursive", p.Item2);
                node.Add(e);
            }
            elem.Add(node);

            node = new XElement("LibPaths");
            foreach(var p in LibPaths) {
                var e = new XElement("LibPath");
                e.SetAttributeValue("Path", p.Item1);
                e.SetAttributeValue("Recursive", p.Item2);
                node.Add(e);
            }
            elem.Add(node);

            node = new XElement("Libs");
            foreach(var p in Libs) {
                var e = new XElement("Lib");
                e.SetAttributeValue("Path", p);
                node.Add(e);
            }
            elem.Add(node);

            return elem;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.DocumentSettings"/> class with a sensible set of default values if <paramref name="elem"/> is <c>null</c>, and to the values contained in the Xml element otherwise.
        /// </summary>
        /// <param name="doc">Document.</param>
        /// <param name="elem">An XML element that may be null.</param>
        private DocumentSettings(HeadlessDocument doc, XElement elem)
        {
            _document = doc;

            IncludePaths = new List<Tuple<string, bool>>();
            LibPaths = new List<Tuple<string, bool>>();
            Libs = new List<string>();
            CompilerFlags = new List<string>();

            this.Compiler = "/usr/bin/c++";
            this.RelativeSourceOutputPath = "";
            this.RelativeLibOutputPath = "";
            this.Hostname = "localhost";
            this.Port = 12345;
            this.Language = Code.Language.Cpp;
            this.RunInEditor = false;

            Name = "MyPetriNet";
            Enum = DefaultEnum;

            if(elem == null) {
                CompilerFlags.Add("-std=c++1y");
                CompilerFlags.Add("-g");
            }
            else {
                if(elem.Attribute("Name") != null) {
                    Name = elem.Attribute("Name").Value;
                }

                if(elem.Attribute("Language") != null) {
                    try {
                        Language = (Code.Language)Code.Language.Parse(Language.GetType(),
                                                                    elem.Attribute("Language").Value);
                    }
                    catch(Exception) {
                        Console.Error.WriteLine("Invalid language value: " + elem.Attribute("Language").Value);
                    }
                }

                if(elem.Attribute("Enum") != null) {
                    Enum = new Code.Enum(Language, elem.Attribute("Enum").Value);
                }
				
                if(elem.Attribute("SourceOutputPath") != null) {
                    RelativeSourceOutputPath = elem.Attribute("SourceOutputPath").Value;
                }
                if(elem.Attribute("LibOutputPath") != null) {
                    RelativeLibOutputPath = elem.Attribute("LibOutputPath").Value;
                }

                if(elem.Attribute("Hostname") != null) {
                    Hostname = elem.Attribute("Hostname").Value;
                }

                if(elem.Attribute("Port") != null) {
                    Port = UInt16.Parse(elem.Attribute("Port").Value);
                }

                if(elem.Attribute("RunInEditor") != null) {
                    RunInEditor = bool.Parse(elem.Attribute("RunInEditor").Value);
                }

                var node = elem.Element("Compiler");
                if(node != null) {
                    Compiler = node.Attribute("Invocation").Value;

                    foreach(var e in node.Elements()) {
                        CompilerFlags.Add(e.Attribute("Value").Value);
                    }
                }

                node = elem.Element("IncludePaths");
                if(node != null) {
                    foreach(var e in node.Elements()) {
                        IncludePaths.Add(Tuple.Create(e.Attribute("Path").Value,
                                                      bool.Parse(e.Attribute("Recursive").Value)));
                    }
                }

                node = elem.Element("LibPaths");
                if(node != null) {
                    foreach(var e in node.Elements()) {
                        LibPaths.Add(Tuple.Create(e.Attribute("Path").Value,
                                                  bool.Parse(e.Attribute("Recursive").Value)));
                    }
                }

                node = elem.Element("Libs");
                if(node != null) {
                    foreach(var e in node.Elements()) {
                        Libs.Add(e.Attribute("Path").Value);
                    }
                }
            }

            Modified = false;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Petri.Editor.DocumentSettings"/> has been modified since its last save.
        /// </summary>
        /// <value><c>true</c> if modified; otherwise, <c>false</c>.</value>
        public bool Modified {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the document.
        /// </summary>
        /// <value>The name of the document.</value>
        public string Name {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the enum representing the possible results of the execution of an action.
        /// </summary>
        /// <value>The enum.</value>
        public Code.Enum Enum {
            get;
            set;
        }

        /// <summary>
        /// Gets the default enum for a new document.
        /// </summary>
        /// <value>The default enum.</value>
        public Code.Enum DefaultEnum {
            get {
                return new Code.Enum(Language, "ActionResult", new string[]{ "OK", "NOK" });
            }
        }

        /// <summary>
        /// Gets the include search paths associated with the document.
        /// The first member of each entry is the path to the include search path, and the second member is set to <c>true</c> for a recursive include search path.
        /// </summary>
        /// <value>The include search paths.</value>
        public List<Tuple<string, bool>> IncludePaths {
            get;
            private set;
        }

        /// <summary>
        /// Gets the library search paths associated with the document.
        /// The first member of each entry is the path to the library search path, and the second member is set to <c>true</c> for a recursive library search path.
        /// </summary>
        /// <value>The library search paths.</value>
        public List<Tuple<string, bool>> LibPaths {
            get;
            private set;
        }

        /// <summary>
        /// Gets the library that are needed to compile the document.
        /// </summary>
        /// <value>The libraries.</value>
        public List<string> Libs {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the compiler's command.
        /// </summary>
        /// <value>The compiler command.</value>
        public string Compiler {
            get;
            set;
        }

        /// <summary>
        /// The additional user provided flags to be passed to the compiler.
        /// </summary>
        /// <value>The compiler flags.</value>
        public List<string> CompilerFlags {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the relative lib output path, i.e. the directoty that will contain the library after compilation, relative to the document's path.
        /// </summary>
        /// <value>The lib output path.</value>
        public string RelativeLibOutputPath {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the source output path, i.e. the directoty that will contain the source after generation, relative to the document's path.
        /// </summary>
        /// <value>The source output path.</value>
        public string RelativeSourceOutputPath {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the hostname the DebugClient will attempt to connect to upon debugging.
        /// </summary>
        /// <value>The hostname.</value>
        public string Hostname {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the port the DebugClient will attempt to connect to upon debugging.
        /// </summary>
        /// <value>The port.</value>
        public UInt16 Port {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the language the document uses for generation and compilation.
        /// </summary>
        /// <value>The language.</value>
        public Code.Language Language {
            get {
                return _language;
            }
            set {
                _language = value;

                if(_document.Settings != null && _document is Document) {
                    ((Document)_document).OnLanguageChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Petri.Editor.DocumentSettings"/> is to be run in the editor.
        /// This is currently only possible for C# documents, and this will result in no need for a standalone application for running the petri net.
        /// The petri net will be run directly into the debugger part of the application.
        /// </summary>
        /// <value><c>true</c> if run in editor; otherwise, <c>false</c>.</value>
        public bool RunInEditor {
            get {
                return _runInEditor;
            }
            set {
                _runInEditor = value;
            }
        }

        /// <summary>
        /// A readable name for the provided language.
        /// </summary>
        /// <returns>The name.</returns>
        /// <param name="language">Language.</param>
        public static string LanguageName(Code.Language language)
        {
            switch(language) {
            case Code.Language.Cpp:
                return "C++";
            case Code.Language.C:
                return "C";
            case Code.Language.CSharp:
                return "C#";
            }

            throw new Exception("Unsupported language!");
        }

        /// <summary>
        /// A readable name for the current document's language.
        /// </summary>
        /// <returns>The name.</returns>
        public string LanguageName()
        {
            return LanguageName(Language);
        }

        /// <summary>
        /// The path to the source file generated by the document.
        /// </summary>
        /// <value>The source path.</value>
        public string RelativeSourcePath {
            get {
                return System.IO.Path.Combine(RelativeSourceOutputPath,
                                              this.Name + "." + PetriGen.SourceExtensionFromLanguage(Language));
            }
        }

        /// <summary>
        /// The path where the document's dynamic library is to be generated, including the file's name and extension.
        /// </summary>
        /// <value>The lib path.</value>
        public string RelativeLibPath {
            get {
                return System.IO.Path.Combine(RelativeLibOutputPath, this.Name + LibExtension);
            }
        }

        /// <summary>
        /// Returns the dynamic library file extension according to the document's current language.
        /// </summary>
        /// <value>The lib extension.</value>
        public string LibExtension {
            get {
                if(Language == Code.Language.CSharp) {
                    return ".dll";
                }
                else if(Language == Code.Language.Cpp || Language == Code.Language.C) {
                    return ".so";
                }

                throw new Exception("DocumentSettings.LibExtension: Should not get there!");
            }
        }

        /// <summary>
        /// Gets the command line arguments necessary for compiling the provided source file into the requested lib.
        /// </summary>
        /// <returns>The arguments for the compiler invocation.</returns>
        /// <param name="source">The path to the source file to be compiled.</param>
        /// <param name="lib">The path and filename where the library will be generated.</param>
        public string CompilerArguments(string source, string lib)
        {
            string val = "";
            foreach(var f in CompilerFlags) {
                val += f + " ";
            }

            if(Language == Code.Language.C || Language == Code.Language.Cpp) {
                val += "-shared ";
                if(Configuration.RunningPlatform == Platform.Mac) {
                    val += "-undefined dynamic_lookup -flat_namespace ";
                }
                else if(Configuration.RunningPlatform == Platform.Linux) {
                    val += "-fPIC ";
                }

                val += "-iquote'" + RelativeSourceOutputPath + "' ";
                val += "-I'" + RelativeSourceOutputPath + "' ";

                val += GetPaths();

                foreach(var l in Libs) {
                    val += "-l'" + l + "' ";
                }

                if(Language == Code.Language.Cpp) {
                    val += "-std=c++14 ";
                }

                if(Configuration.Arch == 64) {
                    val += "-m64 ";
                }
                else if(Configuration.Arch == 32) {
                    val += "-m32 ";
                }

                val += "-o '" + lib + "' ";

                if(Language == Code.Language.Cpp) {
                    val += "-x c++ '" + source + "'";
                }
                else if(Language == Code.Language.C) {
                    val += "-x c '" + source + "'";
                }
            }
            else if(Language == Code.Language.CSharp) {
                val += "-t:library -r:CSRuntime ";

                val += GetPaths();

                foreach(var h in _document.Headers) {
                    val += "'" + h + "' ";
                }

                val += "'" + source + "' ";

                val += "-out:'" + lib + "' ";
            }
            else {
                throw new Exception("DocumentSettings.CompilerArguments: Should not get there!");
            }

            return val;
        }

        /// <summary>
        /// Returns the list of library and include paths in a string formatted for passing as command line arguments to the compiler.
        /// </summary>
        /// <returns>The paths to be passed to the compiler.</returns>
        string GetPaths()
        {
            string val = "";
            foreach(var i in LibPaths) {
                // Recursive?
                if(i.Item2) {
                    var directories = System.IO.Directory.EnumerateDirectories(i.Item1,
                                                                               "*",
                                                                               System.IO.SearchOption.AllDirectories);
                    foreach(var dir in directories) {
                        if(Language == Code.Language.C || Language == Code.Language.Cpp) {
                            val += "-L'" + dir + "' ";
                        }
                        else if(Language == Code.Language.CSharp) {
                            val += "-lib:'" + dir + "' ";
                        }
                    }
                }
                if(Language == Code.Language.C || Language == Code.Language.Cpp) {
                    val += "-L'" + i.Item1 + "' ";
                }
                else if(Language == Code.Language.CSharp) {
                    val += "-lib:'" + i.Item1 + "' ";
                }
            }

            if(Language == Code.Language.C || Language == Code.Language.Cpp) {
                foreach(var i in IncludePaths) {
                    // Recursive?
                    if(System.IO.Directory.Exists(i.Item1)) {
                        if(i.Item2) {
                            var directories = System.IO.Directory.EnumerateDirectories(i.Item1,
                                                                                       "*",
                                                                                       System.IO.SearchOption.AllDirectories);
                            foreach(var dir in directories) {
                                // Do not add dotted files
                                if(!dir.StartsWith(".")) {
                                    val += "-I'" + dir + "' ";
                                }
                            }
                        }
                    }
                    else {
                        Console.Error.WriteLine("Unable to find the include directory " + i.Item1 + "!");
                    }
                    val += "-iquote'" + i.Item1 + "' ";
                    val += "-I'" + i.Item1 + "' ";
                }
            }

            return val;
        }

        Code.Language _language;
        HeadlessDocument _document;
        bool _runInEditor;
    }
}

