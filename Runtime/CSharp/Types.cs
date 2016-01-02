using System;

namespace Petri.Runtime
{
    public delegate Int32 ActionCallableDel();
    public delegate Int32 ParametrizedActionCallableDel(PetriNet petriNet);
    public delegate bool TransitionCallableDel(Int32 result);

    public class WrapForNative
    {
        public static ActionCallableDel Wrap(ActionCallableDel callable, string actionName)
        {
            return () => {
                try {
                    return callable();
                }
                catch(Exception e) {
                    Console.Error.WriteLine("The execution of the action {0} failed with the exception \"{1}\"",
                                            actionName,
                                            e.Message);
                    return default(Int32);
                }
            };
        }

        public static ParametrizedActionCallableDel Wrap(ParametrizedActionCallableDel callable, string actionName)
        {
            return (PetriNet pn) => {
                try {
                    return callable(pn);
                }
                catch(Exception e) {
                    Console.Error.WriteLine("The execution of the action {0} failed with the exception \"{1}\"",
                                            actionName,
                                            e.Message);
                    return default(Int32);
                }
            };
        }

        public static TransitionCallableDel Wrap(TransitionCallableDel callable, string transitionName)
        {
            return (Int32 result) => {
                try {
                    return callable(result);
                }
                catch(Exception e) {
                    Console.Error.WriteLine("The condition testing of the condition {0} failed with the exception \"{1}\"",
                                            transitionName,
                                            e.Message);
                    return default(bool);
                }
            };
        }
    }
}

