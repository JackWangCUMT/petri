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

namespace Petri
{
	public abstract class PetriGen {
		public PetriGen(HeadlessDocument doc, CodeGen generator) {
			CodeGen = generator;
			Document = doc;
		}

		protected HeadlessDocument Document {
			get;
			private set;
		}

		protected CodeGen CodeGen {
			get;
			set;
		}

		protected abstract void Begin();

		protected virtual void GenerateCodeFor(Entity entity, IDManager lastID) {
			if(entity is PetriNet) {
				var pn = (PetriNet)entity;
				foreach(State s in pn.States) {
					GenerateCodeFor(s, lastID);
				}

				CodeGen += "\n";

				foreach(Transition t in pn.Transitions) {
					GenerateCodeFor(t, lastID);
				}
			}
			else {
				throw new Exception("Should not get there…");
			}
		}

		protected abstract void End();

		protected void Format() {
			CodeGen.Format();
		}

		public string GetValue() {
			Begin();
			GenerateCodeFor(Document.PetriNet, new IDManager(Document.LastEntityID + 1));
			End();

			return CodeGen.Value;
		}

		public string GetHash() {
			Begin();
			GenerateCodeFor(Document.PetriNet, new IDManager(Document.LastEntityID + 1));
			End();

			return Hash;
		}

		public abstract string Extension {
			get;
		}

		public virtual void Write() {
			System.IO.File.WriteAllText(PathToFile(Document.Settings.Name + "." + Extension), GetValue());
		}

		protected string PathToFile(string filename) {
			return System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Directory.GetParent(Document.Path).FullName, Document.Settings.SourceOutputPath), filename);
		}


		protected string Hash {
			get;
			set;
		}
	}
}

