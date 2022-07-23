using System;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using VirtualRadio.Network;
using System.Threading.Tasks;

namespace VirtualRadio.Server
{
    class Mixer
    {
        Random random = new Random();
        private Complex[] sendBlock = new Complex[256];
        ushort sequence = 0;
        private void GetBlock(byte[] input, Dictionary<IPEndPoint, Client> clients)
        {
            //Noise
            for (int i = 0; i < sendBlock.Length; i++)
            {
                double noise = (random.NextDouble() * 2.0 - 1.0) * 0.01;
                sendBlock[i] = noise;
            }

            foreach (KeyValuePair<IPEndPoint, Client> client in clients)
            {
                Complex[] clientComplex = client.Value.GetComplexBlock();
                for (int i = 0; i < clientComplex.Length; i++)
                {
                    sendBlock[i] = sendBlock[i] + clientComplex[i];
                }
            }

            for (int i = 0; i < sendBlock.Length; i++)
            {
                double iSample = Math.Clamp(sendBlock[i].Real, -1, 1);
                double qSample = Math.Clamp(sendBlock[i].Imaginary, -1, 1);
                byte iByte = (byte)(iSample * 127 + 128);
                byte qByte = (byte)(qSample * 127 + 128);
                input[6 + i * 2] = iByte;
                input[6 + i * 2 + 1] = qByte;
            }
        }

        public byte[] GetBlockMessage(Dictionary<IPEndPoint, Client> clients)
        {
            byte[] retVal = MessageGenerator.WriteData(sequence);
            sequence++;
            GetBlock(retVal, clients);
            return retVal;
        }
    }
}