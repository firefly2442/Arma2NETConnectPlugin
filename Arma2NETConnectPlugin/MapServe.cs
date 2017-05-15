using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Arma2NETConnectPlugin
{

    public class MapServe
    {
        private TcpListener tcp_listener = null;
        private Thread mapservethread = null;

        public MapServe()
        {
            //constructor
            Logger.addMessage(Logger.LogType.Info, "MapServe constructor.");
            tcp_listener = new TcpListener(IPAddress.Any, 65043);
            tcp_listener.Start();
            mapservethread = new Thread(new ThreadStart(this.run));
            mapservethread.Start();
        }

        public void run()
        {
            Logger.addMessage(Logger.LogType.Info, "MapServe run thread, inside method.");
            byte[] rcvBuffer = new byte[32000]; //32 KB
            while (true)
            {
                // Run forever, accepting and servicing connections
                TcpClient client = null;
                NetworkStream netStream = null;
                try
                {
                    //this blocks until a connection is made
                    Logger.addMessage(Logger.LogType.Info, "MapServe - getting TCP client connection.");
                    client = tcp_listener.AcceptTcpClient(); // Get client connection
                    Logger.addMessage(Logger.LogType.Info, "MapServe - setting up stream.");
                    netStream = client.GetStream();

                    while (true) //try to keep the TCP socket connection open for as long as possible
                    {
                        Logger.addMessage(Logger.LogType.Info, "MapServe - Stream up.");
                        // Receive until client closes connection, indicated by 0 return value
                        int bytesRcvd;
                        String result = "";
                        bool getMapFiles = true;
                        while (((bytesRcvd = netStream.Read(rcvBuffer, 0, rcvBuffer.Length)) > 0))
                        {
                            result = result + System.Text.Encoding.UTF8.GetString(rcvBuffer, 0, rcvBuffer.Length);
                            if (result.Contains(".GetMapFiles.")) {
                                getMapFiles = true;
                                break;
                            } else if (result.Contains(".GetFile.")) {
                                getMapFiles = false;
                                break;
                            }
                        }
                        Logger.addMessage(Logger.LogType.Info, "MapServe - Finished reading in TCP.");

                        result = result.TrimEnd('\0'); //trim off null characters
                        result = result.Replace(".GetMapFiles.", "");
                        result = result.Replace(".GetFile.", "");

                        Logger.addMessage(Logger.LogType.Info, "MapServe - TCP message from Droid: " + result);

                        if (getMapFiles) {
                            Logger.addMessage(Logger.LogType.Info, "MapServe - starting to get map files.");
                            //get a list of all the directories and files that we will need to push
                            Logger.addMessage(Logger.LogType.Info, "MapServe - path: " + Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "maps"));

                            String[] allFolders = Directory.GetDirectories(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "maps"), "*.*", System.IO.SearchOption.AllDirectories);
                            String[] allFiles = Directory.GetFiles(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "maps"), "*.*", System.IO.SearchOption.AllDirectories);
                            var msg = "";
                            foreach (string s in allFolders) {
                                msg = msg + s + "\n";
                            }
                            foreach (string s in allFiles) {
                                msg = msg + s + "\n";
                            }

                            //replace the full paths so we just have relative paths
                            msg = msg.Replace(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "maps"), "");
                            Logger.addMessage(Logger.LogType.Info, "MapServe - msg: " + msg);

                            byte[] byteBuffer = System.Text.Encoding.UTF8.GetBytes(msg);
                            netStream.Write(byteBuffer, 0, byteBuffer.Length);
                            byte[] finalBuffer = System.Text.Encoding.UTF8.GetBytes(".GetMapFiles.");
                            netStream.Write(finalBuffer, 0, finalBuffer.Length);
                            netStream.Flush();
                            Logger.addMessage(Logger.LogType.Info, "MapServe - TCP final message sent.");
                        } else {

                        }

                        
                    }
                }
                catch (Exception ex)
                {
                    Logger.addMessage(Logger.LogType.Error, "Unhandled MapServe exception:" + ex.ToString());
                }
            }
        }
    }
}
