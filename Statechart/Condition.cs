using System;
using System.Collections.Generic;
using Regex = System.Text.RegularExpressions.Regex;

namespace Statechart
{

	public enum LogicalOperator
	{
		None,
		Not,
		And,
		Or
	}

	public abstract class ConditionBase : Cpp.FunctionInvokation
	{
		protected ConditionBase() : base(new Cpp.Function(new Cpp.Type("bool", Cpp.Scope.EmptyScope()), Cpp.Scope.EmptyScope(), ""))
		{

		}

		public abstract bool UsesHeader(string h);

		public static ConditionBase ConditionFromString(string condition, Transition transition, List<Cpp.Function> funcList)
		{
			condition = condition.Replace(" ", string.Empty);
			condition = condition.Replace("\t ", string.Empty);
			if(condition.Contains("?"))
				throw new Exception("Unexpected token");

			/*foreach(var func in funcList) {
				if(condition.StartsWith(func.Name)) {
					// Create invokation
				}
			}*/

			int parenCount = 0;
			int index = 0;
			var subConditions = new List<ConditionBase>();
			string newCondition = "";
			bool isInvokation = false;
			for(int i = 0; i < condition.Length; ++i) {
				if(condition[i] == '(') {
					if(parenCount == 0) {
						isInvokation = false;
						foreach(var f in funcList) {
							if(i >= f.Name.Length) {
								if(condition.Substring(i - f.Name.Length, f.Name.Length) == f.Name) {
									isInvokation = true;
									newCondition += "(";
									break;
								}
							}
						}
					}
					if(!isInvokation) {
						if(parenCount == 0) {
							index = i;
						}
						++parenCount;
					}
				}
				else if(condition[i] == ')') {
					if(isInvokation) {
						isInvokation = false;
						newCondition += ")";
					}
					else {
						--parenCount;
						if(parenCount < 0) {
							break;
						}
						else if(parenCount == 0) {
							string str = condition.Substring(index + 1, i - index - 1);
							var subCond = ConditionBase.ConditionFromString(str, transition, funcList);
							newCondition += "(?" + subConditions.Count.ToString() + ")";
							subConditions.Add(subCond);
						}
					}
				}
				else if(parenCount == 0) {
					newCondition += condition[i];
				}
			}
			if(parenCount < 0) {
				throw new Exception("Extraneous closing parenthesis");
			}
			if(parenCount > 0) {
				throw new Exception("Missing closing parenthesis");
			}

			condition = newCondition;

			ConditionBase andCondition = null;

			var andOperands = condition.Split(new string[] { "&&" }, StringSplitOptions.None);

			for(int i = andOperands.Length - 1; i >= 0; --i) {
				ConditionBase orCondition = null;

				var orOperands = andOperands[i].Split(new string[] { "||" }, StringSplitOptions.None);

				for(int j = orOperands.Length - 1; j >= 0; --j) {
					var unary = ConditionBase.MakeUnary(orOperands[j], transition, funcList, subConditions);
					if(orCondition == null) {
						orCondition = unary;
					}
					else {
						orCondition = new OrCondition(unary, orCondition);
					}
				}

				if(andCondition == null) {
					andCondition = orCondition;
				}
				else {
					andCondition = new AndCondition(orCondition, andCondition);
				}
			}
				
			return andCondition;
		}

		private static ConditionBase MakeUnary(string s, Transition t, List<Cpp.Function> funcList, List<ConditionBase> nested)
		{
			if(s.Length > 0) {
				if(s[0] == '!') {
					return new NotCondition(ConditionBase.MakeUnary(s.Substring(1), t, funcList, nested));
				}
				else if(s.StartsWith("Timeout(")) {
					return new TimeoutCondition(new Cpp.Duration(s.Substring("Timeout(".Length, s.Length - "Timeout(".Length - 1)));
				}

				foreach(Action.ResultatAction res in Enum.GetValues(typeof(Action.ResultatAction))) {
					if(s == res.ToString()) {
						return new CheckResultCondition(t, res);
					}
				}

				int index = s.IndexOf("(?");
				if(index != -1) {
					int last = s.IndexOf(")");
					var nestedIndex = int.Parse(s.Substring(index + 2, last - index - 2));
					return nested[nestedIndex];
				}

				return new Condition(Cpp.Expression.CreateFromString<Cpp.FunctionInvokation>(s, t, funcList));
			}

			throw new Exception("Empty unary operand");
		}

		protected static string Parenthesize(Cpp.FunctionInvokation parent, Cpp.FunctionInvokation child)
		{
			if(ConditionBase.HighestPrecedence(parent.Operator2(), child.Operator2()) == parent.Operator2() && (parent.Operator2() != child.Operator2())) {
				return "(" + child.MakeUserReadable() + ")";
			}

			return child.MakeUserReadable();
		}

		protected static LogicalOperator HighestPrecedence(LogicalOperator op1, LogicalOperator op2)
		{
			return op1 <= op2 ? op1 : op2;
		}

		protected static LogicalOperator LowestPrecedence(LogicalOperator op1, LogicalOperator op2)
		{
			return op1 > op2 ? op1 : op2;
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

	public abstract class UnaryCondition : ConditionBase
	{
		protected UnaryCondition(Cpp.FunctionInvokation f) : base()
		{
			if(!f.Function.ReturnType.Equals(new Cpp.Type("bool", Cpp.Scope.EmptyScope())))
				throw new Exception("Type de retour de la fonction incorrect : bool attendu, " + f.Function.ReturnType.ToString() + " trouvé.");
			this.Invokation = f;
		}

		public Cpp.FunctionInvokation Invokation {
			get;
			set;
		}

		public override string MakeCpp()
		{
			return "std::make_shared<" + this.GetType().Name + ">(" + this.Invokation.MakeCpp() + ")";
		}

		public override bool UsesHeader(string h) {
			return this.Invokation.Function.Header == h;
		}
	}

	public abstract class BinaryCondition : ConditionBase
	{
		protected BinaryCondition(Cpp.FunctionInvokation f1, Cpp.FunctionInvokation f2) : base()
		{
			this.Invokation1 = f1;
			this.Invokation2 = f2;
		}

		public Cpp.FunctionInvokation Invokation1 {
			get;
			set;
		}

		public Cpp.FunctionInvokation Invokation2 {
			get;
			set;
		}

		public override string MakeCpp()
		{
			return "std::make_shared<" + this.GetType().Name + ">(" + this.Invokation1.MakeCpp() + ", " + this.Invokation2.MakeCpp() + ")";
		}

		public override bool UsesHeader(string h) {
			return this.Invokation1.Function.Header == h || this.Invokation2.Function.Header == h;
		}
	}

	public class Condition : UnaryCondition
	{
		public Condition(Cpp.FunctionInvokation f) : base(f)
		{
		}

		public override string MakeUserReadable()
		{
			return this.Invokation.MakeUserReadable();
		}
	}

	public class NotCondition : UnaryCondition
	{
		public NotCondition(Cpp.FunctionInvokation f) : base(f)
		{
		}

		public override string MakeUserReadable()
		{
			return "!" + ConditionBase.Parenthesize(this, this.Invokation);
		}

		public override LogicalOperator Operator2()
		{
			return LogicalOperator.Not;
		}
	}

	public class AndCondition : BinaryCondition
	{
		public AndCondition(Cpp.FunctionInvokation f1, Cpp.FunctionInvokation f2) : base(f1, f2)
		{
		}

		public override string MakeUserReadable()
		{
			return ConditionBase.Parenthesize(this, this.Invokation1) + " && " + ConditionBase.Parenthesize(this, this.Invokation2);
		}

		public override LogicalOperator Operator2()
		{
			return LogicalOperator.And;
		}
	}

	public class OrCondition : BinaryCondition
	{
		public OrCondition(Cpp.FunctionInvokation f1, Cpp.FunctionInvokation f2) : base(f1, f2)
		{
		}

		public override string MakeUserReadable()
		{
			return ConditionBase.Parenthesize(this, this.Invokation1) + " || " + ConditionBase.Parenthesize(this, this.Invokation2);
		}

		public override LogicalOperator Operator2()
		{
			return LogicalOperator.Or;
		}
	}
}

