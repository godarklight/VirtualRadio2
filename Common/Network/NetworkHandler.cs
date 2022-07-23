using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VirtualRadio.Common;

namespace VirtualRadio.Network
{
    public class NetworkHandler
    {
        public Action<IPEndPoint> HandleHeartbeat;
        public Action<IPEndPoint, string> HandleConnectionEnd;
        public Action<IPEndPoint, int> HandleSetVFO;
        public Action<IPEndPoint, double> HandleSetPower;
        public Action<IPEndPoint, ModeType> HandleSetMode;
        public Action<IPEndPoint, bool> HandleSetIQ;
        public Action<IPEndPoint, ushort, byte[]> HandleData;

        byte[] u2 = new byte[2];
        byte[] u4 = new byte[4];

        public void HandlePacket(IPEndPoint endpoint, byte[] buffer)
        {
            if (buffer.Length < 3)
            {
                return;
            }
            //VR2 magic header check
            if (buffer[0] != 86 || buffer[1] != 82 || buffer[2] != 50)
            {
                return;
            }
            MessageType type = (MessageType)buffer[3];
            switch (type)
            {
                case MessageType.HEARTBEAT:
                    HandleHeartbeat(endpoint);
                    break;
                case MessageType.CONNECTION_END:
                    string reason = null;
                    if (buffer.Length > 4)
                    {
                        reason = Encoding.UTF8.GetString(buffer, 4, buffer.Length - 4);
                    }
                    HandleConnectionEnd(endpoint, reason);
                    break;
                case MessageType.SET_VFO:
                    if (buffer.Length != 8)
                    {
                        return;
                    }
                    int freq = BitConverter.ToInt32(buffer, 4);
                    freq = IPAddress.NetworkToHostOrder(freq);
                    HandleSetVFO(endpoint, freq);
                    break;
                case MessageType.SET_POWER:
                    if (buffer.Length != 8)
                    {
                        return;
                    }
                    Array.Copy(buffer, 4, u4, 0, 4);
                    //Endian flip
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(u4);
                    }
                    double power = BitConverter.ToSingle(u4, 0) / 1000.0;
                    if (power > 0.25)
                    {
                        power = 0.25;
                    }
                    HandleSetPower(endpoint, power);
                    break;
                case MessageType.SET_MODE:
                    if (buffer.Length != 5)
                    {
                        return;
                    }
                    HandleSetMode(endpoint, (ModeType)buffer[4]);
                    break;
                case MessageType.SET_RATE:
                    if (buffer.Length != 8)
                    {
                        return;
                    }
                    int rate = BitConverter.ToInt32(buffer, 4);
                    rate = IPAddress.NetworkToHostOrder(rate);
                    HandleSetVFO(endpoint, rate);
                    break;
                case MessageType.SET_IQ:
                    if (buffer.Length != 5)
                    {
                        return;
                    }
                    HandleSetIQ(endpoint, buffer[4] != 0);
                    break;
                case MessageType.DATA:
                    if (buffer.Length != 518)
                    {
                        return;
                    }
                    Array.Copy(buffer, 4, u2, 0, 2);
                    //Endian flip
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(u2);
                    }
                    ushort sequence = BitConverter.ToUInt16(u2, 0);
                    byte[] temp = new byte[512];
                    Buffer.BlockCopy(buffer, 6, temp, 0, 512);
                    HandleData(endpoint, sequence, temp);
                    break;
            }
        }
    }
}