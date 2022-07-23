using System;

namespace VirtualRadio.Common
{
    public class RingBuffer
    {
        int period_size;
        int periods;
        int writeIndex = 0;
        int readIndex = 0;
        byte[] buffer;
        public RingBuffer(int period_size, int periods)
        {
            readIndex = periods - 1;
            this.period_size = period_size;
            this.periods = periods;
            buffer = new byte[periods * period_size];
        }

        public int AvailableRead
        {
            get
            {
                int writeReal = writeIndex;
                if (writeIndex < readIndex)
                {
                    writeReal = writeIndex + periods;
                }
                return writeReal - readIndex;
            }
        }

        public int AvailableWrite
        {
            get
            {
                if (readIndex == writeIndex)
                {
                    return periods;
                }
                int readReal = readIndex;
                if (readIndex > writeIndex)
                {
                    readReal = readIndex - periods;
                }
                return readReal - writeIndex;
            }
        }

        public void Write(byte[] input, int sequence)
        {

            writeIndex = sequence % periods;
            if (writeIndex == readIndex)
            {
                Console.WriteLine("Head overwrite");
                //Throw half the data away if we overwrite the tail
                readIndex = writeIndex - (periods / 2);
                if (readIndex < 0)
                {
                    readIndex += periods;
                }
            }
            Buffer.BlockCopy(input, 0, buffer, writeIndex * period_size, period_size);
        }

        public int Read(byte[] output)
        {
            if (readIndex == writeIndex)
            {
                Console.WriteLine("Underrun");
                return 0;
            }
            Buffer.BlockCopy(buffer, (readIndex % periods) * period_size, output, 0, period_size);
            readIndex++;
            if (readIndex == periods)
            {
                readIndex = 0;
            }
            return period_size;
        }
    }
}