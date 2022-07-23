using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace VirtualRadio.Server
{
    class RtlOutput
    {
        TcpListener server;
        List<TcpClient> clients = new List<TcpClient>();
        byte[] rtlDongleInfo = new byte[12];


        public RtlOutput()
        {
            server = new TcpListener(new IPEndPoint(IPAddress.IPv6Any, 1234));
            server.Server.DualMode = true;
            server.Start();
            server.BeginAcceptTcpClient(HandleConnect, null);
            rtlDongleInfo[0] = 82;
            rtlDongleInfo[1] = 84;
            rtlDongleInfo[2] = 76;
            rtlDongleInfo[3] = 48;
        }

        private void HandleConnect(IAsyncResult ar)
        {
            TcpClient client = server.EndAcceptTcpClient(ar);
            Console.WriteLine("Connected TCP client");
            client.GetStream().Write(rtlDongleInfo, 0, rtlDongleInfo.Length);
            lock (clients)
            {
                clients.Add(client);
            }
            server.BeginAcceptTcpClient(HandleConnect, server);
        }

        public void SendData(byte[] data)
        {
            lock (clients)
            {
                TcpClient removeClient = null;
                foreach (TcpClient client in clients)
                {
                    try
                    {
                        client.GetStream().Write(data, 6, data.Length - 6);
                    }
                    catch
                    {
                        Console.WriteLine("Disconnected TCP client");
                        removeClient = client;
                    }
                }
                if (removeClient != null)
                {
                    clients.Remove(removeClient);
                }
            }
        }
    }
}