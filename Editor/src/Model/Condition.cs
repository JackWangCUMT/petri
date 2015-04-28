using System;
using System.Collections.Generic;
using Regex = System.Text.RegularExpressions.Regex;

namespace Petri
{

	public abstract class ConditionBase : Cpp.FunctionInvocation
	{
		protected ConditionBase(Transition t) : base(new Cpp.Function(new Cpp.Type("bool", Cpp.Scope.EmptyScope), Cpp.Scope.EmptyScope, "", false)) {
			_transition = t;
		}

		public abstract override bool UsesFunction(Cpp.Function f);

		public static ConditionBase ConditionFromString(string condition, Transition t, IEnumerable<Cpp.Function> funcList, IDictionary<string, string> macros) {
			if(condition.StartsWith("Timeout(")) {
				return new TimeoutCondition(new Cpp.Duration(condition.Substring("Timeout(".Length, condition.Length - "Timeout(".Length - 1)), t);
			}

			var exp = Cpp.Expression.CreateFromString<Cpp.Expression>(condition, null, funcList, macros);

			return new ExpressionCondition(exp, t);
		}

		protected Transition _transition;
	}

	public class TimeoutCondition : ConditionBase
	{
		public TimeoutCondition(Cpp.Duration d, Transition t) : base(t) {
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
		public ExpressionCondition(Cpp.Expression cond, Transition t) : base(t) {
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
			var old = new Dictionary<Cpp.LitteralExpression, string>();
			string enumName = _transition.Document.Settings.Enum.Name;

			var litterals = Expression.GetLitterals();
			foreach(Cpp.LitteralExpression le in litterals) {
				foreach(string e in _transition.Document.Settings.Enum.Members) {
					if(le.Expression == e) {
						old.Add(le, le.Expression);
						le.Expression = enumName + "::" + le.Expression;
					}
				}
			}

			string cpp = Expression is Cpp.LitteralExpression ? Expression.MakeCpp() : "(*" + Expression.MakeCpp() + ")()";

			string s =  "std::make_shared<Condition<" + enumName + ">>([](" + enumName + " _PETRI_PRIVATE_GET_ACTION_RESULT_) { return " + cpp + "; })";

			foreach(var tup in old) {
				tup.Key.Expression = tup.Value;
			}

			return s.Replace("$Res", "_PETRI_PRIVATE_GET_ACTION_RESULT_");
		}
	}
}

