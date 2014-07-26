using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arma2Net;
using System.IO;

namespace Arma2NETAndroidPlugin
{
    class Startup
    {
        public static Logger logger_object = null;
        public static Boolean started_up = false;
        public static UDPConnection udpconnection = null;

        public static void StartupConnection()
        {
            if (started_up == false)
            {
                //create appdata folder if it doesn't already exist
                var appDataLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Arma2NETAndroid");
                //check to see if the Arma2NETAndroid folder exists, if not create it
                if (!System.IO.Directory.Exists(appDataLocation))
                {
                    System.IO.Directory.CreateDirectory(appDataLocation);
                }

                //Start up logging
                logger_object = new Logger();
                Logger.addMessage(Logger.LogType.Info, "Logging started in directory: " + Logger.getLogDir());

                Logger.addMessage(Logger.LogType.Info, "Arma2NETAndroid Plugin Started.");

                //Use AssemblyInfo.cs version number
                //Holy cow this is confusing...
                //http://stackoverflow.com/questions/909555/how-can-i-get-the-assembly-file-version
                //http://all-things-pure.blogspot.com/2009/09/assembly-version-file-version-product.html
                //http://stackoverflow.com/questions/64602/what-are-differences-between-assemblyversion-assemblyfileversion-and-assemblyin
                Logger.addMessage(Logger.LogType.Info, "Version number: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());

                udpconnection = new UDPConnection();

                //set mutex so we know we've started everything up
                started_up = true;
            }
        }

        public static void Unload()
        {
            Logger.addMessage(Logger.LogType.Info, "Unloading plugin.");
            udpconnection.CloseConnection();
            Logger.Stop();
            Startup.started_up = false;
        }
    }
}
