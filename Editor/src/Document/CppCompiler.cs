using System;
using System.Diagnostics;
using Gtk;

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
			string s = document.Settings.CompilerArguments;
			if(s.Length < 5000) {
				p.StartInfo.Arguments = document.Settings.CompilerArguments;
			}
			else {
				return "Erreur : l'invocation du compilateur est trop longue (" + s.Length.ToString() + " caractères. Essayez de supprimer des chemins d'inclusion récursifs.";
			}
			p.Start();

			string output = p.StandardOutput.ReadToEnd();
			p.WaitForExit();

			return output;
		}

		Document document;
	}
}

