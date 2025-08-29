using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ResourceFileEditor.Manager.Audio;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ResourceFileEditor.Manager.Audio
{
    sealed class AudioManager
    {
        const UInt32 SOUND_MAGIC_IDMSA = 0x6D7A7274;

        public static Stream LoadFile(Stream file, out AudioInfo audioInfo)
        {
            MemoryStream data = new MemoryStream();
            audioInfo = new AudioInfo();
            int index = 0;
            int extSize = 2;

            UInt32 magic = FileManager.FileManager.readUint32Swapped(file, index);
            if (magic == SOUND_MAGIC_IDMSA)
            {
                setWaveHeader(data);

                WaveFormat waveFormat = new WaveFormat();
                index += 4;
                index += 8; // skip timestamp
                index += 1; // skip loaded
                int playBegin = FileManager.FileManager.readIntSwapped(file, index);
                index += 4;
                int playLength = FileManager.FileManager.readIntSwapped(file, index);
                index += 4;

                byte[] fbr = FileManager.FileManager.readByteArray(file, index, WaveFormat.Basic.classSize);
                data.Write(fbr, 0, WaveFormat.Basic.classSize);
                index += WaveFormat.Basic.classSize;

                waveFormat.basic = WaveFormat.Basic.fromByteArray(fbr);

                // Save the basic parameters
                audioInfo.SampleRate = (int)waveFormat.basic.samplesPerSec;
                audioInfo.Channels = waveFormat.basic.numChannels;
                audioInfo.BitsPerSample = waveFormat.basic.bitsPerSample;

                switch(waveFormat.basic.formatTag)
                {
                    case (ushort)WaveFormat.Basic.FormatTag.FORMAT_PCM:
                        extSize = 0;
                        audioInfo.Format = AudioFormat.PCM;
                        break;

                    case (ushort)WaveFormat.Basic.FormatTag.FORMAT_ADPCM:
                        data.Write(BitConverter.GetBytes((short)WaveFormat.Extra.Adpcm.classSize), 0, 2);
                        waveFormat.extraSize = FileManager.FileManager.readUint16(file, index);
                        index += 2;
                        byte[] abr = FileManager.FileManager.readByteArray(file, index, WaveFormat.Extra.Adpcm.classSize);
                        index += WaveFormat.Extra.Adpcm.classSize;
                        data.Write(abr, 0, WaveFormat.Extra.Adpcm.classSize);
                        extSize += WaveFormat.Extra.Adpcm.classSize;
                        audioInfo.Format = AudioFormat.ADPCM;
                        break;

                    case (ushort)WaveFormat.Basic.FormatTag.FORMAT_XMA2:
                        data.Write(BitConverter.GetBytes((short)WaveFormat.Extra.Xma2.classSize), 0, 2);
                        waveFormat.extraSize = FileManager.FileManager.readUint16(file, index);
                        index += 2;
                        byte[] xbr = FileManager.FileManager.readByteArray(file, index, WaveFormat.Extra.Xma2.classSize);
                        index += WaveFormat.Extra.Xma2.classSize;
                        data.Write(xbr, 0, WaveFormat.Extra.Xma2.classSize);
                        extSize += WaveFormat.Extra.Xma2.classSize;

                        var xma2 = WaveFormat.Extra.Xma2.fromByteArray(xbr);
                        audioInfo.IsXma2 = true;
                        audioInfo.Xma2PlayLength = xma2.playLength;
                        audioInfo.Format = AudioFormat.XMA2;
                        break;

                    case (ushort)WaveFormat.Basic.FormatTag.FORMAT_EXTENSIBLE:
                        data.Write(BitConverter.GetBytes((short)WaveFormat.Extra.Extensible.classSize), 0, 2);
                        waveFormat.extraSize = FileManager.FileManager.readUint16(file, index);
                        index += 2;
                        byte[] ebr = FileManager.FileManager.readByteArray(file, index, WaveFormat.Extra.Extensible.classSize);
                        index += WaveFormat.Extra.Extensible.classSize;
                        data.Write(ebr, 0, WaveFormat.Extra.Extensible.classSize);
                        extSize += WaveFormat.Extra.Extensible.classSize;
                        audioInfo.Format = AudioFormat.Extensible;
                        break;

                    default:
                        audioInfo.Format = AudioFormat.Unknown;
                        break;
                }

                data.Write(Encoding.ASCII.GetBytes("data"), 0, 4);
                int amplitudeNum = FileManager.FileManager.readIntSwapped(file, index);
                index += 4;
                index += amplitudeNum; // skip amplitudes

                int totalBufferSize = FileManager.FileManager.readIntSwapped(file, index);
                index += 4;
                audioInfo.DataSize = totalBufferSize;

                data.Write(BitConverter.GetBytes(totalBufferSize), 0, 4);

                int bufferNum = FileManager.FileManager.readIntSwapped(file, index);
                index += 4;
                int totalSamples = 0;
                for (int i = 0; i < bufferNum; i++)
                {
                    int numSamples = FileManager.FileManager.readIntSwapped(file, index);
                    index += 4;
                    int bufferSize = FileManager.FileManager.readIntSwapped(file, index);
                    index += 4;
                    byte[] buffer = FileManager.FileManager.readByteArray(file, index, bufferSize);
                    index += bufferSize;
                    if (buffer != null)
                    {
                        data.Write(buffer, 0, bufferSize);
                        totalSamples += numSamples;
                    }
                }

                // <-- Save totalSamples to audioInfo -->
                audioInfo.TotalSamples = totalSamples;

                if (!audioInfo.IsXma2)
                {
                    audioInfo.DataSize = totalSamples * waveFormat.basic.numChannels * (waveFormat.basic.bitsPerSample / 8);
                }
            }

            data.Position = 4;
            data.Write(BitConverter.GetBytes(data.Length - 8), 0, 4);
            data.Position = 16;
            data.Write(BitConverter.GetBytes(WaveFormat.Basic.classSize + extSize), 0, 4);
            data.Position = 0;

            return data;
        }

        private static void setWaveHeader(MemoryStream data)
        {
            byte[] buffer = Encoding.ASCII.GetBytes("RIFF");
            data.Write(buffer, 0, 4);
            data.Write(BitConverter.GetBytes(0), 0, 4); // Stream size TBF
            buffer = Encoding.ASCII.GetBytes("WAVE");
            data.Write(buffer, 0, 4);
            buffer = Encoding.UTF8.GetBytes("fmt ");
            data.Write(buffer, 0, 4);
            data.Write(BitConverter.GetBytes(WaveFormat.Basic.classSize), 0, 4);
        }
    }
}