using System;
using System.Linq;
using System.IO;

namespace ADSLogging
{
    /// <summary>
    /// Represents a logger for recording changes in variable values to log files.
    /// </summary>
    public class Logger
    {
        private readonly Configuration config;
        private int logNumber = 1;

        /// <summary>
        /// Initializes a new instance of the Logger class with the specified configuration.
        /// </summary>
        /// <param name="config">The configuration settings for the logger.</param>
        public Logger(Configuration config)
        {
            this.config = config;
        }

        /// <summary>
        /// Logs the specified symbol path and value to the log file.
        /// </summary>
        /// <param name="symbolPath">The symbol path of the variable.</param>
        /// <param name="value">The value of the variable.</param>
        private void LogData(string symbolPath, string value)
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Variable '{symbolPath}' changed to: {value}\r";

            try
            {
                MessageHandler.LogValueChanged(logEntry);

                string logFilePath = $"{config.LoggingPath}Log.txt";

                if (File.Exists(logFilePath) && File.ReadLines(logFilePath).Count() >= config.MaxLinesPerLogFile)
                {
                    string newLogFilePath = $"{config.LoggingPath}Log{logNumber++}.txt";
                    File.Move(logFilePath, newLogFilePath);
                }

                using (StreamWriter writer = File.AppendText(logFilePath))
                {
                    writer.Write(logEntry);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while writing to log file: " + e.Message);
            }
        }

        /// <summary>
        /// Checks the specified variable data against the configured settings and logs changes if necessary.
        /// </summary>
        /// <param name="variableConfig">The configuration of the variable to be checked.</param>
        /// <param name="value">The current value of the variable.</param>
        public void CheckData(VariableConfig variableConfig, object value)
        {
            if (!IsNumericType(value))
            {
                LogData(variableConfig.SymbolPath, value.ToString());
                return;
            }

            int? decimalPlaces = variableConfig.DecimalPlaces;
            double? minChangeThreshold = variableConfig.Threshold;

            // Truncate the value if configured and possible
            if ((value is double || value is float) && decimalPlaces != null)
            {
                TruncateAndParse(ref value, (int)decimalPlaces);
            }

            // Check if a min change threshold is configured and calculate if it should be logged
            if (minChangeThreshold is null || (variableConfig.LastKnownValue is null || CheckMinChangeThreshold(value, variableConfig.LastKnownValue, minChangeThreshold.Value)))
            {
                variableConfig.LastKnownValue = value;
                LogData(variableConfig.SymbolPath, value.ToString());
            }
        }

        /// <summary>
        /// Checks if the specified value has changed beyond the configured minimum change threshold.
        /// </summary>
        /// <typeparam name="T">The type of the values being compared.</typeparam>
        /// <param name="value">The current value.</param>
        /// <param name="lastKnownValue">The last known value.</param>
        /// <param name="minChangeThreshold">The minimum change threshold.</param>
        /// <returns>True if the change exceeds the threshold, otherwise false.</returns>
        private static bool CheckMinChangeThreshold<T>(T value, T lastKnownValue, double minChangeThreshold)
        {
            if (value is float floatValue && lastKnownValue is float lastKnownValueFloat)
            {
                return Math.Abs(floatValue - lastKnownValueFloat) >= minChangeThreshold;
            }

            if (value is double doubleValue && lastKnownValue is double lastKnownValueDouble)
            {
                return Math.Abs(doubleValue - lastKnownValueDouble) >= minChangeThreshold;
            }

            // For generic types
            return Math.Abs(Convert.ToDouble(value) - Convert.ToDouble(lastKnownValue)) >= minChangeThreshold;
        }

        /// <summary>
        /// Truncates and parses the specified value according to the configured decimal places.
        /// </summary>
        /// <param name="value">The value to be truncated and parsed.</param>
        /// <param name="decimalPlaces">The number of decimal places to truncate to.</param>
        private static void TruncateAndParse(ref object value, int decimalPlaces)
        {
            string stringValue = value.ToString();

            // Check if the value has a decimal separator
            int decimalSeparatorIndex = stringValue.IndexOf(',');

            if (decimalSeparatorIndex != -1 && stringValue.Length - decimalSeparatorIndex > decimalPlaces)
            {
                string truncatedStringValue = stringValue.Substring(0, decimalSeparatorIndex + decimalPlaces + 1);   // +1 to include the Seperator

                if (value is double && double.TryParse(truncatedStringValue, out double doubleValue))
                {
                    value = doubleValue;
                }
                else if (value is float && float.TryParse(truncatedStringValue, out float floatValue))
                {
                    value = floatValue;
                }
            }
        }

        /// <summary>
        /// Checks if the specified value is of an integer type.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>True if the value is of an integer type, otherwise false.</returns>
        private static bool IsIntegerType(object value)
        {
            return value is sbyte || value is byte ||
                   value is short || value is ushort ||
                   value is int || value is uint ||
                   value is long || value is ulong;
        }

        /// <summary>
        /// Checks if the specified value is of a numeric type.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>True if the value is of a numeric type, otherwise false.</returns>
        private static bool IsNumericType(object value)
        {

            return IsIntegerType(value) || value is float || value is double;
        }
    }
}
