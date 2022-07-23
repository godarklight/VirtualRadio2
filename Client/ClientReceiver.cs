using System;
using System.Collections.Generic;
using System.Net;
using VirtualRadio.Common;
using VirtualRadio.Network;

namespace VirtualRadio.Client
{
    class ClientReceiver
    {
        ClientSender clientSender;
        public ClientReceiver(ClientSender sender, NetworkHandler handler)
        {
            handler.HandleHeartbeat = HandleHeartbeat;
            handler.HandleConnectionEnd = HandleConnectionEnd;
            handler.HandleSetVFO = HandleSetVFO;
            handler.HandleSetPower = HandleSetPower;
            handler.HandleSetIQ = HandleSetIQ;
            handler.HandleSetMode = HandleSetMode;
            handler.HandleData = HandleData;
            this.clientSender = sender;
        }

        private void HandleHeartbeat(IPEndPoint endPoint)
        {
        }

        private void HandleConnectionEnd(IPEndPoint endPoint, string reason)
        {
        }

        private void HandleSetVFO(IPEndPoint endPoint, int frequency)
        {
        }

        private void HandleSetPower(IPEndPoint endPoint, double power)
        {
        }

        private void HandleSetMode(IPEndPoint endPoint, ModeType mode)
        {
        }

        private void HandleSetRate(IPEndPoint endPoint, int rate)
        {
        }

        private void HandleSetIQ(IPEndPoint endPoint, bool iq)
        {
        }

        private void HandleData(IPEndPoint endPoint, ushort sequence, byte[] data)
        {
        }
    }
}