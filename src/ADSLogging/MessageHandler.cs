using System;

namespace ADSLogging
{
    public class MessageHandler
    {
        private static void LogWithColor(string prefix, string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"[{prefix}] {message}");
            Console.ResetColor();
        }

        public static void LogValueChanged(string message)
        {
            LogWithColor("VALUE CHANGED", message, ConsoleColor.White);
        }

        public static void LogStatus(string message)
        {
            LogWithColor("INFO", message, ConsoleColor.Green);
        }

        public static void LogError(string message)
        {
            LogWithColor("ERROR", message, ConsoleColor.Red);
        }

        public static void LogWarning(string message)
        {
            LogWithColor("WARNING", message, ConsoleColor.Yellow);
        }
    }
}
