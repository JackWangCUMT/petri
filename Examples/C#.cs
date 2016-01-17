using System;
using Petri.Runtime;
using PNAction = Petri.Runtime.Action;



namespace Petri.Generated
{
	public class CSharp
	{
		public static DynamicLib Lib {
			get;
			private set;
		}
		static void Populate(PetriNet petriNet) {
			var state_1 = new PNAction(1, "Root_1", state_1_invocation, 0);
			petriNet.AddAction(state_1, false);
			var state_2 = new PNAction(2, "Root_2", state_2_invocation, 1);
			petriNet.AddAction(state_2, false);
			var state_3 = new PNAction(3, "Root_3", state_3_invocation, 1);
			petriNet.AddAction(state_3, false);
			
			
			var transition_4 = state_1.AddTransition(4, "4", state_2, transition_4_invocation);
			var transition_5 = state_2.AddTransition(5, "5", state_3, transition_5_invocation);
		}
		static Int32 state_1_invocation(PetriNet petriNet) {
			return (Int32)(TestNS.Action1());
		}
		static Int32 state_2_invocation(PetriNet petriNet) {
			return (Int32)(Petri.Runtime.Utility.DoNothing());
		}
		static Int32 state_3_invocation(PetriNet petriNet) {
			return (Int32)(Petri.Runtime.Utility.DoNothing());
		}
		static bool transition_4_invocation(Int32 _PETRI_PRIVATE_GET_ACTION_RESULT_) {
			return true;
		}
		static bool transition_5_invocation(Int32 _PETRI_PRIVATE_GET_ACTION_RESULT_) {
			return true;
		}
		
		
		static IntPtr Create() {
			var petriNet = new PetriNet("C#");
			Populate(petriNet);
			return petriNet.Release();
		}
		
		static IntPtr CreateDebug() {
			var petriNet = new PetriDebug("C#");
			Populate(petriNet);
			return petriNet.Release();
		}
		
		static string Hash() {
			return "DCD92DB4D0D82EACF35A7D521873D75B45D24908";
		}
		
		static string Prefix() {
			return "C#";
		}
		
		static string Name() {
			return "C#";
		}
		
		static UInt16 Port() {
			return 12345;
		}
		
	}
}

