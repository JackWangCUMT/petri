using System;
using System.Collections.Generic;
using Regex = System.Text.RegularExpressions.Regex;

namespace Petri
{

	public abstract class ConditionBase : Cpp.FunctionInvocation
	{
		protected ConditionBase() : base(new Cpp.Function(new Cpp.Type("bool", Cpp.Scope.EmptyScope), Cpp.Scope.EmptyScope, "", false)) {

		}

		public abstract override bool UsesFunction(Cpp.Function f);

		public static ConditionBase ConditionFromString(string condition, Cpp.Enum resultEnum, Transition transition, IEnumerable<Cpp.Function> funcList, IDictionary<string, string> macros) {
			foreach(string res in resultEnum.Members) {
				if(condition == res) {
					return new CheckResultCondition(transition, resultEnum.Name, res);
				}
			}
			if(condition.StartsWith("Timeout(")) {
				return new TimeoutCondition(new Cpp.Duration(condition.Substring("Timeout(".Length, condition.Length - "Timeout(".Length - 1)));
			}
			return new ExpressionCondition(Cpp.Expression.CreateFromString<Cpp.Expression>(condition, null, funcList, macros));
		}
	}

	public class CheckResultCondition : ConditionBase
	{
		public CheckResultCondition(Transition t, string enumName, string res) : base() {
			this.Transition = t;
			this.ActionResult = res;
			EnumName = enumName;
		}

		Transition Transition {
			get;
			set;
		}

		string ActionResult {
			get;
			set;
		}

		string EnumName {
			get;
			set;
		}

		public override string MakeCpp() {
			return this.Transition.CppName + "->compareResult(" + EnumName + "::" + this.ActionResult + ")";
		}

		public override string MakeUserReadable() {
			return this.ActionResult;
		}

		public override bool UsesFunction(Cpp.Function f) {
			return false;
		}
	}

	public class TimeoutCondition : ConditionBase
	{
		public TimeoutCondition(Cpp.Duration d) : base() {
			this.Duration = d;
		}

		public override string MakeUserReadable() {
			return "Timeout(" + this.Duration.Value + ")";
		}

		public override string MakeCpp() {
			return "std::make_shared<" + this.GetType().Name + ">(" + this.Duration.Value + ")";
		}

		public Cpp.Duration Duration {
			get;
			set;
		}

		public override bool UsesFunction(Cpp.Function f) {
			return false;
		}
	}

	public class ExpressionCondition : ConditionBase
	{
		public ExpressionCondition(Cpp.Expression cond) : base() {
			Expression = cond;
		}

		public override bool UsesFunction(Cpp.Function f) {
			return Expression.UsesFunction(f);
		}

		public override string MakeUserReadable() {
			return Expression.MakeUserReadable();
		}

		public Cpp.Expression Expression {
			get;
			private set;
		}

		public override string MakeCpp() {
			return "std::make_shared<Condition>(make_callable_ptr([]() { return " + Expression.MakeCpp() + "->operator()(); }))";
		}
	}
}

