using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;

namespace ADSLogging
{
    public enum TwinCATVersion
    {
        TwinCAT2,
        TwinCAT3
    }

    public class Program
    {
        private static readonly ManualResetEvent exitEvent = new ManualResetEvent(false);
        private const string TC2_RESOURCE_PATH = "costura64.twincat2.ads.dll.compressed";
        private const string TC3_RESOURCE_PATH = "costura64.twincat3.ads.dll.compressed";
        private const string TC_ASSEMBLY_NAME = "TwinCAT.Ads";
        private const string VERSION = "1.0.0";

        public static void Main(string[] args)
        {
            // Add Eventhandler to the current Appdomain to dynamically load the required assembly on runtime
            AppDomain appDomain = AppDomain.CurrentDomain;
            appDomain.AssemblyResolve += AssemblyResolve;

            try
            {
                Console.WriteLine($"############### ADS-Logging version {VERSION} ###############");
                MessageHandler.LogStatus("Reading configuration...");

                StartUp();

                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    MessageHandler.LogStatus("Exiting...");
                    exitEvent.Set();
                };

                exitEvent.WaitOne();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Initializes the application and starts the ADS client based on the configuration settings.
        /// </summary>
        private static void StartUp() 
        {
            Configuration config = new Configuration();
            Logger logger = !config.Error ? new Logger(config) : null;

            if (logger is null)
            {
                Console.WriteLine("Logger initialization failed. Program stopped.");
                return;
            }
                
            if (config.TwinCATVersion == TwinCATVersion.TwinCAT2)
            {
                LoadTC2(config, logger);
            }
            else
            {
                LoadTC3(config, logger);
            }
        }

        private static void LoadTC2(Configuration config, Logger logger)
        {
            TwinCAT2.Client client = new TwinCAT2.Client(config, logger);
            if (client.StartConnection())
                client.RegisterNotifications();
            else
                Console.WriteLine("Program stopped. Press Ctrl+C to exit...");
        }

        private static void LoadTC3(Configuration config, Logger logger)
        {
            TwinCAT3.Client client = new TwinCAT3.Client(config, logger);
            if (client.StartConnection())
                client.RegisterNotifications();
            else
                Console.WriteLine("Program is running. Press Ctrl+C to exit...");
        }

        /// <summary>
        /// Handles the AssemblyResolve event to dynamically load required assemblies.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The event data.</param>
        /// <returns>The loaded assembly if successful, otherwise null.</returns>
        private static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName assemblyName = new AssemblyName(args.Name);
            string resourcePath = string.Empty;
            

            if (assemblyName.Name == TC_ASSEMBLY_NAME)
            {
                if (assemblyName.Version.Major == 6)
                    resourcePath = TC3_RESOURCE_PATH;
                else
                    resourcePath = TC2_RESOURCE_PATH;
            }

            byte[] compressedAssemblyBytes = LoadEmbeddedAssembly(resourcePath);
            if (compressedAssemblyBytes != null)
            {
                byte[] decompressedAssemblyBytes = DecompressAssembly(compressedAssemblyBytes);

                Assembly loadedAssembly = Assembly.Load(decompressedAssemblyBytes);


                return loadedAssembly;
            }

            return null;
        }

        /// <summary>
        /// Loads the embedded assembly from the resources.
        /// </summary>
        /// <param name="resourcePath">The resource path of the embedded assembly.</param>
        /// <returns>The byte array containing the embedded assembly if successful, otherwise null.</returns>
        private static byte[] LoadEmbeddedAssembly(string resourcePath)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePath))
            {
                if (stream != null)
                {
                    byte[] assemblyBytes = new byte[stream.Length];
                    stream.Read(assemblyBytes, 0, assemblyBytes.Length);

                    return assemblyBytes;
                }
            }

            return null;
        }

        /// <summary>
        /// Decompresses the compressed assembly byte array.
        /// </summary>
        /// <param name="compressedBytes">The compressed assembly byte array to decompress.</param>
        /// <returns>The decompressed byte array.</returns>
        private static byte[] DecompressAssembly(byte[] compressedBytes)
        {
            using (MemoryStream compressedStream = new MemoryStream(compressedBytes))
            {
                using (MemoryStream decompressedStream = new MemoryStream())
                {
                    using (DeflateStream deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
                    {
                        deflateStream.CopyTo(decompressedStream);
                    }

                    return decompressedStream.ToArray();
                }
            }
        }
    }
}
