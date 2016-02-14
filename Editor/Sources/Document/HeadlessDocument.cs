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
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml;
using System.Linq;
using Petri.Editor.Code;

namespace Petri.Editor
{
    /// <summary>
    /// A class that intends to encapsulate an ever increasing sequence of identifiers.
    /// </summary>
    public class IDManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.IDManager"/> with the parameter as the first ID that will be consumed.
        /// </summary>
        /// <param name="firstID">First ID.</param>
        public IDManager(UInt64 firstID)
        {
            ID = firstID;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.IDManager"/> class.
        /// The first ID that will be consumed by the new instance will be the next ID that would have been consumed by the parameter.
        /// </summary>
        /// <param name="reference">Reference.</param>
        public IDManager(IDManager reference)
        {
            ID = reference.ID + 1;
        }

        /// <summary>
        /// Consume a new ID from this instance and returns it.
        /// </summary>
        public UInt64 Consume()
        {
            return ID++;
        }

        /// <summary>
        /// Gets the current ID of this instance.
        /// </summary>
        /// <value>The I.</value>
        public UInt64 ID {
            get;
            private set;
        }
    }

    public class HeadlessDocument
    {
        internal HeadlessDocument(string path, DocumentSettings settings) : this(path)
        {
            Settings = settings;
        }

        public HeadlessDocument(string path)
        {
            LastHeadersUpdate = DateTime.MinValue;
            Headers = new List<string>();
            CodeActions = new List<Function>();
            AllFunctionsList = new List<Function>();
            CodeConditions = new List<Function>();

            PreprocessorMacros = new Dictionary<string, string>();

            Conflicting = new Dictionary<Entity, string>();

            Path = path;
            ResetID();
        }

        public string Path {
            get;
            set;
        }

        public List<string> Headers {
            get;
            private set;
        }

        public virtual void AddHeader(string header)
        {
            AddHeaderNoUpdate(header);
        }

        public void AddHeaderNoUpdate(string header)
        {
            if(header.Length == 0 || Headers.Contains(header))
                return;

            if(header.Length > 0) {
                string filename = header;

                // If path is relative, then make it absolute
                if(!System.IO.Path.IsPathRooted(header)) {
                    filename = System.IO.Path.Combine(System.IO.Directory.GetParent(this.Path).FullName,
                                                      filename);
                }

                var functions = Parser.Parse(Settings.Language, filename);
                foreach(var func in functions) {
                    AllFunctionsList.Add(func);
                }

                Headers.Add(header);
            }
        }

        public Dictionary<string, string> PreprocessorMacros {
            get;
            private set;
        }

        public List<Function> CodeConditions {
            get;
            private set;
        }

        public List<Function> AllFunctionsList {
            get;
            private set;
        }

        public IEnumerable<Function> AllFunctions {
            get {
                Function[] ff = new Function[AllFunctionsList.Count + 4];
                ff[0] = RuntimeFunctions.DoNothingFunction(this);
                ff[1] = RuntimeFunctions.PrintFunction(this);
                ff[2] = RuntimeFunctions.PauseFunction(this);
                ff[3] = RuntimeFunctions.RandomFunction(this);
                for(int i = 0; i < AllFunctionsList.Count; ++i) {
                    ff[i + 4] = AllFunctionsList[i];
                }

                return ff;
            }
        }

        public List<Function> CodeActions {
            get;
            private set;
        }

        public void DispatchFunctions()
        {
            CodeActions.Clear();
            CodeConditions.Clear();
            Code.Type e = Settings.Enum.Type, b = new Code.Type(Settings.Language, "bool");

            foreach(Function f in AllFunctions) {
                if(f.ReturnType.Equals(e)) {
                    CodeActions.Add(f);
                }
                else if(f.ReturnType.Equals(b)) {
                    CodeConditions.Add(f);
                }
            }
        }

        public RootPetriNet PetriNet {
            get;
            set;
        }

        public string GetRelativeToDoc(string path)
        {
            if(!System.IO.Path.IsPathRooted(path)) {
                path = System.IO.Path.GetFullPath(path);
            }

            string parent = System.IO.Directory.GetParent(Path).FullName;

            return Configuration.GetRelativePath(path, parent);
        }

        public string GetAbsoluteFromRelativeToDoc(string path)
        {
            if(!System.IO.Path.IsPathRooted(path)) {
                return System.IO.Path.Combine(System.IO.Directory.GetParent(Path).FullName, path);
            }

            return path;
        }

        protected virtual Tuple<int, int> GetWindowSize()
        {
            return Tuple.Create(_wW, _wH);
        }

        protected virtual void SetWindowSize(int w, int h)
        {
            _wW = w;
            _wH = h;
        }

        protected virtual Tuple<int, int> GetWindowPosition()
        {
            return Tuple.Create(_wX, _wY);
        }

        protected virtual void SetWindowPosition(int x, int y)
        {
            _wX = x;
            _wY = y;
        }

        protected DateTime LastHeadersUpdate {
            get;
            set;
        }

        /// <summary>
        /// Save the document to disk, or throw an Exception if the Path is empty.
        /// </summary>
        public virtual void Save()
        {
            string tempFileName = "";
            if(Path == "") {
                throw new Exception(Configuration.GetLocalized("Empty path!"));
            }

            var doc = new XDocument();
            var root = new XElement("Document");

            root.Add(Settings.GetXml());

            var winConf = new XElement("Window");
            {
                var size = GetWindowSize();
                var position = GetWindowPosition();
                winConf.SetAttributeValue("X", position.Item1.ToString());
                winConf.SetAttributeValue("Y", position.Item2.ToString());
                winConf.SetAttributeValue("W", size.Item1.ToString());
                winConf.SetAttributeValue("H", size.Item2.ToString());
            }

            var headers = new XElement("Headers");
            foreach(var h in Headers) {
                var hh = new XElement("Header");
                hh.SetAttributeValue("File", h);
                headers.Add(hh);
            }
            var macros = new XElement("Macros");
            foreach(var m in PreprocessorMacros) {
                var mm = new XElement("Macro");
                mm.SetAttributeValue("Name", m.Key);
                mm.SetAttributeValue("Value", m.Value);
                macros.Add(mm);
            }
            doc.Add(root);
            root.Add(winConf);
            root.Add(headers);
            root.Add(macros);
            root.Add(PetriNet.GetXML());

            // Write to a temporary file to avoid corrupting the existing document on error
            tempFileName = System.IO.Path.GetTempFileName();
            XmlWriterSettings xsettings = new XmlWriterSettings();
            xsettings.Indent = true;
            xsettings.Encoding = new System.Text.UTF8Encoding(false);
            XmlWriter writer = XmlWriter.Create(tempFileName, xsettings);

            doc.Save(writer);
            writer.Flush();
            writer.Close();

            if(System.IO.File.Exists(this.Path))
                System.IO.File.Delete(this.Path);
            System.IO.File.Move(tempFileName, this.Path);
            tempFileName = "";

            Settings.Modified = false;
        }

        /// <summary>
        /// Loads the document from disk.
        /// </summary>
        public void Load()
        {
            var document = XDocument.Load(Path);

            var elem = document.FirstNode as XElement;

            Settings = DocumentSettings.CreateSettings(this, elem.Element("Settings"));

            var winConf = elem.Element("Window");
            SetWindowPosition(int.Parse(winConf.Attribute("X").Value),
                              int.Parse(winConf.Attribute("Y").Value));
            SetWindowSize(int.Parse(winConf.Attribute("W").Value),
                          int.Parse(winConf.Attribute("H").Value));

            Headers.Clear();
            AllFunctionsList.Clear();

            PreprocessorMacros.Clear();

            var node = elem.Element("Headers");
            if(node != null) {
                foreach(var e in node.Elements()) {
                    this.AddHeaderNoUpdate(e.Attribute("File").Value);
                }
            }
            LastHeadersUpdate = DateTime.Now;

            node = elem.Element("Macros");
            if(node != null) {
                foreach(var e in node.Elements()) {
                    PreprocessorMacros.Add(e.Attribute("Name").Value, e.Attribute("Value").Value);
                }
            }

            DispatchFunctions();

            PetriNet = new RootPetriNet(this, elem.Element("PetriNet"));
            PetriNet.Canonize();
        }

        public string CodePrefix {
            get {
                return Settings.Name;
            }
        }

        /// <summary>
        /// Generates the code without prompting the user. If no code have ever been generated for this document, meaning we don't know where to save it, then an Exception is thrown.
        /// <exception cref="Exception">When no save path has been defined for the document.</exception>
        /// </summary>
        public Dictionary<Entity, CodeRange> GenerateCodeDontAsk()
        {
            if(this.Settings.RelativeSourceOutputPath.Length == 0) {
                throw new Exception(Configuration.GetLocalized("No source output path defined. Please open the Petri net with the graphical editor and generate the <language> code once.",
                                                               Settings.LanguageName()));
            }

            var generator = PetriGen.PetriGenFromLanguage(Settings.Language, this);
            generator.WritePetriNet();

            return generator.CodeRanges;
        }

        /// <summary>
        /// Compiles the document. If some output is made by the invoked compiler, then the method returns false, true otherwise.
        /// </summary>
        /// <param name="wait">Unused.</param>
        public virtual bool Compile(bool wait)
        {
            var c = new Compiler(this);
            var o = c.CompileSource(Settings.RelativeSourcePath, Settings.RelativeLibPath);
            if(o != "") {
                ParseCompilationErrors(o);

                return false;
            }

            return true;
        }

        /// <summary>
        /// Parses the compilation errors and attempt to give a meaningful diagnostic.
        /// </summary>
        /// <param name="errors">The error string.</param>
        protected virtual void ParseCompilationErrors(string errors)
        {
            Console.Error.WriteLine(Configuration.GetLocalized("Compilation failed.")
            + "\n" + Configuration.GetLocalized("Compiler invocation:")
            + "\n" + Settings.Compiler
            + " " + Settings.CompilerArguments(Settings.RelativeSourcePath,
                                               Settings.RelativeLibPath) + "\n\n" + Configuration.GetLocalized("Compilation errors:") + "\n" + errors);
        
        }

        /// <summary>
        /// Gets or sets the ID manager of the document.
        /// </summary>
        /// <value>The identifier manager.</value>
        public IDManager IDManager {
            get;
            set;
        }

        public Entity EntityFromID(UInt64 id)
        {
            return PetriNet.EntityFromID(id);
        }

        public void ResetID()
        {
            ////this.LastEntityID = 0;
            IDManager = new IDManager(0);
        }

        /// <summary>
        /// Gets or sets the document's settings.
        /// </summary>
        /// <value>The settings.</value>
        public DocumentSettings Settings {
            get;
            protected set;
        }

        public string Hash {
            get {
                var generator = PetriGen.PetriGenFromLanguage(Settings.Language, this);
                return generator.GetHash();
            }
        }

        public Dictionary<Entity, string> Conflicting {
            get;
            private set;
        }

        public void AddConflicting(Entity entity, string message)
        {
            string value;
            if(Conflicting.TryGetValue(entity, out value)) {
                value = value + "\n\n" + message;
            }
            else {
                value = message;
            }

            Conflicting[entity] = value;
        }

        public bool Conflicts(Entity e)
        {
            if(Conflicting.ContainsKey(e)) {
                return true;
            }
            else if(e is PetriNet) {
                foreach(var ee in Conflicting) {
                    if(((PetriNet)e).EntityFromID(ee.Key.ID) != null) {
                        return true;
                    }
                }
            }

            return false;
        }

        int _wX, _wY, _wW, _wH;
    }
}

