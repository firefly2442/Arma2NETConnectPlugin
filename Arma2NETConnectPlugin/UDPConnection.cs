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
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace Arma2NETConnectPlugin
{
    class UDPConnection : UDP
    {
        private UdpClient udp_client = null;
        private IPEndPoint ip = null;
        private IPEndPoint ip_local = null;
        private TCPThread tcp = null;
        private Thread tcpthread = null;

        public UDPConnection()
        {
            //Constructor
            if (udp_client == null) {
                SetupNetwork();
            }
        }

        public override void SendData(string value, int maxResultSize)
        {
            if (udp_client == null) {
                SetupNetwork();
            }

            Logger.addMessage(Logger.LogType.Info, "Started SendData");

            if (udp_client != null) {
                Logger.addMessage(Logger.LogType.Info, "Sending UDP heartbeat");
                //send the data over the network via UDP broadcast
                byte[] heartbeat = System.Text.Encoding.UTF8.GetBytes("Arma2NETConnectPlugin");
                udp_client.Send(heartbeat, heartbeat.Length, ip);
                udp_client.Send(heartbeat, heartbeat.Length, ip_local);
                Logger.addMessage(Logger.LogType.Info, "Sent UDP heartbeat");
            }


            //add message to queue only if the TCP connection is up
            if (tcp.connected) {
                tcp.messages.Add(value);
            }
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
                    //only send to local network, try both just in case the router is blocking 255.255.255.255
                    ip = new IPEndPoint(IPAddress.Parse("255.255.255.255"), 65041);
                    ip_local = new IPEndPoint(getBroadcastAddress(), 65041);
                }
                catch (Exception ex)
                {
                    Logger.addMessage(Logger.LogType.Error, "Unable to open UDP connection." + ex.ToString());
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

        public static IPAddress getBroadcastAddress()
        {
            //http://blogs.msdn.com/b/knom/archive/2008/12/31/ip-address-calculations-with-c-subnetmasks-networks.aspx
            //https://stackoverflow.com/questions/6803073/get-local-ip-address-c-sharp
            IPAddress address = null;

            //finds the local IP address
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    address = ip;
                    Logger.addMessage(Logger.LogType.Info, "Local IP address: " + address.ToString());
                    break;
                }
            }

            IPAddress subnetMask = IPAddress.Parse("255.255.255.0");

            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                Logger.addMessage(Logger.LogType.Error, "Lengths of IP address and subnet mask do not match.");

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
            }
            IPAddress bcast = new IPAddress(broadcastAddress);
            Logger.addMessage(Logger.LogType.Info, "Using broadcast address: " + bcast.ToString());
            return bcast;
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
