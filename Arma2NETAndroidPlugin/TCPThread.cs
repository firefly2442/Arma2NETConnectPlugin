using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace Arma2NETAndroidPlugin
{
    class Message
    {
        public string utility;
        public string value;

        public Message(string utility, string value)
        {
            //constructor
            this.utility = utility;
            this.value = value;
        }
    }

    class TCPThread
    {
        private TcpListener tcp_listener = null;
        //thread safe queue for adding/removing entries
        public BlockingCollection<Message> messages = new BlockingCollection<Message>();

        public TCPThread()
        {
            //constructor
            tcp_listener = new TcpListener(IPAddress.Any, 65042);
            tcp_listener.Start();
        }

        public void run()
        {
            Logger.addMessage(Logger.LogType.Info, "Run thread, inside method.");
            byte[] rcvBuffer = new byte[16384]; // Receive buffer, 16KB (corresponds to callExtension limit in Arma)
            while (true)
            {
                // Run forever, accepting and servicing connections
                TcpClient client = null;
                NetworkStream netStream = null;
                try
                {
                    client = tcp_listener.AcceptTcpClient(); // Get client connection
                    netStream = client.GetStream();
                    Logger.addMessage(Logger.LogType.Info, "Stream up.");
                    // Receive until client closes connection, indicated by 0 return value
                    netStream.Read(rcvBuffer, 0, rcvBuffer.Length);

                    String result = System.Text.Encoding.UTF8.GetString(rcvBuffer, 0, rcvBuffer.Length);
                    result = result.TrimEnd('\0'); //trim off null characters
                    Logger.addMessage(Logger.LogType.Info, "TCP message: " + result);

                    //http://www.codethinked.com/blockingcollection-and-iproducerconsumercollection
                    foreach (Message msg in messages.GetConsumingEnumerable())
                    {
                        // Send message back to Android
                        string send_message = msg.utility + "," + msg.value;
                        byte[] byteBuffer = System.Text.Encoding.UTF8.GetBytes(send_message);
                        netStream.Write(byteBuffer, 0, byteBuffer.Length);
                        netStream.Flush();
                        Logger.addMessage(Logger.LogType.Info, "TCP message sent.");
                    }
                }
                catch (Exception ex)
                {
                    Logger.addMessage(Logger.LogType.Error, "Unable to close connection." + ex.ToString());
                }
            }
        }
    }
}
