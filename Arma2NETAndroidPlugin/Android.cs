/*
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Arma2Net;

namespace Arma2NETAndroidPlugin
{
    //the function name for the plugin (called from Arma side)
    [Addin("Arma2NETAndroid", Version = "0.1.0.0", Author = "firefly2442", Description = "Passes information from Arma to Android.")]
    public class Arma2NETAndroid : Addin
    {
        //AsyncAddIn - when you want to pass data from the game and immediately return null
        // then, subsequent checks by the game check to see if the data can be returned.
        //On the SQF side, this means that we can only do one call at a time...

        public Arma2NETAndroid()
        {
            InvocationMethod = new AsyncAddinInvocationMethod(this);
        }

        //This method is called when callExtension is used from SQF:
        public override string Invoke(string args, int maxResultSize)
        {
            //if we haven't setup the connection yet, this will do it
            Startup.StartupConnection();

            IList<object> arguments;
            if (Format.TrySqfAsCollection(args, out arguments) && arguments[0] != null)
            {
                string value = "";
                foreach (string arg in arguments)
                    value = value + arg + ",";
                value = value.TrimEnd(','); //trim tail comma

                Logger.addMessage(Logger.LogType.Info, "Received: " + value);

                //send the UDP heartbeat and then send the data over TCP
                Startup.udpconnection.SendData(value, maxResultSize);

                string returned = "Empty";
                //pull from the collection to see if we have anything to return
                if (TCPThread.inbound_messages.Count > 0) {
                    returned = TCPThread.inbound_messages.Take();
                }

                //We need to return something even if there's nothing to return, so we just return an empty array if we're not ready yet.
                //if (returned.ToString() == "")
                    //return Format.ObjectAsSqf("[]");
                return Format.ObjectAsSqf(returned);
            }
            else
            {
                Logger.addMessage(Logger.LogType.Error, "The number and/or format of the arguments passed in doesn't match.");
                throw new ArgumentException();
            }
        }
    }
}
