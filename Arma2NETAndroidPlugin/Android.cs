using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Arma2Net.AddInProxy;

namespace Arma2NETAndroidPlugin
{
    //the function name for the plugin (called from Arma side)
    [AddIn("Arma2NETAndroid", Version = "0.1.0.0", Publisher = "firefly2442", Description = "Passes information from Arma to Android.")]
    public class Arma2NETAndroid : AsyncAddIn
    {
        //AsyncAddIn - when you want to pass data from the game and immediately return null
        // then, subsequent checks by the game check to see if the data can be returned.
        //On the SQF side, this means that we can only do one call at a time...


        //This method is called when callExtension is used from SQF:
        public override string InvokeAsync(string args, int maxResultSize, CancellationToken token)
        {
            //if we haven't setup the connection yet, this will do it
            Startup.StartupConnection();

            IList<object> arguments;
            if (Format.TrySqfAsCollection(args, out arguments) && arguments.Count == 2 && arguments[0] != null && arguments[1] != null)
            {
                string utility = arguments[0] as string;
                string value = arguments[1] as string;

                Logger.addMessage(Logger.LogType.Info, "Received - Utility: " + utility + " Value: " + value);

                IEnumerable<string[][]> returned = Startup.udpconnection.SendData(utility, value, maxResultSize);

                //We need to return something even if there's nothing to return, so we just return an empty array if we're not ready yet.
                if (returned.ToString() == "")
                    return Format.ObjectAsSqf("[]");
                return Format.ObjectAsSqf(returned);
            }
            else
            {
                Logger.addMessage(Logger.LogType.Error, "The number and/or format of the arguments passed in doesn't match.");
                throw new ArgumentException();
            }
        }

        public override void Unload()
        {
            Startup.Unload();
        }
    }
}
