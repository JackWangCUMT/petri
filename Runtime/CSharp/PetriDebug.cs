using System;

namespace Petri.Runtime
{
    public class PetriDebug : PetriNet
    {
        public PetriDebug(string name)
        {
            Handle = Interop.PetriNet.PetriNet_createDebug(name);
        }

        public override void Stop()
        {
            base.Stop();
        }
    }
}

