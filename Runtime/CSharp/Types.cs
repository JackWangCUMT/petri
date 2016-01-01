using System;

namespace Petri.Runtime
{
    public delegate ActionResult_t ActionCallableDel();
    public delegate ActionResult_t ParametrizedActionCallableDel(PetriNet petriNet);
    public delegate bool TransitionCallableDel(ActionResult_t result);

    public struct ActionResult_t
    {
        Int32 _value;

        public ActionResult_t(Int32 v)
        {
            _value = v;
        }

        public static implicit operator Int32(ActionResult_t result)
        {
            return result._value;
        }
    }

    public struct ActionCallable
    {
        ActionCallableDel _value;

        public ActionCallable(ActionCallableDel v)
        {
            _value = v;
        }

        public static implicit operator ActionCallableDel(ActionCallable result)
        {
            return result._value;
        }
    }

    public struct ParametrizedActionCallable
    {
        ParametrizedActionCallableDel _value;

        public ParametrizedActionCallable(ParametrizedActionCallableDel v)
        {
            _value = v;
        }

        public static implicit operator ParametrizedActionCallableDel(ParametrizedActionCallable result)
        {
            return result._value;
        }
    }

    public struct TransitionCallable
    {
        TransitionCallableDel _value;

        public TransitionCallable(TransitionCallableDel v)
        {
            _value = v;
        }

        public static implicit operator TransitionCallableDel(TransitionCallable result)
        {
            return result._value;
        }
    }
}

