using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using VirtualRadio.Common;
using VirtualRadio.Network;


using System.IO;

namespace VirtualRadio.Client
{
    class ClientSender
    {
        IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.IPv6Loopback, 5973);
        bool running = true;
        Thread sendLoop;
        NetworkEndpoint network;
        ConcurrentQueue<byte[]> sendAudio = new ConcurrentQueue<byte[]>();
        int oldFreq = 14100;
        LowPassFilter lpfSSB = new LowPassFilter(2700, 44100);
        LowPassFilter lpfAM = new LowPassFilter(9000, 44100);
        ModeType mode = ModeType.USB;
        double[] outputRaw = new double[512];
        double[] outputFiltered = new double[512];


        public ClientSender(NetworkEndpoint network, string serverHostname, int serverPort)
        {
            IPHostEntry iphe = Dns.GetHostEntry(serverHostname, System.Net.Sockets.AddressFamily.InterNetworkV6);
            serverEndpoint = new IPEndPoint(iphe.AddressList[0], 5973);
            this.network = network;
            sendLoop = new Thread(new ThreadStart(SendLoop));
            sendLoop.Name = "Client-SendLoop";
            sendLoop.Start();
        }

        private void SendLoop()
        {
            Random random = new Random();

            byte[] heartbeat = MessageGenerator.WriteHeartbeat();
            network.Enqueue(serverEndpoint, heartbeat);

            double blocksPerTick = 44100.0 / (512.0 * TimeSpan.TicksPerSecond);
            long currentBlock = 0;
            long startTime = DateTime.UtcNow.Ticks;
            ushort sequence = 0;

            while (running)
            {
                Thread.Sleep(1);
                long currentTime = DateTime.UtcNow.Ticks;
                long timeDelta = currentTime - startTime;
                long targetBlock = (long)(timeDelta * blocksPerTick);
                while (currentBlock < targetBlock)
                {

                    byte[] audioData = null;

                    if (sendAudio.TryDequeue(out audioData))
                    {
                        byte[] sendBlock = MessageGenerator.WriteData(sequence);
                        Buffer.BlockCopy(audioData, 0, sendBlock, 6, audioData.Length);
                        sequence++;
                        network.Enqueue(serverEndpoint, sendBlock);
                    }

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

        public void SetMode(ModeType mode)
        {
            this.mode = mode;
            byte[] fmMode = MessageGenerator.WriteMode(mode);
            network.Enqueue(serverEndpoint, fmMode);
        }

        public int SetVFO(string newFreq)
        {
            int parseInt;
            if (Int32.TryParse(newFreq, out parseInt))
            {
                oldFreq = parseInt;
                byte[] vfo = MessageGenerator.WriteVFO(parseInt * 1000);
                network.Enqueue(serverEndpoint, vfo);
            }
            return oldFreq;
        }

        public void MicEvent(byte[] input)
        {
            byte[] copy = new byte[input.Length];
            //Don't filter FM as it is frequency bound
            if (mode == ModeType.WFM || mode == ModeType.FM)
            {
                Buffer.BlockCopy(input, 0, copy, 0, input.Length);
            }
            else
            {
                FormatConvert.S16ToDouble(input, outputRaw);
                IFilter filter = lpfAM;
                if (mode == ModeType.LSB || mode == ModeType.USB)
                {
                    filter = lpfSSB;
                }
                for (int i = 0; i < outputRaw.Length; i++)
                {
                    filter.AddSample(outputRaw[i]);
                    outputFiltered[i] = filter.GetSample();
                }
                FormatConvert.DoubleToS16(outputFiltered, copy);
                sendAudio.Enqueue(copy);
            }
        }

        public void Stop()
        {
            running = false;
            sendLoop.Join();
        }
    }
}