using System;
using System.Net;
using System.Numerics;
using VirtualRadio.Common;
using VirtualRadio.Network;

namespace VirtualRadio.Server
{
    class Client
    {
        public long lastReceive = DateTime.UtcNow.Ticks;
        public long lastSend;
        public IPEndPoint endpoint;
        public RingBuffer buffer = new RingBuffer(512, 16);
        public int frequency = 14100000;
        public double power = 0.25;
        public int rate = 8000;
        public ModeType mode = ModeType.AM;
        public bool iq = true;
        //Audio baseband
        byte[] readBlock = new byte[512];
        double carrierAngle;
        double blockPos = 0;
        //range = -1 to 1
        double[] audioBlock;
        double[] previousAudioBlock;
        double[] halfBlock;
        Complex[] hilbertPrev = new Complex[512];
        Complex[] hilbertMid = new Complex[512];
        Complex[] hilbertNext = new Complex[512];
        IFilter lowpassFilter = new WindowedSinc(2700, 500, 8000, false);
        public Complex[] GetComplexBlock()
        {
            Complex[] complexBlock = new Complex[256];
            for (int i = 0; i < complexBlock.Length; i++)
            {
                //Get power level, read next block if we are off the end
                blockPos += rate / 250000.0;
                if (audioBlock == null || blockPos > (audioBlock.Length - 1))
                {
                    if (audioBlock != null)
                    {
                        double[] temp = audioBlock;
                        audioBlock = previousAudioBlock;
                        previousAudioBlock = temp;
                        blockPos -= audioBlock.Length;
                    }
                    else
                    {
                        audioBlock = new double[512];
                        previousAudioBlock = new double[512];
                        halfBlock = new double[512];
                    }
                    buffer.Read(readBlock);
                    for (int j = 0; j < readBlock.Length; j++)
                    {
                        //Filter the input to remove spikes
                        lowpassFilter.AddSample((readBlock[j] / 128.0) - 1.0);
                        double filteredValue = lowpassFilter.GetSample();
                        audioBlock[j] = filteredValue;
                        if (mode == ModeType.LSB || mode == ModeType.USB)
                        {
                            if (j == readBlock.Length - 1)
                            {

                                for (int k = 0; k < halfBlock.Length / 2; k++)
                                {
                                    halfBlock[k] = previousAudioBlock[k + halfBlock.Length / 2];
                                    halfBlock[k + halfBlock.Length / 2] = audioBlock[k];
                                }
                                hilbertPrev = hilbertNext;
                                hilbertNext = Hilbert.Calculate(audioBlock);
                                hilbertMid = Hilbert.Calculate(halfBlock);
                                int halfLength = hilbertMid.Length / 2;
                                for (int l = 0; l < halfBlock.Length; l++)
                                {
                                    if (l < halfLength)
                                    {
                                        double prevPercent = 1.0 - (l / (double)halfLength);
                                        hilbertMid[l] = hilbertMid[l] * (1.0 - prevPercent) + hilbertPrev[l + halfLength] * prevPercent;
                                    }
                                    else
                                    {
                                        double nextPercent = (l - halfLength) / (double)halfLength;
                                        hilbertMid[l] = hilbertMid[l] * (1.0 - nextPercent) + hilbertNext[l - halfLength] * nextPercent;
                                    }
                                }
                            }
                        }
                    }
                }

                Complex samplePower = 0;
                double target = blockPos % 1;
                if (blockPos < 0)
                {
                    target = blockPos + 1;
                }
                if (mode != ModeType.LSB && mode != ModeType.USB)
                {
                    double samplePowerd = 0;
                    //Interpolating between audio buffers
                    if (blockPos < 0)
                    {
                        samplePowerd = previousAudioBlock[previousAudioBlock.Length - 1] * (1.0 - target);
                        samplePowerd += audioBlock[0] * target;

                    }
                    //Everything else
                    else
                    {
                        int startIndex = (int)(blockPos);
                        samplePowerd = audioBlock[startIndex] * (1.0 - target);
                        samplePowerd += audioBlock[startIndex + 1] * target;
                    }
                    samplePower = new Complex(samplePowerd, samplePowerd);
                }
                else
                {
                    //Interpolate between hilbert samples
                    int halfLength = hilbertMid.Length / 2;
                    if (blockPos < 0)
                    {
                        //First sample for negative block pos (-1 to 0)
                        Complex a = hilbertPrev[halfLength - 1];
                        Complex b = hilbertMid[0];
                        samplePower = a * (1.0 - target);
                        samplePower += b * target;
                    }
                    else
                    {
                        int midIndex = (int)blockPos;
                        Complex a = hilbertMid[midIndex];
                        Complex b = hilbertMid[midIndex + 1];
                        samplePower = a * (1.0 - target);
                        samplePower += b * target;
                    }
                }

                carrierAngle += (Math.Tau * (frequency - 14125000)) / 250000.0;
                Complex carrier = new Complex(Math.Cos(carrierAngle), Math.Sin(carrierAngle));

                if (mode == ModeType.AM)
                {
                    //Convert from -1:1 to 0:1
                    double amPower = (samplePower.Real + 1.0) / 2.0;
                    complexBlock[i] = carrier * power * amPower;
                }
                if (mode == ModeType.FM || mode == ModeType.WFM)
                {
                    //5khz deviation
                    if (mode == ModeType.FM)
                    {
                        carrierAngle += (Math.Tau * samplePower.Real * 5000.0) / 250000.0;
                    }
                    if (mode == ModeType.WFM)
                    {
                        carrierAngle += (Math.Tau * samplePower.Real * 75000.0) / 250000.0;
                    }
                    complexBlock[i] = carrier * power;
                }
                if (mode == ModeType.LSB || mode == ModeType.USB)
                {
                    if (mode == ModeType.USB)
                    {
                        samplePower = Complex.Conjugate(samplePower);
                    }
                    complexBlock[i] = carrier * samplePower * power;
                }

                while (carrierAngle > Math.Tau)
                {
                    carrierAngle -= Math.Tau;
                }
                while (carrierAngle < -Math.Tau)
                {
                    carrierAngle += Math.Tau;
                }
            }
            return complexBlock;
        }
    }
}