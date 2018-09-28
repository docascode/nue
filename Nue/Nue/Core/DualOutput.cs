using System;
using System.IO;
using System.Text;

namespace Nue.Core
{
    public static class DualOutput
    {
        private static TextWriter _current;

        private class OutputWriter : TextWriter
        {
            static object locker = new object();
            public override Encoding Encoding
            {
                get
                {
                    return _current.Encoding;
                }
            }

            public override void WriteLine(string value)
            {
                _current.WriteLine(value);

                lock (locker)
                {
                    File.AppendAllText("nue-execution-log.txt", value);
                }
            }
        }

        public static void Initialize()
        {
            _current = Console.Out;
            Console.SetOut(new OutputWriter());
        }
    }
}
