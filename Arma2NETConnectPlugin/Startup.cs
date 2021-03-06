﻿/*
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.IO;

namespace Arma2NETConnectPlugin
{
    class Startup
    {
        public static Logger logger_object = null;
        public static Boolean started_up = false;
        public static UDPConnection udpconnection = null;
        public static MapServe mapconnection = null;

        public static void StartupConnection()
        {
            if (started_up == false)
            {
                //create appdata folder if it doesn't already exist
                var appDataLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Arma2NETConnect");
                //check to see if the Arma2NETConnect folder exists, if not create it
                if (!System.IO.Directory.Exists(appDataLocation))
                {
                    System.IO.Directory.CreateDirectory(appDataLocation);
                }

                //Start up logging
                logger_object = new Logger();
                Logger.addMessage(Logger.LogType.Info, "Logging started in directory: " + Logger.getLogDir());

                Logger.addMessage(Logger.LogType.Info, "Arma2NETConnect Plugin Started.");

                //Use AssemblyInfo.cs version number
                //Holy cow this is confusing...
                //http://stackoverflow.com/questions/909555/how-can-i-get-the-assembly-file-version
                //http://all-things-pure.blogspot.com/2009/09/assembly-version-file-version-product.html
                //http://stackoverflow.com/questions/64602/what-are-differences-between-assemblyversion-assemblyfileversion-and-assemblyin
                Logger.addMessage(Logger.LogType.Info, "Version number: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());

                udpconnection = new UDPConnection();

                mapconnection = new MapServe();

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
