using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace Arma2NETAndroidPlugin
{
    class UDPConnection : UDP
    {
        private UdpClient udp_client = null;
        private IPEndPoint ip = null;
        private TCPThread tcp = null;
        private Thread tcpthread = null;

        public UDPConnection()
        {
            //Constructor
            if (udp_client == null) {
                SetupNetwork();
            }
        }

        public override IEnumerable<string[][]> SendData(string utility, string value, int maxResultSize)
        {
            if (udp_client == null) {
                SetupNetwork();
            }

            Logger.addMessage(Logger.LogType.Info, "Started SendData");

            if (udp_client != null) {
                //send the data over the network via UDP broadcast
                byte[] heartbeat = System.Text.Encoding.UTF8.GetBytes("Arma2NETAndroidPlugin");
                udp_client.Send(heartbeat, heartbeat.Length, ip);
                Logger.addMessage(Logger.LogType.Info, "Sent UDP heartbeat");
            }

            //add message to queue
            Message msg = new Message(utility, value);
            tcp.messages.Add(msg);

            yield break;
        }

        private void SetupNetwork()
        {
            //http://stackoverflow.com/questions/10832770/sending-udp-broadcast-receiving-multiple-messages
            //UDP broadcast does NOT leave the subnet

            if (udp_client == null)
            {
                try
                {
                    udp_client = new UdpClient();
                    //only send to local network
                    ip = new IPEndPoint(IPAddress.Parse("255.255.255.255"), 65041);
                }
                catch (Exception ex)
                {
                    Logger.addMessage(Logger.LogType.Error, "Unable to open connection." + ex.ToString());
                }
            }

            if (tcpthread == null)
            {
                Logger.addMessage(Logger.LogType.Info, "About to start TCP thread.");
                //http://msdn.microsoft.com/en-us/library/aa645740%28v=vs.71%29.aspx
                tcp = new TCPThread();
                tcpthread = new Thread(new ThreadStart(tcp.run));
                Logger.addMessage(Logger.LogType.Info, "TCP thread about to start.");
                tcpthread.Start();
                //wait for thread to become active
                while (!tcpthread.IsAlive);
                Logger.addMessage(Logger.LogType.Info, "TCP thread is active.");
            }
        }

        public void CloseConnection()
        {
            if (udp_client != null)
            {
                try
                {
                    udp_client.Close();
                }
                catch (Exception ex)
                {
                    Logger.addMessage(Logger.LogType.Error, "Unable to close connection." + ex.ToString());
                }
            }

            if (tcpthread != null && tcpthread.IsAlive)
            {
                tcpthread.Abort();
            }
        }
    }
}
