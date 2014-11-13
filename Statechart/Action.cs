using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace Statechart
{
	public sealed class Action : NonRootState
	{
		public enum ResultatAction {ActionReussie, ActionEchec};

		public Action (Document doc, StateChart parent, bool active, Cairo.PointD pos) : base(doc, parent, active, pos)
		{
			this.Position = pos;
			this.Radius = 20;

			this.Active = active;

			this.Function = this.DefaultAction();
		}

		public Action(Document doc, StateChart parent, XElement descriptor, IEnumerable<Cpp.Function> functions) : base(doc, parent, descriptor) {
			this.Function = Cpp.Expression.CreateFromString<Cpp.FunctionInvokation>(descriptor.Attribute("Function").Value, this, functions);
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

		public Cpp.FunctionInvokation DefaultAction() {
			var f = new Cpp.Function(new Cpp.Type("ResultatAction", Cpp.Scope.EmptyScope()), Cpp.Scope.EmptyScope(), "defaultAction");
			f.AddParam(new Cpp.Param(new Cpp.Type("Action *", Cpp.Scope.EmptyScope()), "action"));
			return new Statechart.Cpp.FunctionInvokation(f, new Statechart.Cpp.EntityExpression(this, "this")); 
		}

		public Cpp.FunctionInvokation Function {
			get {
				return function;
			}
			set {
				function = value;
				if(this.IsDefault())
					function = this.DefaultAction();
					
				Document.Controller.Modified = true;
			}
		}

		public override bool UsesHeader(string h) {
			return this.Function.Function.Header == h;
		}

		public bool IsDefault()
		{
			return this.Function.Function.Name == "defaultAction";
		}

		public override void GenerateCpp(Cpp.Generator source, IDManager lastID) {
			source += "auto " + this.CppName + " = std::make_shared<Action>();";
			source += this.CppName + "->setAction(" + this.Function.MakeCpp() + ");";
			source += this.CppName + "->setRequiredTokens(" + this.RequiredTokens.ToString() + ");";

			source += this.CppName + "->setName(\"" + this.Parent.Name + "_" + this.Name + "\");";
			source += this.CppName + "->setID(" + this.ID.ToString() + ");";
			source += "stateChart->addAction(" + this.CppName + ", " + ((this.Active && (this.Parent is RootStateChart)) ? "true" : "false") + ");";
		}

		Cpp.FunctionInvokation function;
	}
}

