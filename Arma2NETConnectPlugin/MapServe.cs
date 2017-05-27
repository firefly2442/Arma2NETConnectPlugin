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

            Boolean run = true;
            while (run)
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
                        // Receive until client closes connection, indicated by 0 return value
                        int bytesRcvd;
                        String result = "";
                        String streamCase = "1";
                        byte[] rcvBuffer = new byte[32000]; //32 KB
                        while ((bytesRcvd = netStream.Read(rcvBuffer, 0, rcvBuffer.Length)) > 0)
                        {
                            result = result + System.Text.Encoding.UTF8.GetString(rcvBuffer, 0, rcvBuffer.Length);
                            if (result.Contains(".GetListSize.")) {
                                streamCase = "1";
                            } else if (result.Contains(".GetMapFiles.")) {
                                streamCase = "2";
                            } else if (result.Contains(".GetFile.")) {
                                streamCase = "3";
                            }
                            break;
                        }
                        if (result.Contains(".Shutdown.")) {
                            netStream.Close();
                            client.Close();
                            run = false;
                            Logger.addMessage(Logger.LogType.Info, "MapServe - Finished, shutting down.");
                            break;
                        }

                        //Logger.addMessage(Logger.LogType.Info, "MapServe - TCP message before culling: '" + result + "'");

                        result = result.TrimEnd('\0'); //trim off null characters
                        result = result.Replace(".GetListSize.", "");
                        result = result.Replace(".GetMapFiles.", "");
                        result = result.Replace(".GetFile.", "");

                        //Logger.addMessage(Logger.LogType.Info, "MapServe - TCP message from Droid: '" + result + "'");

                        if (streamCase == "1") {
                            var msg = getFilesList();
                            Logger.addMessage(Logger.LogType.Info, "MapServe - sent list length: " + msg.Length); //in string character length

                            byte[] sendSizeBuffer = System.Text.Encoding.UTF8.GetBytes(msg.Length.ToString());
                            netStream.Write(sendSizeBuffer, 0, sendSizeBuffer.Length);

                            byte[] finalMsgBuffer = System.Text.Encoding.UTF8.GetBytes(".GetListSize.");
                            netStream.Write(finalMsgBuffer, 0, finalMsgBuffer.Length);
                        } else if (streamCase == "2") {
                            Logger.addMessage(Logger.LogType.Info, "MapServe - starting to get map files.");
                            //get a list of all the directories and files that we will need to push
                            Logger.addMessage(Logger.LogType.Info, "MapServe - path: " + Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "maps"));

                            var msg = getFilesList();
                            //Logger.addMessage(Logger.LogType.Info, "MapServe - msg: '" + msg + "'");

                            byte[] byteBuffer = System.Text.Encoding.UTF8.GetBytes(msg);
                            Logger.addMessage(Logger.LogType.Info, "MapServe - Need to send file list bytes size:" + byteBuffer.Length);
                            netStream.Write(byteBuffer, 0, byteBuffer.Length);
                            netStream.Flush();
                            //Logger.addMessage(Logger.LogType.Info, "MapServe - TCP final message sent.");
                        } else if (streamCase == "3") {
                            //send file
                            result = result.Replace("/", "\\");
                            var filepath = Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)+@"\maps"+result); //@ denotes string literal

                            //https://www.codeproject.com/Articles/32633/Sending-Files-using-TCP
                            Stream fileStream = File.OpenRead(filepath);
                            byte[] sendFileBuffer = new byte[32000]; //32 KB
                            int numPackets = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(fileStream.Length) / Convert.ToDouble(sendFileBuffer.Length)));
                            int TotalLength = (int)fileStream.Length;
                            Logger.addMessage(Logger.LogType.Info, "MapServe - file: " + filepath + " size: " + TotalLength);
                            int CurrentPacketLength;
                            byte[] SendingBuffer;
                            for (int i = 0; i < numPackets; i++) {
                                //Logger.addMessage(Logger.LogType.Info, "MapServe - number packets to send: " + (numPackets-i));
                                if (TotalLength > sendFileBuffer.Length) {
                                    CurrentPacketLength = sendFileBuffer.Length;
                                    TotalLength = TotalLength - CurrentPacketLength;
                                }
                                else {
                                    CurrentPacketLength = TotalLength;
                                }
                                SendingBuffer = new byte[CurrentPacketLength];
                                fileStream.Read(SendingBuffer, 0, CurrentPacketLength);
                                netStream.Write(SendingBuffer, 0, (int)SendingBuffer.Length);
                                netStream.Flush();
                            }
                            fileStream.Close();
                            //Logger.addMessage(Logger.LogType.Info, "MapServe - finished sending file!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.addMessage(Logger.LogType.Error, "Unhandled MapServe exception:" + ex.ToString());
                }
            }
        }



        private String getFilesList()
        {
            String[] allFolders = Directory.GetDirectories(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "maps"), "*.*", System.IO.SearchOption.AllDirectories);
            String[] allFiles = Directory.GetFiles(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "maps"), "*.*", System.IO.SearchOption.AllDirectories);
            var msg = "";
            foreach (string s in allFolders) {
                msg = msg + s + "\n";
            }
            foreach (string s in allFiles) {
                if (s.EndsWith(".png") || s.EndsWith(".txt")) {
                    long sSize = new System.IO.FileInfo(s).Length; //get filesize in bytes
                    msg = msg + sSize + "\t" + s + "\n";
                }
            }

            //replace the full paths so we just have relative paths
            msg = msg.Replace(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "maps"), "");

            return msg;
        }
    }
}
