using System;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using VirtualRadio.Common;
using VirtualRadio.Network;


namespace VirtualRadio.Server
{
    class Mixer
    {
        private Complex[] sendBlock = new Complex[512];
        ushort sequence = 0;
        private void GetBlock(byte[] input, Dictionary<IPEndPoint, Client> clients)
        {
            Array.Clear(sendBlock);

            foreach (KeyValuePair<IPEndPoint, Client> client in clients)
            {
                Complex[] clientComplex = client.Value.WriteComplexBlock(sendBlock);
            }

            FormatConvert.ComplexToIQData(sendBlock, input);
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