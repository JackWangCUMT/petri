using System;
using System.IO;

namespace TestPetri
{
    public class Utility
    {
        public static TResult InvokeAndRedirectOutput<TResult>(System.Func<TResult> function,
                                                               out string stdout,
                                                               out string stderr)
        {
            TResult result;

            var previousOut = Console.Out;
            var previousErr = Console.Error;

            using(StringWriter sout = new StringWriter(), serr = new StringWriter()) {
                Console.SetOut(sout);
                Console.SetError(serr);

                result = function();

                stdout = sout.ToString();
                stderr = serr.ToString();
            }

            Console.SetOut(previousOut);
            Console.SetError(previousErr);

            return result;
        }

        public static void InvokeAndRedirectOutput(System.Action function, out string stdout, out string stderr)
        {
            InvokeAndRedirectOutput(() => {
                function();
                return 0;
            }, out stdout, out stderr);
        }
    }
}

