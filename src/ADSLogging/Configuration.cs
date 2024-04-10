using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace ADSLogging
{
    /// <summary>
    /// Represents the configuration of a logging variable, including symbol path, decimal places, threshold, and last known value.
    /// </summary>
    public class VariableConfig
    {
        public string SymbolPath { get; set; } = "";
        public int? DecimalPlaces { get; set; }
        public double? Threshold { get; set; }
        public object LastKnownValue { get; set; }

        /// <summary>
        /// Initializes a new instance of the VariableConfig class with the specified symbol path, decimal places, and threshold.
        /// </summary>
        /// <param name="symbolPath">The symbol path of the logging variable.</param>
        /// <param name="decimalPlaces">The number of decimal places for the logging variable.</param>
        /// <param name="threshold">The threshold value for the logging variable.</param>
        public VariableConfig(string symbolPath, int? decimalPlaces, double? threshold)
        {
            this.SymbolPath = symbolPath;
            this.DecimalPlaces = decimalPlaces;
            this.Threshold = threshold;
        }
    }

    /// <summary>
    /// Represents the configuration settings for the ADS logging application.
    /// </summary>
    public class Configuration
    {
        #region XML-Config
        // Nodes
        private const string NODE_TC_VERSION = "/Configuration/TwinCATVersion";
        private const string NODE_AMS_NET_ID = "/Configuration/AmsNetId";
        private const string NODE_PORT = "/Configuration/Port";
        private const string NODE_MAX_LINES_PER_LOG_FILE = "/Configuration/MaxLinesPerLogFile";
        private const string NODE_VARIABLE_CONFIG = "/Configuration/VariableConfig/var";
        // Attributes
        private const string ATTR_THRESHOLD = "Threshold";
        private const string ATTR_DECIMAL_PLACES = "DecimalPlaces";
        // Default values
        private const TwinCATVersion DEFAULT_TC_VERSION = TwinCATVersion.TwinCAT3;
        private const string DEFAULT_AMS_NET_ID = "192.168.1.1.1.1";
        private static readonly Dictionary<string, (string DecimalPlaces, string Threshold)> variableAttributes = new Dictionary<string, (string, string)>
        {
            { "MAIN.rRealValue", ("2", "0.01") },
            { "MAIN.rLREALValue", ("3", null) },
            { "MAIN.iIntValue", (null, null) },
            { "MAIN.xBoolValue", (null, null) },
            { "MAIN.bByteValue", (null, "2") },
        };
        // Other
        private const string CONFIG_FILE_NAME = "Configuration.xml";
        #endregion

        private readonly string configPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory(), CONFIG_FILE_NAME);
        private readonly XmlDocument xmlDoc = new XmlDocument();

        public TwinCATVersion TwinCATVersion { get; private set; }
        public bool Error { get; private set; } = false;
        public string AmsNetId { get; private set; }
        public int TcPort { get; private set; }
        public string LoggingPath { get; private set; } = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory(), @"logs\");
        public int MaxLinesPerLogFile { get; private set; } = 1000;
        public List<VariableConfig> LoggingVariables { get; private set; } = new List<VariableConfig>();

        /// <summary>
        /// Initializes a new instance of the Configuration class, reading settings from the configuration file.
        /// </summary>
        public Configuration()
        {
            if (!File.Exists(configPath))
            {
                HandleMissingConfiguration();
            }
            else
            {
                Error = ReadConfiguration();
                if (!Error)
                {
                    MessageHandler.LogStatus("Configuration successfully read");
                    CreateLoggingFolder();
                }
            }
        }

        /// <summary>
        /// Handles the case when the configuration file is missing.
        /// Creates a new configuration file and logs a message indicating the need for configuration.
        /// </summary>
        private void HandleMissingConfiguration()
        {
            MessageHandler.LogStatus("No configuration found. Creating...");
            CreateConfiguration();
            MessageHandler.LogStatus("Program exiting. Please configure first");
            Error = true;
        }

        /// <summary>
        /// Reads the configuration from the XML file.
        /// </summary>
        /// <returns>True if any error occurs during the reading process, otherwise false.</returns>
        private bool ReadConfiguration()
        {
            List<Func<bool>> readMethods = new List<Func<bool>>()
            {
                ReadTCVersion,
                ReadTCPort,
                ReadAmsNetId,
                ReadMaxLinesPerLogfile,
                ReadVariableConfig,
            };

            try
            {
                // Load configuration
                xmlDoc.Load(configPath);

                // Read configuration until an error occures
                foreach(Func<bool> readMethod in readMethods)
                {
                    if (readMethod())
                        return true;
                }

                return false;
            }
            catch (Exception e)
            {
                MessageHandler.LogError($"Can't read the configuration. Program stops\n{e.Message}");
                return true;
            }
        }

        /// <summary>
        /// Reads the TwinCAT version from the configuration.
        /// </summary>
        /// <returns>True if the TwinCAT version is invalid or not found in the configuration, otherwise false.</returns>
        private bool ReadTCVersion()
        {
            string tcVersion = xmlDoc.SelectSingleNode(NODE_TC_VERSION)?.InnerText;
            if (Enum.TryParse(tcVersion, out TwinCATVersion parsedTCVersion))
            {
                TwinCATVersion = parsedTCVersion;
                return false;
            }

            MessageHandler.LogError($"Specificed TwinCAT version is not possible {tcVersion}\nPossible values are {TwinCATVersion.TwinCAT2} or {TwinCATVersion.TwinCAT3}");
            return true;
        }

        /// <summary>
        /// Reads the AMS NetId from the configuration based on the TwinCAT version.
        /// </summary>
        /// <returns>True if the AMS NetId is invalid or not found in the configuration, otherwise false.</returns>
        private bool ReadAmsNetId()
        {
            string parsedAmsNetId = string.Empty;

            if (TwinCATVersion == TwinCATVersion.TwinCAT2)
            {
                parsedAmsNetId = ReadTC2AmsNetId();
            } 
            else if (TwinCATVersion == TwinCATVersion.TwinCAT3)
            {
                parsedAmsNetId = ReadTc3AmsNetId();
            }

            if (string.IsNullOrEmpty(parsedAmsNetId))
                return true;

            AmsNetId = parsedAmsNetId;
            return false;
        }

        private string ReadTC2AmsNetId()
        {
            return TwinCAT2.Client.GetAmsNetId(xmlDoc.SelectSingleNode(NODE_AMS_NET_ID)?.InnerText);
        }

        private string ReadTc3AmsNetId()
        {
            return TwinCAT3.Client.GetAmsNetId(xmlDoc.SelectSingleNode(NODE_AMS_NET_ID)?.InnerText);
        }

        /// <summary>
        /// Reads the TwinCAT port from the configuration.
        /// </summary>
        /// <returns>True if the TwinCAT port is invalid or not found in the configuration, otherwise false.</returns>
        private bool ReadTCPort()
        {
            if (!int.TryParse(xmlDoc.SelectSingleNode(NODE_PORT)?.InnerText, out int parsedPort) || parsedPort <= 0)
            {
                MessageHandler.LogWarning($"Invalid TCPort value found in configuration. Using default value: {TcPort}");
                return true;
            }
            else
            {
                TcPort = parsedPort;
                return false;
            }
        }

        /// <summary>
        /// Reads the maximum number of lines per logfile from the configuration.
        /// </summary>
        /// <returns>True if the maximum lines per logfile value is invalid or not found in the configuration, otherwise false.</returns>
        private bool ReadMaxLinesPerLogfile()
        {
            if (!int.TryParse(xmlDoc.SelectSingleNode(NODE_MAX_LINES_PER_LOG_FILE)?.InnerText, out int parsedMaxLines) || parsedMaxLines <= 0)
            {
                MessageHandler.LogWarning($"Invalid MaxLinesPerLogFile value found in configuration. Using default value: {MaxLinesPerLogFile}");
                return true;
            }
            else
            {
                MaxLinesPerLogFile = parsedMaxLines;
                return false;
            }
        }

        /// <summary>
        /// Reads the variable configurations from the configuration.
        /// </summary>
        /// <returns>True if any error occurs during the reading process, otherwise false.</returns>
        private bool ReadVariableConfig()
        {
            XmlNodeList varNodes = xmlDoc.SelectNodes(NODE_VARIABLE_CONFIG);
            if (varNodes is null)
            {
                MessageHandler.LogWarning("No logging variables found in configuration. Program stops");
                return true;
            }
               
            foreach (XmlNode varNode in varNodes)
            {
                string symbolPath = varNode.InnerText;

                if (LoggingVariables.Any(v => v.SymbolPath == symbolPath))
                {
                    continue;
                }

                int? decimalPlaces = null;
                if (varNode.Attributes?[ATTR_DECIMAL_PLACES]?.Value != null)
                {
                    if (int.TryParse(varNode.Attributes[ATTR_DECIMAL_PLACES]?.Value, out int parsedDecimalPlaces))
                    {
                        decimalPlaces = parsedDecimalPlaces;
                    }
                }

                // Read Treshold attribute
                double? threshold = null;
                if (varNode.Attributes?[ATTR_THRESHOLD]?.Value != null)
                {
                    string value = varNode.Attributes[ATTR_THRESHOLD].Value;
                    value = value.Replace(",", ".");

                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedThreshold))
                    {
                        threshold = parsedThreshold;
                    }
                }

                // Add to the list
                LoggingVariables.Add(new VariableConfig(symbolPath, decimalPlaces, threshold));
            }

            return false;
        }

        /// <summary>
        /// Creates the logging folder if it does not exist.
        /// </summary>
        private void CreateLoggingFolder()
        {
            if (!Directory.Exists(LoggingPath))
            {
                try
                {
                    Directory.CreateDirectory(LoggingPath);
                    MessageHandler.LogStatus($"Logs will be saved in this folder: {LoggingPath}");
                }
                catch (Exception e)
                {
                    MessageHandler.LogError($"Failed to create the logging folder\n{e.Message}");
                    Error = true;
                }
            }
        }

        /// <summary>
        /// Creates a new XML configuration file with default values and the specified logging variables.
        /// </summary>
        private void CreateConfiguration()
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                xmlDoc.AppendChild(xmlDeclaration);

                XmlElement rootElement = xmlDoc.CreateElement("Configuration");
                xmlDoc.AppendChild(rootElement);

                XmlElement twinCATVersion = xmlDoc.CreateElement("TwinCATVersion");
                twinCATVersion.InnerText = DEFAULT_TC_VERSION.ToString();
                rootElement.AppendChild(twinCATVersion);

                XmlElement amsNetIdElement = xmlDoc.CreateElement("AmsNetId");
                amsNetIdElement.InnerText = DEFAULT_AMS_NET_ID;
                rootElement.AppendChild(amsNetIdElement);

                XmlElement portElement = xmlDoc.CreateElement("Port");
                portElement.InnerText = TcPort.ToString();
                rootElement.AppendChild(portElement);

                XmlElement maxLinesPerLogFileElement = xmlDoc.CreateElement("MaxLinesPerLogFile");
                maxLinesPerLogFileElement.InnerText = MaxLinesPerLogFile.ToString();
                rootElement.AppendChild(maxLinesPerLogFileElement);

                XmlElement variableConfigElement = xmlDoc.CreateElement("VariableConfig");
                rootElement.AppendChild(variableConfigElement);

                foreach (var variable in variableAttributes)
                {
                    XmlElement varElement = xmlDoc.CreateElement("var");
                    varElement.InnerText = variable.Key;

                    // Set DecimalPlaces attribute if present
                    if (variable.Value.DecimalPlaces != null)
                    {
                        varElement.SetAttribute(ATTR_DECIMAL_PLACES, variable.Value.DecimalPlaces);
                    }

                    // Set Threshold attribute if present
                    if (variable.Value.Threshold != null)
                    {
                        varElement.SetAttribute(ATTR_THRESHOLD, variable.Value.Threshold);
                    }

                    variableConfigElement.AppendChild(varElement);
                }

                xmlDoc.Save(configPath);
                MessageHandler.LogStatus($"Configuration file created: {CONFIG_FILE_NAME}");
            }
            catch (Exception e)
            {
                MessageHandler.LogError($"Can't create the configuration. Program stops\n{e.Message}");
            }
        }
    }
}
