using System;
using System.Collections.Generic;
using System.Linq;

namespace Petri
{
	namespace Cpp {
		public static class Operator {
			public enum Name {
				ScopeResolution, // ::
				PostIncr, // ++
				PostDecr, // --
				FunctionalCast, //type(expr)
				FunCall, // ()
				Subscript, // []
				SelectionRef, // .
				SelectionPtr, // ->

				PreIncr, // ++
				PreDecr, // --
				UnaryPlus, // +
				UnaryMinus, // -
				LogicalNot, // !
				BitwiseNot, // ~
				CCast, // (type)expr
				Indirection, // *
				AddressOf, // &
				Sizeof, // sizeof
				New, // new
				NewTab, // new[]
				Delete, // delete
				DeleteTab, // delete[]

				PtrToMemRef, // .*
				PtrToMemPtr, // .->
				Mult,
				Div,
				Mod,
				Plus,
				Minus,
				ShiftLeft, // <<
				ShiftRight, // >>
				LessEqual,
				Less,
				GreaterEqual,
				Greater,
				Equal,
				NotEqual,
				BitwiseAnd, // &
				BitwiseXor, // ^
				BitwiseOr, // |
				LogicalAnd, // &&
				LogicalOr, // ||

				TernaryConditional, // ?:
				Assignment, // =
				PlusAssign, // +=
				MinusAssign, // -=
				MultAssign, // *=
				DivAssign, // /=
				ModAssign, // %=
				ShiftLeftAssign, // <<=
				ShiftRightAssign, // >>=
				BitwiseAndAssig, // &=
				BitwiseXorAssign, // ^=
				BitwiseOrAssign, // |=
				Throw, // throw
				Comma, // ,
				None
			}

			public enum Associativity {LeftToRight, RightToLeft};

			public enum Type {
				Binary,
				PrefixUnary,
				SuffixUnary,
				Ternary
			}

			public struct Op {
				public Op(Associativity a, Type t, string c, int p, bool impl, bool ambig) {
					associativity = a;
					type = t;
					precedence = p;
					implemented = impl;
					cpp = c;
					ambiguous = ambig;
				}

				public bool ambiguous;
				public string cpp;
				public bool implemented;
				public Associativity associativity;
				public Type type;
				public int precedence;
			}

			public static Dictionary<Name, Op> Properties {
				get;
				private set;
			}

			public static Dictionary<int, List<Name>> ByPrecedence {
				get;
				private set;
			}

			static Operator() {
				Properties = new Dictionary<Name, Op>();

				Properties[Name.ScopeResolution] = new Op(Associativity.LeftToRight, Type.Binary, "::", 0, false, false);

				Properties[Name.PostIncr] = new Op(Associativity.LeftToRight, Type.SuffixUnary, "++", 1, true, false);
				Properties[Name.PostDecr] = new Op(Associativity.LeftToRight, Type.SuffixUnary, "--", 1, true, false);
				Properties[Name.FunctionalCast] = new Op(Associativity.LeftToRight, Type.PrefixUnary, "", 1, false, false);
				Properties[Name.FunCall] = new Op(Associativity.LeftToRight, Type.SuffixUnary, "()", 1, true, false);
				Properties[Name.Subscript] = new Op(Associativity.LeftToRight, Type.SuffixUnary, "", 1, false, false);

				// Actual precedence is the same as function call for . and ->, but we cheat for f().X being parsed
				Properties[Name.SelectionRef] = new Op(Associativity.LeftToRight, Type.Binary, ".", 2, true, false);
				Properties[Name.SelectionPtr] = new Op(Associativity.LeftToRight, Type.Binary, "->", 2, true, false);

				Properties[Name.PreIncr] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "++", 3, true, false);
				Properties[Name.PreDecr] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "--", 3, true, false);
				Properties[Name.UnaryPlus] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "+", 3, true, true);
				Properties[Name.UnaryMinus] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "-", 3, true, true);
				Properties[Name.LogicalNot] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "!", 3, true, false);
				Properties[Name.BitwiseNot] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "~", 3, true, false);
				Properties[Name.CCast] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "", 3, false, false);
				Properties[Name.Indirection] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "*", 3, false, false);
				Properties[Name.AddressOf] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "&", 3, false, false);
				Properties[Name.Sizeof] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "", 3, false, false);
				Properties[Name.New] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "", 3, false, false);
				Properties[Name.NewTab] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "", 3, false, false);
				Properties[Name.Delete] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "", 3, false, false);
				Properties[Name.DeleteTab] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "", 3, false, false);

				Properties[Name.PtrToMemRef] = new Op(Associativity.LeftToRight, Type.Binary, ".*", 4, false, false);
				Properties[Name.PtrToMemPtr] = new Op(Associativity.LeftToRight, Type.Binary, "->*", 4, false, false);

				Properties[Name.Mult] = new Op(Associativity.LeftToRight, Type.Binary, "*", 5, true, false);
				Properties[Name.Div] = new Op(Associativity.LeftToRight, Type.Binary, "/", 5, true, false);
				Properties[Name.Mod] = new Op(Associativity.LeftToRight, Type.Binary, "%", 5, true, false);

				Properties[Name.Plus] = new Op(Associativity.LeftToRight, Type.Binary, "+", 6, true, true);
				Properties[Name.Minus] = new Op(Associativity.LeftToRight, Type.Binary, "-", 6, true, true);

				Properties[Name.ShiftLeft] = new Op(Associativity.LeftToRight, Type.Binary, "<<", 7, true, false);
				Properties[Name.ShiftRight] = new Op(Associativity.LeftToRight, Type.Binary, ">>", 7, true, false);

				Properties[Name.LessEqual] = new Op(Associativity.LeftToRight, Type.Binary, "<=", 8, true, false);
				Properties[Name.Less] = new Op(Associativity.LeftToRight, Type.Binary, "<", 8, true, false);
				Properties[Name.GreaterEqual] = new Op(Associativity.LeftToRight, Type.Binary, ">=", 8, true, false);
				Properties[Name.Greater] = new Op(Associativity.LeftToRight, Type.Binary, ">", 8, true, false);

				Properties[Name.Equal] = new Op(Associativity.LeftToRight, Type.Binary, "==", 9, true, false);
				Properties[Name.NotEqual] = new Op(Associativity.LeftToRight, Type.Binary, "!=", 9, true, false);

				Properties[Name.BitwiseAnd] = new Op(Associativity.LeftToRight, Type.Binary, "&", 10, false, false);
				Properties[Name.BitwiseXor] = new Op(Associativity.LeftToRight, Type.Binary, "^", 11, false, false);
				Properties[Name.BitwiseOr] = new Op(Associativity.LeftToRight, Type.Binary, "|", 12, false, false);
				Properties[Name.LogicalAnd] = new Op(Associativity.LeftToRight, Type.Binary, "&&", 13, true, false);
				Properties[Name.LogicalOr] = new Op(Associativity.LeftToRight, Type.Binary, "||", 14, true, false);

				Properties[Name.TernaryConditional] = new Op(Associativity.RightToLeft, Type.Ternary, "?", 15, false, false);
				Properties[Name.Assignment] = new Op(Associativity.RightToLeft, Type.Binary, "=", 15, false, false);
				Properties[Name.PlusAssign] = new Op(Associativity.RightToLeft, Type.Binary, "+=", 15, false, false);
				Properties[Name.MinusAssign] = new Op(Associativity.RightToLeft, Type.Binary, "-=", 15, false, false);
				Properties[Name.MultAssign] = new Op(Associativity.RightToLeft, Type.Binary, "*=", 15, false, false);
				Properties[Name.DivAssign] = new Op(Associativity.RightToLeft, Type.Binary, "/=", 15, false, false);
				Properties[Name.ModAssign] = new Op(Associativity.RightToLeft, Type.Binary, "%=", 15, false, false);
				Properties[Name.ShiftLeftAssign] = new Op(Associativity.RightToLeft, Type.Binary, "<<=", 15, false, false);
				Properties[Name.ShiftRightAssign] = new Op(Associativity.RightToLeft, Type.Binary, ">>=", 15, false, false);
				Properties[Name.BitwiseAndAssig] = new Op(Associativity.RightToLeft, Type.Binary, "&=", 15, false, false);
				Properties[Name.BitwiseXorAssign] = new Op(Associativity.RightToLeft, Type.Binary, "^=", 15, false, false);
				Properties[Name.BitwiseOrAssign] = new Op(Associativity.RightToLeft, Type.Binary, "|=", 15, false, false);

				Properties[Name.Throw] = new Op(Associativity.LeftToRight, Type.PrefixUnary, "", 16, false, false);

				Properties[Name.Comma] = new Op(Associativity.LeftToRight, Type.Binary, "", 17, false, false);

				ByPrecedence = new Dictionary<int, List<Name>>();
				for(int i = 0; i <= 17; ++i) {
					ByPrecedence[i] = new List<Name>();
					ByPrecedence[i].AddRange(from prop in Properties
						where prop.Value.precedence == i
						select prop.Key);
				}
			}
		}
	}
}

