using System;
using System.Collections.Generic;
using System.Linq;
using System.Buffers.Binary;

namespace ADSLogging
{
    extern alias TwinCAT3Ads;
    extern alias TwinCAT2Ads;

    /// <summary>
    /// Enumeration of ADS data types.
    /// This enumeration has been reimplemented to support both versions of the DLL.
    /// </summary>
    public enum AdsDataType
    {
        ADST_VOID = 0,
        ADST_INT16 = 2,
        ADST_INT32 = 3,
        ADST_REAL32 = 4,
        ADST_REAL64 = 5,
        ADST_VARIANT = 12,
        ADST_MAXTYPES = 13,
        ADST_INT8 = 16,
        ADST_UINT8 = 17,
        ADST_UINT16 = 18,
        ADST_UINT32 = 19,
        ADST_INT64 = 20,
        ADST_UINT64 = 21,
        ADST_STRING = 30,
        ADST_WSTRING = 31,
        ADST_REAL80 = 32,
        ADST_BIT = 33,
        ADST_BIGTYPE = 65
    }

    public abstract class BaseClient<TClient, TEventArgs> : IDisposable
    {
        protected static readonly List<string> notSupportedPlcOpenTypes = new List<string>()
        {
            "LDATE",
            "LDATE_AND_TIME",
            "LTIME_OF_DAY",
        };
        private readonly List<string> PlcOpenTypes = new List<string>()
        {
            "TIME",
            "LTIME",
            "DT",
            "DATE_AND_TIME",
            "DATE",
            "TOD",
            "TIME_OF_DAY"
        };

        protected readonly List<object> notificationHandles = new List<object>();
        protected readonly Configuration config;
        protected readonly Logger logger;
        protected readonly TClient client;

        /// <summary>
        /// Initializes a new instance of the BaseClient class.
        /// </summary>
        /// <param name="config">The configuration settings for the ADS client.</param>
        /// <param name="logger">The logger instance for logging ADS-related events.</param>
        /// <param name="client">The ADS client instance.</param>
        public BaseClient(Configuration config, Logger logger, TClient client)
        {
            this.config = config;
            this.logger = logger;
            this.client = client;
        }

        /// <summary>
        /// Disposes resources associated with the client instance.
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Attempts to establish a connection to the target device.
        /// </summary>
        /// <returns>True if the connection is successfully established, otherwise false.</returns>
        public abstract bool StartConnection();

        /// <summary>
        /// Handles ADS notification events.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The arguments containing information about the notification event.</param>
        public abstract void HandleAdsNotificationEvent(object sender, TEventArgs eventArgs);

        /// <summary>
        /// Registers device notifications for logging variables.
        /// </summary>
        public abstract void RegisterNotifications();

        /// <summary>
        /// Resolves the AdsDataType enumeration value based on the provided type name.
        /// </summary>
        /// <param name="typeName">The name of the data type.</param>
        /// <returns>
        /// The corresponding AdsDataType enumeration value.
        /// If the provided type name is recognized, the corresponding AdsDataType value is returned.
        /// Otherwise, the default value <see cref="AdsDataType.ADST_VOID"/> is returned.
        /// </returns>
        public AdsDataType ResolveDataTypeId(string typeName)
        {
            if (Enum.TryParse(typeName, true, out AdsDataType adsDataType))
            {
                return adsDataType;
            }

            // Default value, if type is unknown
            return AdsDataType.ADST_VOID;
        }

        /// <summary>
        /// Handles event data by resolving its data type, converting it, and checking against configured variable settings.
        /// </summary>
        /// <param name="data">The event data.</param>
        /// <param name="dataTypeId">The DataTypeId from the Enum.</param>
        /// <param name="typeName">The data type name.</param>
        /// <param name="instancePath">The instance path of the variable.</param>
        public void HandleEvent(ReadOnlyMemory<byte> data, string dataTypeId, string typeName, string instancePath)
        {

            AdsDataType dataType = ResolveDataTypeId(dataTypeId);

            object value = ConvertData(data, dataType, typeName);
            if (value != null)
            {
                // Find the VariableConfig based on instancePath
                VariableConfig variableConfig = config.LoggingVariables.FirstOrDefault(v => string.Compare(v.SymbolPath, instancePath, StringComparison.OrdinalIgnoreCase) == 0);
                logger.CheckData(variableConfig, value);
            }
        }

        /// <summary>
        /// Converts raw byte data to the corresponding .NET data type based on the specified ADS data type.
        /// </summary>
        /// <param name="data">The raw byte data to convert.</param>
        /// <param name="dataType">The ADS data type enumeration value.</param>
        /// <param name="typeName">The type name associated with the data type.</param>
        /// <returns>The converted data as an object, or null if the conversion is not supported.</returns>
        public object ConvertData(ReadOnlyMemory<byte> data, AdsDataType dataType, string typeName)
        {
            if (IsDateTimeType(typeName))
                return ReadPlcDateTime(data, typeName);

            switch (dataType)
            {
                case AdsDataType.ADST_BIT:
                    // Convert raw byte data to a boolean value (bit)
                    return data.Span[0] != 0;

                case AdsDataType.ADST_INT8:
                    return (sbyte)data.Span[0];

                case AdsDataType.ADST_UINT8:
                    return data.Span[0];

                case AdsDataType.ADST_INT16:
                    return BinaryPrimitives.ReadInt16LittleEndian(data.Span);

                case AdsDataType.ADST_UINT16:
                    return BinaryPrimitives.ReadUInt16LittleEndian(data.Span);

                case AdsDataType.ADST_INT32:
                    return BinaryPrimitives.ReadInt32LittleEndian(data.Span);

                case AdsDataType.ADST_UINT32:
                    return BinaryPrimitives.ReadUInt32LittleEndian(data.Span);

                case AdsDataType.ADST_INT64:
                    return BinaryPrimitives.ReadInt64LittleEndian(data.Span);

                case AdsDataType.ADST_UINT64:
                    return BinaryPrimitives.ReadUInt64LittleEndian(data.Span);

                case AdsDataType.ADST_REAL32:
                    return BitConverter.ToSingle(data.ToArray(), 0);

                case AdsDataType.ADST_REAL64:
                    return BitConverter.ToDouble(data.ToArray(), 0);

                case AdsDataType.ADST_STRING:
                    return System.Text.Encoding.ASCII.GetString(data.Span.Slice(0, data.Span.IndexOf((byte)0)).ToArray());

                case AdsDataType.ADST_BIGTYPE:
                    if (typeName.Contains("POINTER"))
                        return BinaryPrimitives.ReadUInt32LittleEndian(data.Span);
                    return null;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Checks if the given data type name corresponds to a PLC Open date and time type.
        /// </summary>
        /// <param name="typeName">The name of the data type to be checked.</param>
        /// <returns>True if the data type is a PLC Open date and time type, otherwise false.</returns>
        private bool IsDateTimeType(string typeName)
        {
            return PlcOpenTypes.Contains(typeName);
        }

        /// <summary>
        /// Reads and converts PLC Open date and time values based on the specified type name.
        /// </summary>
        /// <param name="typeName">The type name specifying the PLC Open date and time format.</param>
        /// <returns>The converted C# object representing the PLC Open date and time value.</returns>
        private static object ReadPlcDateTime(ReadOnlyMemory<byte> data, string typeName)
        {
            switch (typeName)
            {
                case "LTIME":
                    return TimeDataConverter.ToNanosecondsString(data);
                case "TIME":
                    return TimeDataConverter.ToLimitedMillisecondsString(data);
                case "TOD":
                case "TIME_OF_DAY":
                    return TimeDataConverter.ToLimitedMillisecondsString(data, isTimeOfDay:true);
                case "DT":
                case "DATE_AND_TIME":
                case "DATE":
                    DateTime dateTime = TimeDataConverter.ToDateTime(data);
                    if (typeName == "DATE")
                        return dateTime.ToShortDateString();
                    return dateTime;
                default:
                    return null;
            }
        }
    }
}

namespace TwinCAT2
{
    extern alias TwinCAT2Ads;

    using ADSLogging;
    using TwinCAT2Ads::TwinCAT.Ads;
    using TwinCAT2Ads::TwinCAT.Ads.TypeSystem;
    using TwinCAT2Ads::TwinCAT.TypeSystem;

    public class Client : BaseClient<TcAdsClient, AdsNotificationEventArgs>
    {
        private readonly AdsStream readStream = new AdsStream(sizeof(UInt64));

        /// <summary>
        /// Initializes a new instance of the Client class.
        /// </summary>
        /// <param name="config">The configuration settings for the ADS client.</param>
        /// <param name="logger">The logger instance for logging ADS-related events.</param>
        public Client(Configuration config, Logger logger) : base(config, logger, new TcAdsClient()) { }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override void Dispose()
        {
            foreach (int notificationHandle in notificationHandles)
            {
                client.DeleteDeviceNotification(notificationHandle);
            }
            client.AdsNotification -= HandleAdsNotificationEvent;
            client.Dispose();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><inheritdoc/></returns>
        public override bool StartConnection()
        {
            try
            {
                MessageHandler.LogStatus($"Trying to connect to target at: {config.AmsNetId} with Port: {config.TcPort}");
                client.Connect(config.AmsNetId, config.TcPort);
                AdsErrorCode adsErrorCode = client.TryReadState(out StateInfo stateInfo);

                if (client != null && adsErrorCode == AdsErrorCode.NoError)
                {
                    MessageHandler.LogStatus("Successfully connected to target");
                    client.AdsNotification += HandleAdsNotificationEvent;
                    return true;
                }
                else
                {
                    MessageHandler.LogError($"Can't find a target with AmsNetId: {config.AmsNetId} and Port: {config.TcPort}\nIs the PLC in Run mode?");
                    return false;
                }
            }
            catch (AdsErrorException adsError)
            {
                MessageHandler.LogError($"Can't connect to target. ADS error: {adsError.ErrorCode} - {adsError.Message}");
                return false;
            }
            catch (Exception e)
            {
                MessageHandler.LogError($"Fatal error. Program stops\n{e.Message}");
                return false;
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="sender"><inheritdoc/></param>
        /// <param name="e"><inheritdoc/></param>
        public override void HandleAdsNotificationEvent(object sender, AdsNotificationEventArgs e)
        {
            if (e.UserData is Symbol symbolInfo)
            {
                HandleEvent(e.DataStream.ToArray(), symbolInfo.DataTypeId.ToString(), symbolInfo.TypeName, symbolInfo.InstancePath);
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override void RegisterNotifications()
        {
            ISymbolLoader loader = SymbolLoaderFactory.Create(client, SymbolLoaderSettings.Default);

            foreach (VariableConfig variableConfig in config.LoggingVariables)
            {
                try
                {
                    Symbol symbolInfo = (Symbol)loader.Symbols[variableConfig.SymbolPath];
                    Console.WriteLine(symbolInfo.TypeName);
                    if (notSupportedPlcOpenTypes.Contains(symbolInfo.TypeName) || symbolInfo.TypeName.Contains("WSTRING"))
                    {
                        MessageHandler.LogWarning($"The data type: {symbolInfo.TypeName} from the variable: {variableConfig.SymbolPath} is not supported.\nThis variable will be ignored!");
                        continue;
                    }

                    notificationHandles.Add(client.AddDeviceNotification(variableConfig.SymbolPath, readStream, AdsTransMode.OnChange, 200, 0, symbolInfo));
                    MessageHandler.LogStatus($"Variable: {variableConfig.SymbolPath} successfully added to the logging");
                }
                catch (Exception e)
                {
                    MessageHandler.LogError($"Can't register variable {variableConfig.SymbolPath}\n{e.Message}");
                }
            }
        }

        /// <summary>
        /// Gets the AmsNetId from the specified string representation of AmsNetId.
        /// If the input is invalid, a warning is logged, and the local AmsNetId is used.
        /// </summary>
        /// <param name="amsNetId">The string representation of AmsNetId to be parsed.</param>
        /// <returns>The parsed AmsNetId or the local AmsNetId if parsing fails.</returns>
        public static string GetAmsNetId(string amsNetId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(amsNetId))
                {
                    MessageHandler.LogWarning("No valid AmsNetId found in configuration. Using the local AmsNetId");
                    return AmsNetId.Local.ToString();
                }
                else
                {
                    if (AmsNetId.TryParse(amsNetId, out AmsNetId parsedAmsNetId)) 
                        return parsedAmsNetId.ToString();
                    return string.Empty;
                }   
            }
            catch (Exception ex)
            {
                MessageHandler.LogError($"No AmsNetId found. Probably the TwinCAT System Service is not running. Program stops\n{ex}");
                return string.Empty;
            }
        }
    }
}

namespace TwinCAT3
{
    extern alias TwinCAT3Ads;

    using ADSLogging;
    using TwinCAT3Ads::TwinCAT.Ads;
    using TwinCAT.Ads;
    using TwinCAT.Ads.TypeSystem;
    using TwinCAT.TypeSystem;
    using TwinCAT3Ads::TwinCAT.Ads.TypeSystem;
    using TwinCAT;
    using TwinCAT3Ads::TwinCAT;

    public class Client : BaseClient<AdsClient, AdsNotificationEventArgs>
    {
        /// <summary>
        /// Initializes a new instance of the Client class.
        /// </summary>
        /// <param name="config">The configuration settings for the ADS client.</param>
        /// <param name="logger">The logger instance for logging ADS-related events.</param>
        public Client (Configuration config, Logger logger) : base(config, logger, new AdsClient()) { }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override void Dispose()
        {
            foreach (uint notificationHandle in notificationHandles)
            {
                client.DeleteDeviceNotification(notificationHandle);
            }
            client.AdsNotification -= HandleAdsNotificationEvent;
            client.Dispose();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><inheritdoc/></returns>
        public override bool StartConnection()
        {
            try
            {
                MessageHandler.LogStatus($"Trying to connect to target at: {config.AmsNetId} with Port: {config.TcPort}");
                client.Connect(config.AmsNetId, config.TcPort);
                AdsErrorCode adsErrorCode = client.TryReadState(out StateInfo stateInfo);

                if (client != null && adsErrorCode == AdsErrorCode.NoError)
                {
                    MessageHandler.LogStatus("Successfully connected to target");
                    client.AdsNotification += HandleAdsNotificationEvent;
                    return true;
                }
                else
                {
                    MessageHandler.LogError($"Can't find a target with AmsNetId: {config.AmsNetId} and Port: {config.TcPort}\nIs the PLC in Run mode?");
                    return false;
                }
            }
            catch (AdsErrorException adsError)
            {
                MessageHandler.LogError($"Can't connect to target. ADS error: {adsError.ErrorCode} - {adsError.Message}");
                return false;
            }
            catch (Exception e)
            {
                MessageHandler.LogError($"Fatal error. Program stops\n{e.Message}");
                return false;
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="sender"><inheritdoc/></param>
        /// <param name="e"><inheritdoc/></param>
        public override void HandleAdsNotificationEvent(object sender, AdsNotificationEventArgs e)
        {
            if (e.UserData is IAdsSymbol symbolInfo)
            {
                HandleEvent(e.Data, symbolInfo.DataTypeId.ToString(), symbolInfo.TypeName, symbolInfo.InstancePath);
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override void RegisterNotifications()
        {
            ISymbolLoader loader = SymbolLoaderFactory.Create(client, new SymbolLoaderSettings(SymbolsLoadMode.Flat));

            foreach (VariableConfig variableConfig in config.LoggingVariables)
            {
                try
                {
                    Symbol symbolInfo = (Symbol)loader.Symbols[variableConfig.SymbolPath];

                    if (notSupportedPlcOpenTypes.Contains(symbolInfo.TypeName) || symbolInfo.TypeName.Contains("WSTRING"))
                    {
                        MessageHandler.LogWarning($"The data type: {symbolInfo.TypeName} from the variable: {variableConfig.SymbolPath} is not supported.\nThis variable will be ignored!");
                        continue;
                    }

                    notificationHandles.Add(client.AddDeviceNotification(variableConfig.SymbolPath, symbolInfo.Size, NotificationSettings.Default ,symbolInfo));
                    MessageHandler.LogStatus($"Variable: {variableConfig.SymbolPath} successfully added to the logging");
                }
                catch (Exception e)
                {
                    MessageHandler.LogError($"Can't register variable {variableConfig.SymbolPath}\n{e.Message}");
                }
            }
        }

        /// <summary>
        /// Gets the AmsNetId from the specified string representation of AmsNetId.
        /// If the input is invalid, a warning is logged, and the local AmsNetId is used.
        /// </summary>
        /// <param name="amsNetId">The string representation of AmsNetId to be parsed.</param>
        /// <returns>The parsed AmsNetId or the local AmsNetId if parsing fails.</returns>
        public static string GetAmsNetId(string amsNetId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(amsNetId))
                {
                    MessageHandler.LogWarning("No valid AmsNetId found in configuration. Using the local AmsNetId");
                    return AmsNetId.Local.ToString();
                }
                else
                {
                    if (AmsNetId.TryParse(amsNetId, out AmsNetId parsedAmsNetId))
                        return parsedAmsNetId.ToString();
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                MessageHandler.LogError($"No AmsNetId found. Probably the TwinCAT System Service is not running. Program stops\n{ex}");
                return string.Empty;
            }
        }
    }
}