using System;
using System.Diagnostics;

namespace Petri
{
	public class CppCompiler {
		public CppCompiler(Document doc) {
			document = doc;
		}

		public string Compile() {
			Process p = new Process();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.FileName = document.Settings.Compiler;
			p.StartInfo.Arguments = document.Settings.CompilerArguments;
			p.Start();
			string output = p.StandardOutput.ReadToEnd();
			p.WaitForExit();

			return output;
		}

		Document document;
	}
}

