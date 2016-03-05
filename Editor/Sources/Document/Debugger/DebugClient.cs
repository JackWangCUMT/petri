/*
 * Copyright (c) 2015 Rémi Saurel
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Petri.Editor.Debugger
{
    public abstract class DebugClient
    {
        protected abstract void NotifyStateChanged();

        protected abstract void NotifyUnableToLoadDylib();

        protected abstract void NotifyStatusMessage(string message);

        protected abstract void NotifyEvaluated(string value);

        protected abstract void NotifyServerError(string message);

        protected abstract void NotifyActiveStatesChanged();

        /// <summary>
        /// Initializes a new instance of the <see cref="Petri.Editor.DebugClient"/> class.
        /// </summary>
        /// <param name="doc">Document.</param>
        public DebugClient(HeadlessDocument doc, Debuggable debuggable)
        {
            _debuggable = debuggable;
            _document = doc;
            _sessionState = SessionState.Stopped;
            _petriRunning = false;
            _pause = false;
        }

        ~DebugClient()
        {
            if(_petriRunning || CurrentSessionState != SessionState.Stopped) {
                throw new Exception(Configuration.GetLocalized("Debugger still running!"));
            }
        }

        /// <summary>
        /// The state of a debugger session.
        /// </summary>
        public enum SessionState
        {
            Starting,
            Started,
            Stopped
        }

        /// <summary>
        /// Gets the current state of the debugger session.
        /// </summary>
        /// <value>The state of the current session.</value>
        public SessionState CurrentSessionState {
            get {
                return _sessionState;
            }
            private set {
                _sessionState = value;
                NotifyStateChanged();
            }
        }

        public enum PetriState
        {
            Starting,
            Started,
            Pausing,
            Paused,
            Resuming,
            Stopping,
            Stopped
        }

        /// <summary>
        /// Gets a value indicating whether the PetriNet enclosed in the DebugServer's dynamic library is running or not.
        /// </summary>
        /// <value><c>true</c> if the petri net running; otherwise, <c>false</c>.</value>
        /*public PetriState CurrentPetriState {
            get {
                return _petriRunning;
            }
        }*/

        public bool PetriRunning {
            get {
                return _petriRunning;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Petri.Editor.DebugClient"/> is paused.
        /// The pause will be effective just after the states of the petri net that are active when the message is received have finished their execution.
        /// </summary>
        /// <value><c>true</c> if pause; otherwise, <c>false</c>.</value>
        public bool Pause {
            get {
                return _pause;
            }
            set {
                if(PetriRunning) {
                    try {
                        if(value) {
                            this.SendObject(new JObject(new JProperty("type", "pause")));
                        }
                        else {
                            this.SendObject(new JObject(new JProperty("type", "resume")));
                        }
                    }
                    catch(Exception e) {
                        NotifyUnrecoverableError(Configuration.GetLocalized("An error occurred in the debugger when pausing the petri net:") + " " + e.Message);
                        this.Detach();
                    }
                }
                else {
                    _pause = false;
                    NotifyStateChanged();
                }
            }
        }

        /// <summary>
        /// Gets the version of the debugger.
        /// </summary>
        /// <value>The version.</value>
        public static string Version {
            get {
                return "1.3.4";
            }
        }

        /// <summary>
        /// Attach this instance to a DebugServer listening on the same port as in the Settings of the document.
        /// </summary>
        public void Attach()
        {
            bool success = true;
            if(_document.Settings.RunInEditor) {
                success = LoadLibAndStartServer();
            }

            if(success) {
                CurrentSessionState = SessionState.Starting;
                _receiverThread = new Thread(this.Receiver);
                _pause = false;
                _receiverThread.Start();
                DateTime time = DateTime.Now.AddSeconds(1);
                while(_socket == null && DateTime.Now < time) {
                    System.Threading.Thread.Sleep(20);
                }
            }
        }

        /// <summary>
        /// Disconnect this instance from its DebugServer.
        /// </summary>
        public void Detach()
        {
            if(_document.Settings.RunInEditor) {
                // If the petri net is running in the editor, we have to stop it upon detach.
                this.StopOrDetach(true);
                UnloadLibAndStopServer();
            }
            else {
                this.StopOrDetach(false);
            }
        }

        /// <summary>
        /// Loads the library and starts a local DebugServer.
        /// Valid only when the document is set to Run in the editor.
        /// </summary>
        bool LoadLibAndStartServer()
        {
            UnloadLibAndStopServer();

            _libProxy = new GeneratedDynamicLibProxy(_document.Settings.Language, System.IO.Directory.GetParent(_document.Path).FullName,
                                                     _document.Settings.RelativeLibPath,
                                                     _document.Settings.Name);
            try {
                var dylib = _libProxy.Load<Petri.Runtime.GeneratedDynamicLib>();

                if(dylib == null) {
                    NotifyUnableToLoadDylib();
                    return false;
                }

                _dynamicLib = dylib.Lib;
                _debugServer = new Runtime.DebugServer(_dynamicLib);
                _debugServer.Start();
            }
            catch(Exception e) {
                UnloadLibAndStopServer();
                NotifyUnrecoverableError(Configuration.GetLocalized("An error occurred in the debugger when loading the lib:") + " " + e.Message);

                return false;
            }

            return true;
        }

        /// <summary>
        /// Stops the local DebugServer and unloads the assembly containing the PetriNet's code.
        /// </summary>
        void UnloadLibAndStopServer()
        {
            if(_debugServer != null) {
                _debugServer.Stop();
                _debugServer = null;
            }
            _dynamicLib = null;
            if(_libProxy != null) {
                _libProxy.Unload();
                _libProxy = null;
            }
        }

        /// <summary>
        /// Stops the petri net, detach the debugger and stops the DebugServer (it will not be listening any more after that).
        /// </summary>
        public void StopSession()
        {
            this.StopOrDetach(true);
        }

        /// <summary>
        /// Stops or detach the instance, depending on the parameter's value.
        /// </summary>
        /// <param name="stop">If set to <c>true</c> then the DebugServer is stopped as well as the DebugClient and petri net, whereas only the DebugClient and petri net are stopped if <c>false</c>.</param>
        void StopOrDetach(bool stop)
        {
            _pause = false;
            if(CurrentSessionState == SessionState.Started) {
                if(PetriRunning) {
                    if(Pause) {
                        this.Pause = false;
                    }
                    StopPetri();
                    _petriRunning = false;
                }

                try {
                    if(stop) {
                        this.SendObject(new JObject(new JProperty("type", "detachAndExit")));
                    }
                    else {
                        this.SendObject(new JObject(new JProperty("type", "detach")));
                    }
                }
                catch(Exception) {
                }

                if(_receiverThread != null && !_receiverThread.Equals(Thread.CurrentThread)) {
                    _receiverThread.Join();
                }
                CurrentSessionState = SessionState.Stopped;
            }

            lock(_debuggable.BaseDebugController.ActiveStates) {
                _debuggable.BaseDebugController.ActiveStates.Clear();
            }
        }

        /// <summary>
        /// Tells the DebugServer to run the petri net.
        /// </summary>
        public void StartPetri()
        {
            _pause = false;
            try {
                if(!_petriRunning) {
                    this.SendObject(new JObject(new JProperty("type", "start"),
                                                new JProperty("payload",
                                                              new JObject(new JProperty("hash",
                                                                                        _document.Hash)))));
                }
            }
            catch(Exception e) {
                NotifyUnrecoverableError(Configuration.GetLocalized("An error occurred in the debugger when starting the petri net:") + " " + e.Message);
                this.Detach();
            }
        }

        /// <summary>
        /// Tells the DebugServer to stop the execution of the petri net.
        /// </summary>
        public void StopPetri()
        {
            _pause = false;
            try {
                if(_petriRunning) {
                    this.SendObject(new JObject(new JProperty("type", "stop")));
                }
            }
            catch(Exception e) {
                NotifyUnrecoverableError(Configuration.GetLocalized("An error occurred in the debugger when stopping the petri net:") + " " + e.Message);
                this.Detach();
            }
        }

        /// <summary>
        /// Stops the petri net execution, generate and compile the new petri net and load it into the DebugServer.
        /// </summary>
        public void ReloadPetri(bool startAfterReload = false)
        {
            NotifyStatusMessage(Configuration.GetLocalized("Reloading the petri net…"));
            this.StopPetri();
            if(_document.Compile(true)) {
                try {
                    if(_document.Settings.RunInEditor && _document.Settings.Language == Code.Language.CSharp) {
                        Detach();
                        Attach();
                        if(startAfterReload) {
                            StartPetri();
                        }
                    }
                    else {
                        _startAfterFix = startAfterReload;
                        this.SendObject(new JObject(new JProperty("type", "reload")));
                    }
                }
                catch(Exception e) {
                    NotifyUnrecoverableError(Configuration.GetLocalized("An error occurred in the debugger when reloading the petri net:") + " " + e.Message);
                    this.Detach();
                }
            }
        }

        /// <summary>
        /// Sends the current breakpoints list to the DebugServer.
        /// </summary>
        public void UpdateBreakpoints()
        {
            if(PetriRunning) {
                var breakpoints = new JArray();
                foreach(var p in _debuggable.BaseDebugController.Breakpoints) {
                    breakpoints.Add(new JValue(p.ID));
                }
                this.SendObject(new JObject(new JProperty("type", "breakpoints"),
                                            new JProperty("payload",
                                                          breakpoints)));
            }
        }

        /// <summary>
        /// Triggers an asynchronous evaluation of a code expression.
        /// </summary>
        /// <param name="expression">The expression to evaluate.</param>
        /// <param name="userData">Additional and optional user data that will be used to generate the code.</param>
        public void Evaluate(Code.Expression expression, params object[] userData)
        {
            if(!PetriRunning) {
                var literals = expression.GetLiterals();
                foreach(var l in literals) {
                    if(l is Code.VariableExpression) {
                        throw new Exception(Configuration.GetLocalized("A variable of the petri net cannot be evaluated when the petri net is not running."));
                    }
                }
            }

            string sourceName = System.IO.Path.GetTempFileName();

            var petriGen = CodeGen.PetriGen.PetriGenFromLanguage(_document.Settings.Language,
                                                                 _document);
            petriGen.WriteExpressionEvaluator(expression, sourceName, userData);

            string libName = System.IO.Path.GetTempFileName();

            var c = new Compiler(_document);
            var o = c.CompileSource(sourceName, libName);
            if(o != "") {
                throw new Exception(Configuration.GetLocalized("Compilation error:") + " " + o);
            }
            else {
                if(_document.Settings.Language == Code.Language.CSharp) {
                    try {
                        if(_debugServer == null && expression.GetVariables().Count > 0) {
                            throw new Exception("Expressions containing variables can only evaluated when the petri net is running in the editor.");
                        }
                        var libProxy = new GeneratedDynamicLibProxy(_document.Settings.Language,
                                                                    System.IO.Directory.GetParent(libName).FullName,
                                                                    System.IO.Path.GetFileName(libName),
                                                                    _document.CodePrefix + "Evaluator");
                        var dylib = libProxy.Load<Runtime.Evaluator>();

                        if(dylib == null) {
                            throw new Exception("Unable to load the evaluator!");
                        }
                        string value = dylib.Evaluate(_debugServer.CurrentPetriNet);
                        libProxy.Unload();
                        NotifyEvaluated(value);
                    }
                    catch(Exception e) {
                        NotifyUnrecoverableError(Configuration.GetLocalized("Evaluation error:") + " " + e.Message);
                    }
                    if(libName != "") {
                        System.IO.File.Delete(libName);
                    }
                }
                else {
                    try {
                        this.SendObject(new JObject(new JProperty("type", "evaluate"),
                                                    new JProperty("payload",
                                                                  new JObject(new JProperty("lib",
                                                                                            libName),
                                                                              new JProperty("language",
                                                                                            _document.Settings.LanguageName())))));
                    }
                    catch(Exception e) {
                        this.Detach();
                        throw e;
                    }
                }
            }
            System.IO.File.Delete(sourceName);
        }

        /// <summary>
        /// Notifies the user from an unrecoverable error.
        /// </summary>
        /// <param name="message">The error message.</param>
        protected abstract void NotifyUnrecoverableError(string message);

        /// <summary>
        /// Try to connect to a DebugServer.
        /// </summary>
        void Hello()
        {
            try {
                this.SendObject(new JObject(new JProperty("type", "hello"),
                                            new JProperty("payload",
                                                          new JObject(new JProperty("version",
                                                                                    Version)))));
		
                var ehlo = this.ReceiveObject();
                if(ehlo != null && ehlo["type"].ToString() == "ehlo") {
                    CurrentSessionState = SessionState.Started;
                    NotifyStatusMessage(Configuration.GetLocalized("Sucessfully connected."));
                    return;
                }
                else if(ehlo != null && ehlo["type"].ToString() == "error") {
                    throw new Exception(Configuration.GetLocalized("An error was returned by the debugger:") + " " + ehlo["payload"]);
                }
                throw new Exception(Configuration.GetLocalized("Invalid message received from debugger (expected ehlo).") + " " + ehlo);
            }
            catch(Exception e) {
                NotifyUnrecoverableError(Configuration.GetLocalized("An error occurred in the debugger during the handshake:") + " " + e.Message);
                this.Detach();
            }
        }

        /// <summary>
        /// The method in charge of receiving and dispatching the messages from the DebugServer.
        /// </summary>
        void Receiver()
        {
            try {
                _socket = new TcpClient();
                var result = _socket.BeginConnect(_document.Settings.Hostname,
                                                  _document.Settings.Port,
                                                  null,
                                                  null);
              
                if(!result.AsyncWaitHandle.WaitOne(1000)) {
                    throw new Exception("Timeout!");
                }

                _socket.EndConnect(result);
            }
            catch(Exception e) {
                this.Detach();
                NotifyUnrecoverableError(Configuration.GetLocalized("Unable to connect to the server:") + " " + e.Message);

                return;
            }

            try {
                this.Hello();

                while(CurrentSessionState != SessionState.Stopped && _socket.Connected) {
                    JObject msg = this.ReceiveObject();
                    if(msg == null)
                        break;

                    if(msg["type"].ToString() == "ack") {
                        if(msg["payload"].ToString() == "start") {
                            _petriRunning = true;
                            NotifyStateChanged();
                            this.UpdateBreakpoints();
                            NotifyStatusMessage(Configuration.GetLocalized("The petri net is running…"));
                        }
                        else if(msg["payload"].ToString() == "stopped") {
                            StopPetri();
                        }
                        else if(msg["payload"].ToString() == "stop") {
                            _petriRunning = false;
                            NotifyStateChanged();
                            lock(_debuggable.BaseDebugController.ActiveStates) {
                                _debuggable.BaseDebugController.ActiveStates.Clear();
                            }
                            NotifyActiveStatesChanged();
                            NotifyStatusMessage(Configuration.GetLocalized("The petri net execution has ended."));
                        }
                        else if(msg["payload"].ToString() == "reload") {
                            NotifyStateChanged();
                            NotifyStatusMessage(Configuration.GetLocalized("The Petri net has been successfully reloaded."));
                            if(_startAfterFix) {
                                StartPetri();
                            }
                        }
                        else if(msg["payload"].ToString() == "pause") {
                            _pause = true;
                            NotifyStateChanged();
                            NotifyStatusMessage(Configuration.GetLocalized("Paused."));
                        }
                        else if(msg["payload"].ToString() == "resume") {
                            _pause = false;
                            NotifyStateChanged();
                            NotifyStatusMessage(Configuration.GetLocalized("The petri net is running…"));
                        }
                    }
                    else if(msg["type"].ToString() == "error") {
                        _startAfterFix = false;
                        NotifyServerError(msg["payload"].ToString());

                        if(_petriRunning) {
                            this.StopPetri();
                        }
                    }
                    else if(msg["type"].ToString() == "detach" || msg["type"].ToString() == "detachAndExit") {
                        if(msg["payload"].ToString() == "kbye") {
                            CurrentSessionState = SessionState.Stopped;
                            _petriRunning = false;
                            NotifyStatusMessage(Configuration.GetLocalized("Disconnected."));
                        }
                        else {
                            CurrentSessionState = SessionState.Stopped;
                            _petriRunning = false;

                            throw new Exception(Configuration.GetLocalized("Remote debugger requested a session termination for reason:") + " " + msg["payload"].ToString());
                        }
                    }
                    else if(msg["type"].ToString() == "states") {
                        var states = msg["payload"].Select(t => t).ToList();

                        lock(_debuggable.BaseDebugController.ActiveStates) {
                            _debuggable.BaseDebugController.ActiveStates.Clear();
                            foreach(var s in states) {
                                var id = UInt64.Parse(s["id"].ToString());
                                var e = _document.EntityFromID(id);
                                if(e == null || !(e is State)) {
                                    throw new Exception(Configuration.GetLocalized("Entity sent from runtime doesn't exist on our side! (id: {0})",
                                                                                   id));
                                }
                                _debuggable.BaseDebugController.ActiveStates[e as State] = int.Parse(s["count"].ToString());
                            }
                        }

                        NotifyActiveStatesChanged();
                    }
                    else if(msg["type"].ToString() == "evaluation") {
                        var lib = msg["payload"]["lib"].ToString();
                        if(lib != "") {
                            System.IO.File.Delete(lib);
                        }
                        NotifyEvaluated(msg["payload"]["eval"].ToString());
                    }
                }
                if(CurrentSessionState != SessionState.Stopped) {
                    throw new Exception(Configuration.GetLocalized("Socket unexpectedly disconnected"));
                }
            }
            catch(Exception e) {
                NotifyUnrecoverableError(Configuration.GetLocalized("An error occurred in the debugger client:") + " " + e.Message);
                this.Detach();
            }

            try {
                _socket.Close();
            }
            catch(Exception) {
            }
            NotifyStateChanged();
        }

        /// <summary>
        /// Tries to receive a JSON object from the DebugServer.
        /// </summary>
        /// <returns>The object.</returns>
        JObject ReceiveObject()
        {
            int count = 0;
            while(CurrentSessionState != SessionState.Stopped) {
                string val = this.ReceiveString();

                if(val.Length > 0)
                    return JObject.Parse(val);

                if(++count > 5) {
                    throw new Exception(Configuration.GetLocalized("Remote debugger isn't available anymore!"));
                }
                Thread.Sleep(1);
            }

            return null;
        }

        /// <summary>
        /// Sends a JSON message to the DebugServer.
        /// </summary>
        /// <param name="o">O.</param>
        void SendObject(JObject o)
        {
            this.SendString(o.ToString());
        }

        /// <summary>
        /// Low level message reception routine.
        /// </summary>
        /// <returns>The string.</returns>
        string ReceiveString()
        {
            byte[] msg;

            lock(_downLock) {
                byte[] countBytes = new byte[4];

                int len = _socket.GetStream().Read(countBytes, 0, 4);
                if(len != 4)
                    return "";

                UInt32 count = (UInt32)countBytes[0] | ((UInt32)countBytes[1] << 8) | ((UInt32)countBytes[2] << 16) | ((UInt32)countBytes[3] << 24);
                UInt32 read = 0;

                msg = new byte[count];
                while(read < count) {
                    read += (UInt32)_socket.GetStream().Read(msg, (int)read, (int)(count - read));
                }
            }

            return System.Text.Encoding.UTF8.GetString(msg);
        }

        /// <summary>
        /// Low level message sending routine.
        /// </summary>
        /// <returns>The string.</returns>
        void SendString(string s)
        {
            var msg = System.Text.Encoding.UTF8.GetBytes(s);

            UInt32 count = (UInt32)msg.Length;

            byte[] bytes = new byte[4 + count];

            bytes[0] = (byte)((count >> 0) & 0xFF);
            bytes[1] = (byte)((count >> 8) & 0xFF);
            bytes[2] = (byte)((count >> 16) & 0xFF);
            bytes[3] = (byte)((count >> 24) & 0xFF);

            msg.CopyTo(bytes, 4);

            lock(_upLock) {
                _socket.GetStream().Write(bytes, 0, bytes.Length);
            }
        }

        GeneratedDynamicLibProxy _libProxy;
        Runtime.DebugServer _debugServer;
        Runtime.DynamicLib _dynamicLib;

        bool _startAfterFix = false;
        bool _petriRunning, _pause;
        volatile SessionState _sessionState;
        Thread _receiverThread;

        volatile TcpClient _socket;
        object _upLock = new object();
        object _downLock = new object();

        protected Debuggable _debuggable;
        protected HeadlessDocument _document;
    }
}

