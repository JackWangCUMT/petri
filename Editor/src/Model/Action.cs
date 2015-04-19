using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace Petri
{
	public sealed class Action : NonRootState
	{
		public Action (HeadlessDocument doc, PetriNet parent, bool active, Cairo.PointD pos) : base(doc, parent, active, pos)
		{
			this.Position = pos;
			this.Radius = 20;

			this.Active = active;

			this.Function = this.DefaultAction();
		}

		public Action(HeadlessDocument doc, PetriNet parent, XElement descriptor, IEnumerable<Cpp.Function> functions, IDictionary<string, string> macros) : base(doc, parent, descriptor) {
			Cpp.FunctionInvocation exp;
			try {
				exp = Cpp.Expression.CreateFromString<Cpp.FunctionInvocation>(descriptor.Attribute("Function").Value, this, functions, macros);
				if(!exp.Function.ReturnType.Equals(doc.Settings.Enum.Type)) {
					doc.Conflicting.Add(this);
				}
				Function = exp;
			}
			catch(Exception e) {
				doc.Conflicting.Add(this);
				// TODO: which invocation?
			}
		}

		public override XElement GetXml() {
			var elem = new XElement("Action");
			this.Serialize(elem);
			return elem;
		}

		public override void Serialize(XElement element) {
			base.Serialize(element);
			element.SetAttributeValue("Function", this.Function.MakeUserReadable());
		}

		public Cpp.FunctionInvocation DefaultAction() {
			return new Petri.Cpp.FunctionInvocation(DefaultFunction(Document), new Petri.Cpp.EntityExpression(this, "this"));
		}

		public static Cpp.Function DefaultFunction(HeadlessDocument doc) {
			var f = new Cpp.Function(doc.Settings.Enum.Type, Cpp.Scope.MakeFromNamespace("PetriUtils", Cpp.Scope.EmptyScope), "defaultAction", false);
			f.AddParam(new Cpp.Param(new Cpp.Type("Action *", Cpp.Scope.EmptyScope), "action"));

			return f;
		}

		public static Cpp.Function DoNothingFunction(HeadlessDocument doc) {
			var f = new Cpp.Function(doc.Settings.Enum.Type, Cpp.Scope.MakeFromNamespace("PetriUtils", Cpp.Scope.EmptyScope), "doNothing", false);
			f.AddParam(new Cpp.Param(doc.Settings.Enum.Type, "resultat"));

			return f;
		}

		public Cpp.FunctionInvocation Function {
			get {
				return _function;
			}
			set {
				_function = value;
				if(this.IsDefault())
					_function = this.DefaultAction();
			}
		}

		public override bool UsesFunction(Cpp.Function f) {
			return Function.UsesFunction(f);
		}

		public bool IsDefault()
		{
			return this.Function.Function.Name == "defaultAction" && this.Function.Function.Enclosing.Equals(Cpp.Scope.MakeFromNamespace("PetriUtils", Cpp.Scope.EmptyScope));
		}

		public override string GenerateCpp(Cpp.Generator source, IDManager lastID) {
			source += "auto " + this.CppName + " = std::make_shared<Action>();";
			source += this.CppName + "->setAction(" + this.Function.MakeCpp() + ");";
			source += this.CppName + "->setRequiredTokens(" + this.RequiredTokens.ToString() + ");";

			source += this.CppName + "->setName(\"" + this.Parent.Name + "_" + this.Name + "\");";
			source += this.CppName + "->setID(" + this.ID.ToString() + ");";
			source += "petriNet.addAction(" + this.CppName + ", " + ((this.Active && (this.Parent is RootPetriNet)) ? "true" : "false") + ");";

			return "";
		}

		Cpp.FunctionInvocation _function;
	}
}

