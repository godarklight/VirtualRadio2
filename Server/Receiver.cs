using System;
using System.Collections.Generic;
using System.Net;
using VirtualRadio.Common;
using VirtualRadio.Network;

namespace VirtualRadio.Server
{
    class Receiver
    {
        Dictionary<IPEndPoint, Client> clients = new Dictionary<IPEndPoint, Client>();
        Sender sender;
        public Receiver(Sender sender, NetworkHandler handler)
        {
            handler.HandleHeartbeat = HandleHeartbeat;
            handler.HandleConnectionEnd = HandleConnectionEnd;
            handler.HandleSetVFO = HandleSetVFO;
            handler.HandleSetPower = HandleSetPower;
            handler.HandleSetIQ = HandleSetIQ;
            handler.HandleSetMode = HandleSetMode;
            handler.HandleData = HandleData;
            this.sender = sender;
        }

        private void HandleHeartbeat(IPEndPoint endPoint)
        {
            if (!clients.ContainsKey(endPoint))
            {
                Console.WriteLine($"Client connected: {endPoint}");
                Client cs = new Client();
                cs.endpoint = endPoint;
                clients.Add(endPoint, cs);
                sender.AddClient(endPoint, cs);
            }
        }

        private void HandleConnectionEnd(IPEndPoint endPoint, string reason)
        {
            Console.WriteLine($"Client disconnected: {endPoint}");
            if (clients.ContainsKey(endPoint))
            {
                clients.Remove(endPoint);
            }
            sender.RemoveClient(endPoint);
        }

        private void HandleSetVFO(IPEndPoint endPoint, int frequency)
        {
            if (clients.ContainsKey(endPoint))
            {
                Console.WriteLine($"Client frequency: {endPoint} = {frequency}");
                Client cs = clients[endPoint];
                cs.frequency = frequency;
                cs.lastReceive = DateTime.UtcNow.Ticks;
            }
        }

        private void HandleSetPower(IPEndPoint endPoint, double power)
        {
            if (clients.ContainsKey(endPoint))
            {
                Console.WriteLine($"Client power: {endPoint} = {power}");
                Client cs = clients[endPoint];
                cs.power = power;
                cs.lastReceive = DateTime.UtcNow.Ticks;
            }
        }

        private void HandleSetMode(IPEndPoint endPoint, ModeType mode)
        {
            if (clients.ContainsKey(endPoint))
            {
                Console.WriteLine($"Client mode: {endPoint} = {mode}");
                Client cs = clients[endPoint];
                cs.mode = mode;
                cs.lastReceive = DateTime.UtcNow.Ticks;
            }
        }

        private void HandleSetRate(IPEndPoint endPoint, int rate)
        {
            if (clients.ContainsKey(endPoint))
            {
                Console.WriteLine($"Client rate: {endPoint} = {rate}");
                Client cs = clients[endPoint];
                cs.rate = rate;
                cs.lastReceive = DateTime.UtcNow.Ticks;
            }
        }

        private void HandleSetIQ(IPEndPoint endPoint, bool iq)
        {
            if (clients.ContainsKey(endPoint))
            {
                Console.WriteLine($"Client IQ: {endPoint} = {iq}");
                Client cs = clients[endPoint];
                cs.iq = iq;
                cs.lastReceive = DateTime.UtcNow.Ticks;
            }
        }


        private void HandleData(IPEndPoint endPoint, ushort sequence, byte[] data)
        {
            if (clients.ContainsKey(endPoint))
            {
                Client cs = clients[endPoint];
                cs.buffer.Write(data, sequence);
                cs.lastReceive = DateTime.UtcNow.Ticks;
            }
        }
    }
}