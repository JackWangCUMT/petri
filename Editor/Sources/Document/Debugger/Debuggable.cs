using System;

namespace Petri.Editor.Debugger
{
    public interface Debuggable
    {
        DebugController BaseDebugController {
            get;
        }
    }
}

