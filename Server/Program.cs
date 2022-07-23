using System;
using VirtualRadio.Network;
using System.Threading;

namespace VirtualRadio.Server
{
    class Program
    {
        public static void Main(string[] args)
        {
            NetworkHandler handler = new NetworkHandler();
            NetworkEndpoint network = new NetworkEndpoint(handler, 5973);
            Mixer mixer = new Mixer();
            RtlOutput rtl = new RtlOutput();
            Sender sender = new Sender(mixer, network, rtl);
            Receiver receiver = new Receiver(sender, handler);
            Console.WriteLine("Virtual Server Ready!");
            while (true)
            {
                Thread.Sleep(100);
            }
        }
    }
}
