using System;
using System.Net;
using System.Numerics;
using VirtualRadio.Common;
using VirtualRadio.Network;

namespace VirtualRadio.Server
{
    class Client
    {
        public const int BYTES_PER_PACKET = 1024;
        public long lastReceive = DateTime.UtcNow.Ticks;
        public long lastSend;
        public IPEndPoint endpoint;
        public RingBuffer buffer = new RingBuffer(BYTES_PER_PACKET, 32);
        public int frequency = 14100000;
        public double power = 0.25;
        public int rate = 44100;
        public ModeType mode = ModeType.AM;
        public bool iq = true;
        //Audio baseband
        byte[] readBlock = new byte[BYTES_PER_PACKET];
        double carrierAngle;
        double audioPos = 0;
        //range = -1 to 1
        double[] audioBlock;
        double[] previousAudioBlock;
        double[] halfBlock;
        Complex[] hilbertPrev = new Complex[BYTES_PER_PACKET / 2];
        Complex[] hilbertMid = new Complex[BYTES_PER_PACKET / 2];
        Complex[] hilbertNext = new Complex[BYTES_PER_PACKET / 2];
        public Complex[] WriteComplexBlock(Complex[] complexBlock)
        {
            for (int i = 0; i < complexBlock.Length; i++)
            {
                //Get power level, read next block if we are off the end
                audioPos += rate / 250000.0;
                if (audioBlock == null || audioPos > (audioBlock.Length - 1))
                {
                    if (audioBlock != null)
                    {
                        double[] temp = audioBlock;
                        audioBlock = previousAudioBlock;
                        previousAudioBlock = temp;
                        audioPos -= audioBlock.Length;
                    }
                    else
                    {
                        audioBlock = new double[BYTES_PER_PACKET / 2];
                        previousAudioBlock = new double[BYTES_PER_PACKET / 2];
                        halfBlock = new double[BYTES_PER_PACKET / 2];
                    }
                    buffer.Read(readBlock);
                    FormatConvert.S16ToDouble(readBlock, audioBlock);

                    if (mode == ModeType.LSB || mode == ModeType.USB)
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

                Complex samplePower = 0;
                double target = audioPos % 1;
                if (audioPos < 0)
                {
                    target = audioPos + 1;
                }
                if (mode != ModeType.LSB && mode != ModeType.USB)
                {
                    double samplePowerd = 0;
                    //Interpolating between audio buffers
                    if (audioPos < 0)
                    {
                        samplePowerd = previousAudioBlock[previousAudioBlock.Length - 1] * (1.0 - target);
                        samplePowerd += audioBlock[0] * target;

                    }
                    //Everything else
                    else
                    {
                        int startIndex = (int)(audioPos);
                        samplePowerd = audioBlock[startIndex] * (1.0 - target);
                        samplePowerd += audioBlock[startIndex + 1] * target;
                    }
                    samplePower = new Complex(samplePowerd, samplePowerd);
                }
                else
                {
                    //Interpolate between hilbert samples
                    int halfLength = hilbertMid.Length / 2;
                    if (audioPos < 0)
                    {
                        //First sample for negative block pos (-1 to 0)
                        Complex a = hilbertPrev[halfLength - 1];
                        Complex b = hilbertMid[0];
                        samplePower = a * (1.0 - target);
                        samplePower += b * target;
                    }
                    else
                    {
                        int midIndex = (int)audioPos;
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
                    complexBlock[i] += carrier * power * amPower;
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
                    complexBlock[i] += carrier * power;
                }
                if (mode == ModeType.LSB || mode == ModeType.USB)
                {
                    if (mode == ModeType.USB)
                    {
                        samplePower = Complex.Conjugate(samplePower);
                    }
                    complexBlock[i] += carrier * samplePower * power;
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