using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Linq;

namespace Petri
{
	public sealed class Action : NonRootState
	{
		public Action (HeadlessDocument doc, PetriNet parent, bool active, Cairo.PointD pos) : base(doc, parent, active, pos)
		{
			this.Position = pos;
			this.Radius = 20;

			this.Active = active;

			this.Function = new Cpp.FunctionInvocation(DoNothingFunction(doc));
		}

		public Action(HeadlessDocument doc, PetriNet parent, XElement descriptor) : base(doc, parent, descriptor) {
			TrySetFunction(descriptor.Attribute("Function").Value);
		}

		private void TrySetFunction(string s) {
			Cpp.Expression exp;
			try {
				exp = Cpp.Expression.CreateFromString<Cpp.Expression>(s, this);
				if(exp is Cpp.FunctionInvocation) {
					var f = (Cpp.FunctionInvocation)exp;
					if(!f.Function.ReturnType.Equals(Document.Settings.Enum.Type)) {
						Document.Conflicting.Add(this);
					}
					Function = f;
				}
				else {
					Function = new Cpp.WrapperFunctionInvocation(Document.Settings.Enum.Type, exp);
				}
			}
			catch(Exception) {
				Document.Conflicting.Add(this);
				Function = new Cpp.ConflictFunctionInvocation(s);
			}
		}

		public override void UpdateConflicts() {
			this.TrySetFunction(Function.MakeUserReadable());
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

		public Cpp.FunctionInvocation PrintAction() {
			return new Petri.Cpp.FunctionInvocation(PrintFunction(Document), Cpp.LiteralExpression.CreateFromString("$Name", this), Cpp.LiteralExpression.CreateFromString("$ID", this));
		}

		public static Cpp.Function PrintFunction(HeadlessDocument doc) {
			var f = new Cpp.Function(doc.Settings.Enum.Type, Cpp.Scope.MakeFromNamespace("PetriUtils", Cpp.Scope.EmptyScope), "printAction", true);
			f.AddParam(new Cpp.Param(new Cpp.Type("std::string const &", Cpp.Scope.EmptyScope), "name"));
			f.AddParam(new Cpp.Param(new Cpp.Type("std::uint64_t", Cpp.Scope.EmptyScope), "id"));
			f.TemplateArguments = doc.Settings.Enum.Name;

			return f;
		}

		public static Cpp.Function DoNothingFunction(HeadlessDocument doc) {
			var f = new Cpp.Function(doc.Settings.Enum.Type, Cpp.Scope.MakeFromNamespace("PetriUtils", Cpp.Scope.EmptyScope), "doNothing", true);
			f.TemplateArguments = doc.Settings.Enum.Name;

			return f;
		}

		public static Cpp.Function PauseFunction(HeadlessDocument doc) {
			var f = new Cpp.Function(doc.Settings.Enum.Type, Cpp.Scope.MakeFromNamespace("PetriUtils", Cpp.Scope.EmptyScope), "pause", true);
			f.AddParam(new Cpp.Param(new Cpp.Type("", Cpp.Scope.EmptyScope), "delai"));
			f.TemplateArguments = doc.Settings.Enum.Name;

			return f;
		}

		public Cpp.FunctionInvocation Function {
			get {
				return _function;
			}
			set {
				_function = value;
			}
		}

		public override bool UsesFunction(Cpp.Function f) {
			return Function.UsesFunction(f);
		}

		public override string GenerateCpp(Cpp.Generator source, IDManager lastID) {
			source += "auto " + this.CppName + " = std::make_shared<Action<" + Document.Settings.Enum.Name + ">>();";

			var old = new Dictionary<Cpp.LiteralExpression, string>();
			string enumName = Document.Settings.Enum.Name;

			var litterals = Function.GetLiterals();
			foreach(Cpp.LiteralExpression le in litterals) {
				foreach(string e in Document.Settings.Enum.Members) {
					if(le.Expression == e) {
						old.Add(le, le.Expression);
						le.Expression = enumName + "::" + le.Expression;
					}
					else if(le.Expression == "$Name") {
						old.Add(le, le.Expression);
						le.Expression = "\"" + Name + "\"";
					}
					else if(le.Expression == "$ID") {
						old.Add(le, le.Expression);
						le.Expression = ID.ToString();
					}
				}
			}

			var cpp = Function.MakeCpp();

			var cppVar = new HashSet<Cpp.VariableExpression>();
			GetVariables(cppVar);

			if(cppVar.Count == 0) {
				source += this.CppName + "->setAction(make_callable([&petriNet]() { return " + cpp + "; }));";
			}
			else {
				var cppLockLock = from v in cppVar
								  select "_petri_lock_" + v.Expression;
				
				source += this.CppName + "->setAction(make_callable([&petriNet]() {";
				foreach(var v in cppVar) {
					source += "auto _petri_lock_" + v.Expression + " = petriNet.getVariable(static_cast<std::uint_fast32_t>(Petri_Var_Enum::" + v.Expression + ")).getLock();";
				}

				if(cppVar.Count > 1)
					source += "std::lock(" + String.Join(", ", cppLockLock) + ");";
				else {
					source += String.Join(", ", cppLockLock) + ".lock();";
				}
					
				source += "return " + cpp + ";";
				source += "}));";
			}
			source += this.CppName + "->setRequiredTokens(" + RequiredTokens.ToString() + ");";

			source += this.CppName + "->setName(\"" + this.Parent.Name + "_" + this.Name + "\");";
			source += this.CppName + "->setID(" + this.ID.ToString() + ");";
			source += "petriNet.addAction(" + this.CppName + ", " + ((this.Active && (this.Parent is RootPetriNet)) ? "true" : "false") + ");";

			foreach(var tup in old) {
				tup.Key.Expression = tup.Value;
			}

			return "";
		}

		public void GetVariables(HashSet<Cpp.VariableExpression> res) {				
			var l = Function.GetLiterals();
			foreach(var ll in l) {
				if(ll is Cpp.VariableExpression) {
					res.Add(ll as Cpp.VariableExpression);
				}
			}
		}

		Cpp.FunctionInvocation _function;
	}
}

