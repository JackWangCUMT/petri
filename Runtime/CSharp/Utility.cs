using System;

namespace Petri.Runtime
{
    public enum ActionResultEnum
    {
        OK,
        NOK
    };

    public class Utility
    {
        public static Int32 Pause(double delay)
        {
            return Interop.PetriUtils.PetriUtility_pause((UInt64)(delay * 1.0e6));
        }

        public static Int32 PrintAction(string name, UInt64 id)
        {
            return Interop.PetriUtils.PetriUtility_printAction(name, id);
        }

        public static Int32 DoNothing()
        {
            return Interop.PetriUtils.PetriUtility_doNothing();
        }

        bool ReturnTrue(Int32 res)
        {
            return true;
        }
    }
}

