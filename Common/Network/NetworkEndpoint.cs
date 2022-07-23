using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VirtualRadio.Network
{
    public class NetworkEndpoint
    {
        bool running = true;
        Thread receiveLoop;
        Thread sendLoop;
        UdpClient udpc;
        AutoResetEvent are = new AutoResetEvent(false);
        NetworkHandler handler;
        ConcurrentQueue<Tuple<IPEndPoint, byte[]>> sendQueue = new ConcurrentQueue<Tuple<IPEndPoint, byte[]>>();
        public NetworkEndpoint(NetworkHandler handler, int port)
        {
            this.handler = handler;
            IPEndPoint listenAddr = new IPEndPoint(IPAddress.IPv6Any, port);
            udpc = new UdpClient(listenAddr);
            receiveLoop = new Thread(new ThreadStart(ReceiveLoop));
            receiveLoop.Name = "NetworkEndpoint-ReceiveLoop";
            receiveLoop.Start();
            sendLoop = new Thread(new ThreadStart(SendLoop));
            sendLoop.Name = "NetworkEndpoint-SendLoop";
            sendLoop.Start();
        }

        public void Stop()
        {
            running = false;
            udpc.Close();
            receiveLoop.Join();
            sendLoop.Join();
        }

        public void Enqueue(IPEndPoint endpoint, byte[] buffer)
        {
            sendQueue.Enqueue(new Tuple<IPEndPoint, byte[]>(endpoint, buffer));
            are.Set();
        }

        private void ReceiveLoop()
        {
            while (running)
            {
                try
                {
                    IPEndPoint recvFrom = new IPEndPoint(IPAddress.IPv6Any, 0);
                    byte[] receivedBytes = udpc.Receive(ref recvFrom);
                    handler.HandlePacket(recvFrom, receivedBytes);
                }
                catch (SocketException se)
                {
                    Console.WriteLine($"Error in receive loop: {se.ToString()}");
                }
            }
        }

        private void SendLoop()
        {
            while (running)
            {
                are.WaitOne(50);
                Tuple<IPEndPoint, byte[]> sendPacket;
                while (sendQueue.TryDequeue(out sendPacket))
                {
                    udpc.Send(sendPacket.Item2, sendPacket.Item2.Length, sendPacket.Item1);
                }
            }
        }
    }
}