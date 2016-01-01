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
        public static ActionResult_t Pause(double delay)
        {
            return new ActionResult_t(Interop.PetriUtils.PetriUtility_pause((UInt64)(delay * 1.0e6)));
        }

        public static ActionResult_t PrintAction(string name, UInt64 id)
        {
            return new ActionResult_t(Interop.PetriUtils.PetriUtility_printAction(name, id));
        }

        public static ActionResult_t DoNothing()
        {
            return new ActionResult_t(Interop.PetriUtils.PetriUtility_doNothing());
        }

        bool ReturnTrue(ActionResult_t res)
        {
            return true;
        }
    }
}

