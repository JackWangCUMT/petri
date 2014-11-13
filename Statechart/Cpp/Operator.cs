using System;
using System.Collections.Generic;

namespace Statechart
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
				Less,
				LessEqual,
				Greater,
				GreaterEqual,
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
				public Op(Associativity a, Type t, string c, int p, bool impl) {
					associativity = a;
					type = t;
					precedence = p;
					implemented = impl;
					cpp = c;
				}

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

			static Operator() {
				Properties = new Dictionary<Name, Op>();

				Properties[Name.ScopeResolution] = new Op(Associativity.LeftToRight, Type.Binary, "::", 1, true);
				Properties[Name.PostIncr] = new Op(Associativity.LeftToRight, Type.SuffixUnary, "++", 2, false);
				Properties[Name.PostDecr] = new Op(Associativity.LeftToRight, Type.SuffixUnary, "--", 2, false);
				Properties[Name.FunctionalCast] = new Op(Associativity.LeftToRight, Type.PrefixUnary, "", 2, false);
				Properties[Name.FunCall] = new Op(Associativity.LeftToRight, Type.SuffixUnary, "(", 2, true);
				Properties[Name.Subscript] = new Op(Associativity.LeftToRight, Type.SuffixUnary, "[", 2, false);
				Properties[Name.SelectionRef] = new Op(Associativity.LeftToRight, Type.Binary, ".", 2, true);
				Properties[Name.SelectionPtr] = new Op(Associativity.LeftToRight, Type.Binary, "->", 2, true);

				Properties[Name.PreIncr] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "++", 3, false);
				Properties[Name.PreDecr] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "--", 3, false);
				Properties[Name.UnaryPlus] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "+", 3, true);
				Properties[Name.UnaryMinus] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "-", 3, true);
				Properties[Name.LogicalNot] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "!", 3, true);
				Properties[Name.BitwiseNot] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "~", 3, false);
				Properties[Name.CCast] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "", 3, false);
				Properties[Name.Indirection] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "*", 3, false);
				Properties[Name.AddressOf] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "&", 3, false);
				Properties[Name.Sizeof] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "", 3, false);
				Properties[Name.New] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "", 3, false);
				Properties[Name.NewTab] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "", 3, false);
				Properties[Name.Delete] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "", 3, false);
				Properties[Name.DeleteTab] = new Op(Associativity.RightToLeft, Type.PrefixUnary, "", 3, false);

				Properties[Name.PtrToMemRef] = new Op(Associativity.LeftToRight, Type.Binary, ".*", 4, false);
				Properties[Name.PtrToMemPtr] = new Op(Associativity.LeftToRight, Type.Binary, "->*", 4, false);

				Properties[Name.Mult] = new Op(Associativity.LeftToRight, Type.Binary, "*", 5, true);
				Properties[Name.Div] = new Op(Associativity.LeftToRight, Type.Binary, "/", 5, true);
				Properties[Name.Mod] = new Op(Associativity.LeftToRight, Type.Binary, "%", 5, true);

				Properties[Name.Plus] = new Op(Associativity.LeftToRight, Type.Binary, "+", 6, true);
				Properties[Name.Minus] = new Op(Associativity.LeftToRight, Type.Binary, "-", 6, true);

				Properties[Name.ShiftLeft] = new Op(Associativity.LeftToRight, Type.Binary, "<<", 7, true);
				Properties[Name.ShiftRight] = new Op(Associativity.LeftToRight, Type.Binary, ">>", 7, true);

				Properties[Name.Less] = new Op(Associativity.LeftToRight, Type.Binary, "<", 8, true);
				Properties[Name.LessEqual] = new Op(Associativity.LeftToRight, Type.Binary, "<=", 8, true);
				Properties[Name.Greater] = new Op(Associativity.LeftToRight, Type.Binary, ">", 8, true);
				Properties[Name.GreaterEqual] = new Op(Associativity.LeftToRight, Type.Binary, ">=", 8, true);

				Properties[Name.Equal] = new Op(Associativity.LeftToRight, Type.Binary, "==", 9, true);
				Properties[Name.NotEqual] = new Op(Associativity.LeftToRight, Type.Binary, "!=", 9, true);

				Properties[Name.BitwiseAnd] = new Op(Associativity.LeftToRight, Type.Binary, "&", 10, false);
				Properties[Name.BitwiseXor] = new Op(Associativity.LeftToRight, Type.Binary, "^", 11, false);
				Properties[Name.BitwiseOr] = new Op(Associativity.LeftToRight, Type.Binary, "|", 12, false);
				Properties[Name.LogicalAnd] = new Op(Associativity.LeftToRight, Type.Binary, "&&", 13, false);
				Properties[Name.LogicalOr] = new Op(Associativity.LeftToRight, Type.Binary, "||", 14, false);

				Properties[Name.TernaryConditional] = new Op(Associativity.RightToLeft, Type.Binary, "?", 15, false);
				Properties[Name.Assignment] = new Op(Associativity.RightToLeft, Type.Binary, "=", 15, false);
				Properties[Name.PlusAssign] = new Op(Associativity.RightToLeft, Type.Binary, "+=", 15, false);
				Properties[Name.MinusAssign] = new Op(Associativity.RightToLeft, Type.Binary, "-=", 15, false);
				Properties[Name.MultAssign] = new Op(Associativity.RightToLeft, Type.Binary, "*=", 15, false);
				Properties[Name.DivAssign] = new Op(Associativity.RightToLeft, Type.Binary, "/=", 15, false);
				Properties[Name.ModAssign] = new Op(Associativity.RightToLeft, Type.Binary, "%=", 15, false);
				Properties[Name.ShiftLeftAssign] = new Op(Associativity.RightToLeft, Type.Binary, "<<=", 15, false);
				Properties[Name.ShiftRightAssign] = new Op(Associativity.RightToLeft, Type.Binary, ">>=", 15, false);
				Properties[Name.BitwiseAndAssig] = new Op(Associativity.RightToLeft, Type.Binary, "&=", 15, false);
				Properties[Name.BitwiseXorAssign] = new Op(Associativity.RightToLeft, Type.Binary, "^=", 15, false);
				Properties[Name.BitwiseOrAssign] = new Op(Associativity.RightToLeft, Type.Binary, "|=", 15, false);

				Properties[Name.Throw] = new Op(Associativity.LeftToRight, Type.PrefixUnary, "", 16, false);

				Properties[Name.Comma] = new Op(Associativity.LeftToRight, Type.Binary, "", 17, false);
			}
		}
	}
}

