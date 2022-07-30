using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using VirtualRadio.Network;

namespace VirtualRadio.Server
{
    class Sender
    {
        bool running = true;
        Thread sendLoop;
        Mixer mixer;
        RtlOutput rtl;
        NetworkEndpoint network;
        Dictionary<IPEndPoint, Client> clients = new Dictionary<IPEndPoint, Client>();
        Tuple<IPEndPoint, Client> addClient;
        IPEndPoint removeClient;


        public Sender(Mixer mixer, NetworkEndpoint network, RtlOutput rtl)
        {
            this.mixer = mixer;
            this.network = network;
            this.rtl = rtl;
            sendLoop = new Thread(new ThreadStart(SendLoop));
            sendLoop.Name = "SendLoop";
            sendLoop.Start();
        }

        private void SendLoop()
        {
            double blocksPerTick = 250000.0 / (512.0 * TimeSpan.TicksPerSecond);
            long currentBlock = 0;
            long startTime = DateTime.UtcNow.Ticks;
            while (running)
            {
                Thread.Sleep(1);
                long currentTime = DateTime.UtcNow.Ticks;
                long timeDelta = currentTime - startTime;
                long targetBlock = (long)(timeDelta * blocksPerTick);
                while (currentBlock < targetBlock)
                {
                    byte[] sendBlock = mixer.GetBlockMessage(clients);
                    SendToAll(sendBlock);
                    currentBlock++;
                }
                //I don't think I am very worried about long running servers losing accuracy on dividing large numbers with long and double, but here we are...
                if (timeDelta > TimeSpan.TicksPerDay)
                {
                    startTime = currentTime;
                    currentBlock = 0;
                }
            }
        }

        private void SendToAll(byte[] sendBlock)
        {
            rtl.SendData(sendBlock);

            long currentTime = DateTime.UtcNow.Ticks;
            long heartbeatTime = currentTime - 2 * TimeSpan.TicksPerSecond;
            long disconnectTime = currentTime - 10 * TimeSpan.TicksPerSecond;

            if (addClient != null)
            {
                clients.Add(addClient.Item1, addClient.Item2);
                addClient = null;
            }

            foreach (KeyValuePair<IPEndPoint, Client> kvp in clients)
            {
                Client cs = kvp.Value;
                if (cs.lastReceive < disconnectTime)
                {
                    removeClient = kvp.Key;
                    continue;
                }
                if (cs.lastSend < heartbeatTime)
                {
                    byte[] heartbeat = MessageGenerator.WriteHeartbeat();
                    cs.lastSend = currentTime;
                    network.Enqueue(cs.endpoint, heartbeat);
                }
                if (cs.iq)
                {
                    cs.lastSend = currentTime;
                    network.Enqueue(cs.endpoint, sendBlock);
                }
            }

            if (removeClient != null)
            {
                clients.Remove(removeClient);
            }
        }

        public void AddClient(IPEndPoint endPoint, Client cs)
        {
            addClient = new Tuple<IPEndPoint, Client>(endPoint, cs);
        }

        public void RemoveClient(IPEndPoint endPoint)
        {
            removeClient = endPoint;
        }

        public void Stop()
        {
            running = false;
            sendLoop.Join();
        }
    }
}