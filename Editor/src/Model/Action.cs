using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace Petri
{
	public sealed class Action : NonRootState
	{
		public enum ResultatAction {
			REUSSI, RATE, BLOQUE_PAR_ADV, TIMEOUT, BLOQUE
		};

		public Action (HeadlessDocument doc, PetriNet parent, bool active, Cairo.PointD pos) : base(doc, parent, active, pos)
		{
			this.Position = pos;
			this.Radius = 20;

			this.Active = active;

			this.Function = this.DefaultAction();
		}

		public Action(HeadlessDocument doc, PetriNet parent, XElement descriptor, IEnumerable<Cpp.Function> functions, IDictionary<string, string> macros) : base(doc, parent, descriptor) {
			this.Function = Cpp.Expression.CreateFromString<Cpp.FunctionInvocation>(descriptor.Attribute("Function").Value, this, functions, macros);
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
			return new Petri.Cpp.FunctionInvocation(Action.DefaultFunction, new Petri.Cpp.EntityExpression(this, "this"));
		}

		public static Cpp.Function DefaultFunction {
			get {
				if(_defaultFunction == null) {
					_defaultFunction = new Cpp.Function(new Cpp.Type("ResultatAction", Cpp.Scope.EmptyScope()), Cpp.Scope.MakeFromNamespace("PetriUtils", Cpp.Scope.EmptyScope()), "defaultAction", false);
					_defaultFunction.AddParam(new Cpp.Param(new Cpp.Type("Action *", Cpp.Scope.EmptyScope()), "action"));
				}

				return _defaultFunction;
			}
		}

		public static Cpp.Function DoNothingFunction {
			get {
				if(_doNothingFunction == null) {
					_doNothingFunction = new Cpp.Function(new Cpp.Type("ResultatAction", Cpp.Scope.EmptyScope()), Cpp.Scope.MakeFromNamespace("PetriUtils", Cpp.Scope.EmptyScope()), "doNothing", false);
					_doNothingFunction.AddParam(new Cpp.Param(new Cpp.Type("ResultatAction", Cpp.Scope.EmptyScope()), "resultat"));
				}

				return _doNothingFunction;
			}
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

		public override bool UsesHeader(string h) {
			return this.Function.Function.Header == h;
		}

		public bool IsDefault()
		{
			return this.Function.Function.Name == "defaultAction" && this.Function.Function.Enclosing.Equals(Cpp.Scope.MakeFromNamespace("PetriUtils", Cpp.Scope.EmptyScope()));
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
		static Cpp.Function _defaultFunction;
		static Cpp.Function _doNothingFunction;
	}
}

