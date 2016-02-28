using System;

namespace Petri.Editor.CLI
{
    public class DebuggableHeadlessDocument : HeadlessDocument, Petri.Editor.Debugger.Debuggable
    {
        public DebuggableHeadlessDocument(string path) : base(path)
        {
            DebugController = new Debugger.DebugController(this);
        }

        public Petri.Editor.Debugger.DebugController BaseDebugController {
            get { return DebugController; }
        }

        public Debugger.DebugController DebugController {
            get;
            private set;
        }

        public int Debug() {
            return DebugController.Debug();
        }
    }
}

