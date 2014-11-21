using System;
using System.Collections.Generic;
using Regex = System.Text.RegularExpressions.Regex;

namespace Petri
{

	public abstract class ConditionBase : Cpp.FunctionInvocation
	{
		protected ConditionBase() : base(new Cpp.Function(new Cpp.Type("bool", Cpp.Scope.EmptyScope()), Cpp.Scope.EmptyScope(), "", false))
		{

		}

		public abstract bool UsesHeader(string h);

		public static ConditionBase ConditionFromString(string condition, Transition transition, List<Cpp.Function> funcList)
		{
			foreach(Action.ResultatAction res in Enum.GetValues(typeof(Action.ResultatAction))) {
				if(condition == res.ToString()) {
					return new CheckResultCondition(transition, res);
				}
			}
			if(condition.StartsWith("Timeout(")) {
				return new TimeoutCondition(new Cpp.Duration(condition.Substring("Timeout(".Length, condition.Length - "Timeout(".Length - 1)));
			}
			return new ExpressionCondition(Cpp.Expression.CreateFromString<Cpp.Expression>(condition, null, funcList));
		}
	}

	public class CheckResultCondition : ConditionBase
	{
		public CheckResultCondition(Transition t, Action.ResultatAction res) : base()
		{
			this.Transition = t;
			this.ResultatAction = res;
		}

		Transition Transition {
			get;
			set;
		}

		Action.ResultatAction ResultatAction {
			get;
			set;
		}

		public override string MakeCpp()
		{
			return "std::make_shared<CheckResultCondition>(" + this.Transition.CppName + "->mutableResult(), ResultatAction::" + this.ResultatAction.ToString() + ")";
		}

		public override string MakeUserReadable()
		{
			return this.ResultatAction.ToString();
		}

		public override bool UsesHeader(string h) {
			return false;
		}
	}

	public class TimeoutCondition : ConditionBase
	{
		public TimeoutCondition(Cpp.Duration d) : base()
		{
			this.Duration = d;
		}

		public override string MakeUserReadable()
		{
			return "Timeout(" + this.Duration.Value + ")";
		}

		public override string MakeCpp()
		{
			return "std::make_shared<" + this.GetType().Name + ">(" + this.Duration.Value + ")";
		}

		public Cpp.Duration Duration {
			get;
			set;
		}

		public override bool UsesHeader(string h) {
			return false;
		}
	}

	public class ExpressionCondition : ConditionBase
	{
		public ExpressionCondition(Cpp.Expression cond) : base()
		{
			expr = cond;
		}

		public override bool UsesHeader(string h) {
			return false;
		}

		public override string MakeUserReadable()
		{
			return expr.MakeUserReadable();
		}

		public override string MakeCpp()
		{
			return "std::make_shared<Condition>(make_callable_ptr([]() { return " + expr.MakeCpp() + "->operator()(); }))";
		}

		Cpp.Expression expr;
	}

}

