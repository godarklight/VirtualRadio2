using System;
using System.Net;
using System.Text;
using VirtualRadio.Common;

namespace VirtualRadio.Network
{
    public static class MessageGenerator
    {
        private static void WriteHeader(MessageType messageType, byte[] input)
        {
            input[0] = 86;
            input[1] = 82;
            input[2] = 50;
            input[3] = (byte)messageType;
        }

        public static byte[] WriteHeartbeat()
        {
            byte[] retVal = new byte[4];
            WriteHeader(MessageType.HEARTBEAT, retVal);
            return retVal;
        }

        public static byte[] WriteConnectionEnd(string reason)
        {
            byte[] retVal;
            if (!string.IsNullOrEmpty(reason))
            {
                byte[] stringData = Encoding.UTF8.GetBytes(reason);
                retVal = new byte[4 + stringData.Length];
                WriteHeader(MessageType.CONNECTION_END, retVal);
                Array.Copy(stringData, 0, retVal, 4, stringData.Length);
            }
            else
            {
                retVal = new byte[4];
                WriteHeader(MessageType.CONNECTION_END, retVal);
            }
            return retVal;
        }

        public static byte[] WriteVFO(int frequency)
        {
            byte[] retVal = new byte[8];
            WriteHeader(MessageType.SET_VFO, retVal);
            frequency = IPAddress.HostToNetworkOrder(frequency);
            BitConverter.GetBytes(frequency).CopyTo(retVal, 4);
            return retVal;
        }

        public static byte[] WriteMode(ModeType mode)
        {
            byte[] retVal = new byte[5];
            WriteHeader(MessageType.SET_MODE, retVal);
            retVal[4] = (byte)mode;
            return retVal;
        }


        public static byte[] WritePower(float power)
        {
            byte[] u4 = BitConverter.GetBytes(power);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(u4);
            }
            byte[] retVal = new byte[8];
            WriteHeader(MessageType.SET_POWER, retVal);
            u4.CopyTo(retVal, 4);
            return retVal;
        }

        public static byte[] WriteRate(int rate)
        {
            byte[] retVal = new byte[8];
            WriteHeader(MessageType.SET_VFO, retVal);
            rate = IPAddress.HostToNetworkOrder(rate);
            BitConverter.GetBytes(rate).CopyTo(retVal, 4);
            return retVal;
        }

        public static byte[] WriteIQ(bool receiveIQ)
        {
            byte[] retVal = new byte[5];
            WriteHeader(MessageType.SET_MODE, retVal);
            retVal[4] = 0;
            if (receiveIQ)
            {
                retVal[4] = 1;
            }
            return retVal;
        }

        public static byte[] WriteData(ushort sequence)
        {
            byte[] retVal = new byte[1030];
            WriteHeader(MessageType.AUDIO_DATA, retVal);
            byte[] u2 = BitConverter.GetBytes(sequence);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(u2);
            }
            u2.CopyTo(retVal, 4);
            return retVal;
        }
    }
}