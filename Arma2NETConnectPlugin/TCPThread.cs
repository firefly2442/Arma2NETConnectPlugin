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
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace Arma2NETConnectPlugin
{
    class TCPThread
    {
        private TcpListener tcp_listener = null;
        public bool connected = false;
        //thread safe queue for adding/removing entries
        public BlockingCollection<string> messages = new BlockingCollection<string>(); //outgoing messages
        public static BlockingCollection<string> inbound_messages = new BlockingCollection<string>(); //inbound messages

        public TCPThread()
        {
            //constructor
            tcp_listener = new TcpListener(IPAddress.Any, 65042);
            tcp_listener.Start();
        }

        public void run()
        {
            //Logger.addMessage(Logger.LogType.Info, "Run thread, inside method.");
            byte[] rcvBuffer = new byte[16384]; // Receive buffer, 16KB (corresponds to callExtension limit in Arma)
            while (true)
            {
                // Run forever, accepting and servicing connections
                TcpClient client = null;
                NetworkStream netStream = null;
                try
                {
                    //this blocks until a connection is made
                    client = tcp_listener.AcceptTcpClient(); // Get client connection
                    netStream = client.GetStream();
                    connected = true;
                    //Logger.addMessage(Logger.LogType.Info, "Stream up.");
                    // Receive until client closes connection, indicated by 0 return value
                    int bytesRcvd;
                    String result = "";
                    while (((bytesRcvd = netStream.Read(rcvBuffer, 0, rcvBuffer.Length)) > 0)) {
                        result = result + System.Text.Encoding.UTF8.GetString(rcvBuffer, 0, rcvBuffer.Length);
                        if (result.Contains(".Arma2NETConnectEnd."))
                            break;
                    }
                    //Logger.addMessage(Logger.LogType.Info, "Finished reading in TCP.");

                    result = result.TrimEnd('\0'); //trim off null characters
                    result = result.Remove(result.Length - 20); //remove .Arma2NETConnectEnd.
                    Logger.addMessage(Logger.LogType.Info, "TCP message from Droid: " + result);
                    inbound_messages.Add(result);

                    //http://www.codethinked.com/blockingcollection-and-iproducerconsumercollection
                    int count = 0;
                    if (messages.Count() > 35) //send messages in batches, this way we don't send too much at once if there's a backlog
                        count = 35;
                    else
                        count = messages.Count();
                    while (count != 0)
                    {
                        // Send message back to Android
                        string msg = messages.Take();
                        //if we have multiple messages, we want to correctly parse them all, thus appending the data with an extra comma
                        byte[] byteBuffer = System.Text.Encoding.UTF8.GetBytes(msg+",");
                        netStream.Write(byteBuffer, 0, byteBuffer.Length);
                        netStream.Flush();
                        //Logger.addMessage(Logger.LogType.Info, "TCP message sent.");
                        count--;
                    }
                    //Logger.addMessage(Logger.LogType.Info, "TCP sending final message.");
                    byte[] finalBuffer = System.Text.Encoding.UTF8.GetBytes(".Arma2NETConnectEnd.");
                    netStream.Write(finalBuffer, 0, finalBuffer.Length);
                    netStream.Flush();
                    //Logger.addMessage(Logger.LogType.Info, "TCP final message sent.");

                    netStream.Close();
                    client.Close();
                }
                catch (IOException ex)
                {
                    Logger.addMessage(Logger.LogType.Warning, "TCP IOException." + ex.ToString());
                    connected = false;
                }
                catch (SocketException ex)
                {
                    Logger.addMessage(Logger.LogType.Warning, "TCP SocketException." + ex.ToString());
                    connected = false;
                }
                catch (Exception ex)
                {
                    Logger.addMessage(Logger.LogType.Error, "Unhandled TCP exception:" + ex.ToString());
                    connected = false;
                }
            }
        }
    }
}
