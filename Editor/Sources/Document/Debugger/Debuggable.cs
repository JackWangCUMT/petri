using System;

namespace Petri.Editor
{
    public interface Debuggable
    {
        DebugController BaseDebugController {
            get;
        }
    }
}

