using System;
using System.Text;

namespace Nue.Core
{

    public static class ConsoleEx
    {
        public static void WriteLine(string message, ConsoleColor color = ConsoleColor.White, StringBuilder output = null)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = oldColor;

            output?.AppendLine(message);
        }

        public static void Write(string message, ConsoleColor color = ConsoleColor.White, StringBuilder output = null)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(message);
            Console.ForegroundColor = oldColor;

            output?.Append(message);
        }
    }
}
