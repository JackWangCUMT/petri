using System;
using System.Xml;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Petri
{
	public class RootPetriNet : PetriNet
	{
		public RootPetriNet(Document doc) : base(doc, null, true, new Cairo.PointD(0, 0))
		{
			Document.Controller.Modified = false;
		}

		public RootPetriNet(Document doc, XElement descriptor) : base(doc, null, descriptor)
		{
			Document.Controller.Modified = false;
		}

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

		public override Document Document {
			get {
				return document;
			}
			set {
				document = value;
			}
		}

		public Tuple<Cpp.Generator, string> GenerateCpp()
		{
			var source = new Cpp.Generator();

			string h = this.GenerateCpp(source, new IDManager(Document.LastEntityID));

			return Tuple.Create(source, h);
		}

		public string GetHash() {
			var source = new Cpp.Generator();

			var hash = this.GenerateCpp(source, new IDManager(Document.LastEntityID));

			return hash;
		}

		public override string GenerateCpp(Cpp.Generator source, IDManager lastID) {
			source.AddHeader("\"PetriUtils.h\"");
			foreach(var s in Document.Controller.Headers) {
				source.AddHeader("\"" + s + "\"");
			}

			source += "#define EXPORT __attribute__((visibility(\"default\"))) extern \"C\"\n";

			source += "EXPORT void *" + Document.Settings.Name + "_create() {";
			source += "auto petriNet = std::make_unique<PetriNet>();";
			source += "\n";

			base.GenerateCpp(source, lastID);

			source += "";

			source += "return petriNet.release();";

			source += "}"; // create()

			string toHash = source.Value;

			System.Security.Cryptography.SHA1 sha = new System.Security.Cryptography.SHA1CryptoServiceProvider(); 
			// This is one implementation of the abstract class SHA1.
			string hash = BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(toHash))).Replace("-", "");

			source += "EXPORT char const *" + Document.Settings.Name + "_getHash() {";
			source += "return \"" + hash + "\";";
			source += "}";

			return hash;
		}

		// Use this to scale down the IDs of Actions (resp. Transitions) to 0...N, with N = number of Actions (resp. Transitions)
		public void Canonize()
		{
			var states = this.BuildActionsList();
			states.Add(this);
			states.AddRange(this.Transitions);

			states.Sort(delegate(Entity o1, Entity o2) {
				return o1.ID.CompareTo(o2.ID);
			});

			Document.LastEntityID = 0;
			foreach(Entity o in states) {
				o.ID = Document.LastEntityID;
				++Document.LastEntityID;
			}

			Document.Controller.Modified = true;
		}

		private Document document;
	}
}

