using System;
using ActionResult = Petri.Runtime.ActionResult;

class TestNS {
	public static ActionResult Action1() {
		Console.WriteLine("Action1!");
		return 0;
	}

        public static ActionResult Action2() {
                Console.WriteLine("Action2!");
                return 0;
        }

        public static bool Condition1(Int32 result) {
                Console.WriteLine("Condition1!");
                return true;
        }
}
		

