using System;

namespace Petri.Editor
{
    public class DebuggableHeadlessDocument : HeadlessDocument, Debuggable
    {
        public DebuggableHeadlessDocument(string path) : base(path)
        {
            DebugController = new CLIDebugController(this);
        }

        public DebugController BaseDebugController {
            get { return DebugController; }
        }

        public CLIDebugController DebugController {
            get;
            private set;
        }

        public void Debug() {
            DebugController.Debug();
        }
    }
}

