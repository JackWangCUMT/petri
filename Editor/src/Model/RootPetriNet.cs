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
using System.Xml;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;

namespace Petri
{
	public class RootPetriNet : PetriNet
	{
		public RootPetriNet(HeadlessDocument doc) : base(doc, null, true, new Cairo.PointD(0, 0)) {}

		public RootPetriNet(HeadlessDocument doc, XElement descriptor) : base(doc, null, descriptor) {}

		public override bool Active {
			get {
				return true;
			}
			set {
				base.Active = true;
			}
		}

		public override int RequiredTokens {
			get {
				return 0;
			}
			set {
			}
		}

		public override string Name {
			get {
				return "Root";
			}
			set {
				base.Name = "Root";
			}
		}

		public override HeadlessDocument Document {
			get;
			set;
		}

		public Tuple<Cpp.Generator, string> GenerateCpp() {
			var source = new Cpp.Generator();

			source += "#define PETRI_PREFIX \"" + Document.Settings.Name + "\"\n";

			string h = this.GenerateCpp(source, new IDManager(Document.LastEntityID));

			return Tuple.Create(source, h);
		}

		public string GetHash() {
			var source = new Cpp.Generator();

			source += "#define PETRI_PREFIX \"" + Document.Settings.Name + "\"\n";

			var hash = this.GenerateCpp(source, new IDManager(Document.LastEntityID));

			return hash;
		}

		public override string GenerateCpp(Cpp.Generator source, IDManager lastID) {
			source.AddHeader("<cstdint>");
			source.AddHeader("\"PetriDebug.h\"");
			source.AddHeader("\"PetriUtils.h\"");
			source.AddHeader("\"Action.h\"");
			source.AddHeader("\"Atomic.h\"");
			foreach(var s in Document.Headers) {
				var p1 = System.IO.Path.Combine(System.IO.Directory.GetParent(Document.Path).FullName, s);
				var p2 = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Directory.GetParent(Document.Path).FullName, Document.Settings.SourceOutputPath));
				source.AddHeader("\"" + Configuration.GetRelativePath(p1, p2) + "\"");
			}

			source += "#define EXPORT extern \"C\"";

			source += "\nusing namespace Petri;\n";

			var variables = Variables;
			var cppVar = from v in variables
			             select v.Expression;

			source += Document.GenerateVarEnum();

			source += "namespace {";
			source += "void fill(PetriNet &petriNet) {";

			foreach(var e in cppVar) {
				source += "petriNet.addVariable(static_cast<std::uint_fast32_t>(Petri_Var_Enum::" + e + "));";
			}

			base.GenerateCpp(source, lastID);
			source += "}"; // fill()
			source += "}"; // namespace

			string toHash = source.Value;

			System.Security.Cryptography.SHA1 sha = new System.Security.Cryptography.SHA1CryptoServiceProvider(); 
			// This is one implementation of the abstract class SHA1.
			string hash = BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(toHash))).Replace("-", "");

			source += "";

			source += "EXPORT void *" + Document.Settings.Name + "_create() {";
			source += "auto petriNet = std::make_unique<PetriNet>(PETRI_PREFIX);";
			source += "fill(*petriNet);";
			source += "return petriNet.release();";
			source += "}"; // create()

			source += "";

			source += "EXPORT void *" + Document.Settings.Name + "_createDebug() {";
			source += "auto petriNet = std::make_unique<PetriDebug>(PETRI_PREFIX);";
			source += "fill(*petriNet);";
			source += "return petriNet.release();";
			source += "}"; // create()

			source += "";

			source += "EXPORT char const *" + Document.Settings.Name + "_getHash() {";
			source += "return \"" + hash + "\";";
			source += "}";

			source += "";

			source += "EXPORT char const *" + Document.Settings.Name + "_getAPIDate() {";
			source += "return __TIMESTAMP__;";
			source += "}";

			return hash;
		}

		// Use this to scale down the IDs of entities to 0...N, with N = number of entities
		public void Canonize()
		{
			var entities = this.BuildEntitiesList();
			entities.Add(this);

			entities.Sort(delegate(Entity o1, Entity o2) {
				return o1.ID.CompareTo(o2.ID);
			});

			Document.LastEntityID = 0;
			foreach(Entity o in entities) {
				o.ID = Document.LastEntityID++;
				if(o is InnerPetriNet) {
					((InnerPetriNet)o).EntryPointID = Document.LastEntityID++;
				}
			}
		}

		public HashSet<Cpp.VariableExpression> Variables {
			get {
				var res = new HashSet<Cpp.VariableExpression>();
				var list = BuildEntitiesList();
				foreach(Entity e in list) {
					if(e is Action) {
						((Action)e).GetVariables(res);
					}
					if(e is Transition) {
						((Transition)e).GetVariables(res);
					}
				}

				return res;
			}
		}
	}
}

