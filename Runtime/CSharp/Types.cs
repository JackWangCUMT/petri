using System;

namespace Petri.Runtime
{
    public delegate Int32 ActionCallableDel();
    public delegate Int32 ParametrizedActionCallableDel(PetriNet petriNet);
    public delegate bool TransitionCallableDel(Int32 result);

    public struct ActionResult_t
    {
        public Int32 _value;

        public ActionResult_t(Int32 v)
        {
            _value = v;
        }

        public static implicit operator Int32(ActionResult_t result)
        {
            return result._value;
        }
    }

    public interface ManagedCallback
    {

    }

    public struct ActionCallable : ManagedCallback
    {
        public ActionCallableDel _value;

        public ActionCallable(ActionCallableDel v)
        {
            _value = v;
        }

        public static implicit operator ActionCallableDel(ActionCallable result)
        {
            return result._value;
        }
    }

    public struct ParametrizedActionCallable : ManagedCallback
    {
        public ParametrizedActionCallableDel _value;

        public ParametrizedActionCallable(ParametrizedActionCallableDel v)
        {
            _value = v;
        }

        public static implicit operator ParametrizedActionCallableDel(ParametrizedActionCallable result)
        {
            return result._value;
        }
    }

    public struct TransitionCallable : ManagedCallback
    {
        public TransitionCallableDel _value;

        public TransitionCallable(TransitionCallableDel v)
        {
            _value = v;
        }

        public static implicit operator TransitionCallableDel(TransitionCallable result)
        {
            return result._value;
        }
    }

    public class WrapForNative
    {
        public static ActionCallable Wrap(ActionCallable callable, string actionName)
        {
            return new ActionCallable(() => {
                try {
                    return callable._value();
                }
                catch(Exception e) {
                    Console.Error.WriteLine("The execution of the action {0} failed with the exception \"{1}\"",
                                            actionName,
                                            e.Message);
                    return new ActionResult_t(default(Int32));
                }
            });
        }

        public static ParametrizedActionCallable Wrap(ParametrizedActionCallable callable, string actionName)
        {
            return new ParametrizedActionCallable((PetriNet pn) => {
                try {
                    return callable._value(pn);
                }
                catch(Exception e) {
                    Console.Error.WriteLine("The execution of the action {0} failed with the exception \"{1}\"",
                                            actionName,
                                            e.Message);
                    return new ActionResult_t(default(Int32));
                }
            });
        }

        public static TransitionCallable Wrap(TransitionCallable callable, string transitionName)
        {
            return new TransitionCallable((Int32 result) => {
                try {
                    return callable._value(result);
                }
                catch(Exception e) {
                    Console.Error.WriteLine("The condition testing of the condition {0} failed with the exception \"{1}\"",
                                            transitionName,
                                            e.Message);
                    return default(bool);
                }
            });
        }
    }
}

