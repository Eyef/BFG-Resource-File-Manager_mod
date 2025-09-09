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

            try
            {
                /* ---------- magic ---------- */
                UInt32 magic = FileManager.FileManager.readUint32Swapped(file, index);
                if (magic != SOUND_MAGIC_IDMSA)
                {
                    data.SetLength(0);
                    return data;
                }
                index += 4;

                /* ---------- RIFF header ---------- */
                WriteRiffHeader(data);

                /* ---------- fact chunk (placeholder) ---------- */
                long factSamplesPos = data.Position + 8;
                data.Write(Encoding.ASCII.GetBytes("fact"), 0, 4);
                data.Write(BitConverter.GetBytes(4), 0, 4);
                data.Write(BitConverter.GetBytes(0U), 0, 4);

                /* ---------- fmt chunk ---------- */
                long fmtSizePos = data.Position + 4;
                data.Write(Encoding.ASCII.GetBytes("fmt "), 0, 4);
                data.Write(BitConverter.GetBytes(0), 0, 4);
                long fmtDataStart = data.Position;

                index += 8;  // timestamp
                index += 1;  // loaded flag
                int playBegin = FileManager.FileManager.readIntSwapped(file, index);
                index += 4;
                int playLength = FileManager.FileManager.readIntSwapped(file, index);
                index += 4;

                /* ---------- WAVEFORMATEX ---------- */
                byte[] fbr = FileManager.FileManager.readByteArray(file, index, WaveFormat.Basic.classSize);
                index += WaveFormat.Basic.classSize;
                data.Write(fbr, 0, fbr.Length);
                var basic = WaveFormat.Basic.fromByteArray(fbr);

                audioInfo.SampleRate = (int)basic.samplesPerSec;
                audioInfo.Channels = basic.numChannels;
                audioInfo.BitsPerSample = basic.bitsPerSample;

                uint factSamples = 0;

                /* ---------- extra fmt ---------- */
                switch (basic.formatTag)
                {
                    case (ushort)WaveFormat.Basic.FormatTag.FORMAT_PCM:
                        extSize = 0;
                        audioInfo.Format = AudioFormat.PCM;
                        break;

                    case (ushort)WaveFormat.Basic.FormatTag.FORMAT_ADPCM:
                        audioInfo.Format = AudioFormat.ADPCM;
                        ushort extraAdpcm = FileManager.FileManager.readUint16(file, index);
                        index += 2;
                        byte[] abr = FileManager.FileManager.readByteArray(file, index, WaveFormat.Extra.Adpcm.classSize);
                        index += abr.Length;
                        data.Write(BitConverter.GetBytes((short)WaveFormat.Extra.Adpcm.classSize), 0, 2);
                        data.Write(abr, 0, abr.Length);
                        extSize += abr.Length;
                        break;

                    case (ushort)WaveFormat.Basic.FormatTag.FORMAT_XMA2:
                        audioInfo.Format = AudioFormat.XMA2;
                        ushort extraXma = FileManager.FileManager.readUint16(file, index);
                        index += 2;
                        byte[] xbr = FileManager.FileManager.readByteArray(file, index, WaveFormat.Extra.Xma2.classSize);
                        index += xbr.Length;
                        data.Write(BitConverter.GetBytes((short)WaveFormat.Extra.Xma2.classSize), 0, 2);
                        data.Write(xbr, 0, xbr.Length);
                        extSize += xbr.Length;
                        var xma2 = WaveFormat.Extra.Xma2.fromByteArray(xbr);
                        audioInfo.IsXma2 = true;
                        audioInfo.Xma2PlayLength = xma2.playLength;
                        factSamples = xma2.playLength;
                        break;

                    case (ushort)WaveFormat.Basic.FormatTag.FORMAT_EXTENSIBLE:
                        audioInfo.Format = AudioFormat.Extensible;
                        ushort extraExt = FileManager.FileManager.readUint16(file, index);
                        index += 2;
                        byte[] ebr = FileManager.FileManager.readByteArray(file, index, WaveFormat.Extra.Extensible.classSize);
                        index += ebr.Length;
                        data.Write(BitConverter.GetBytes((short)WaveFormat.Extra.Extensible.classSize), 0, 2);
                        data.Write(ebr, 0, ebr.Length);
                        extSize += ebr.Length;
                        break;

                    default:
                        audioInfo.Format = AudioFormat.Unknown;
                        break;
                }

                /* ---------- correct fmt size ---------- */
                int fmtChunkSize = (int)(data.Position - fmtDataStart);
                data.Position = fmtSizePos;
                data.Write(BitConverter.GetBytes(fmtChunkSize), 0, 4);
                data.Position = data.Length;

                /* ---------- amplitude skip ---------- */
                int ampLen = FileManager.FileManager.readIntSwapped(file, index);
                index += 4;
                if (ampLen > 0) index += ampLen;

                /* ---------- total buffer size ---------- */
                int totalBufferSize = FileManager.FileManager.readIntSwapped(file, index);
                index += 4;
                audioInfo.DataSize = totalBufferSize;

                int bufferNum = FileManager.FileManager.readIntSwapped(file, index);
                index += 4;

                int totalSamples = 0;
                long audioStartInMem = data.Position;
                data.Write(Encoding.ASCII.GetBytes("data"), 0, 4);
                long dataSizePos = data.Position;
                data.Write(BitConverter.GetBytes(0), 0, 4);
                long audioDataStart = data.Position;

                for (int i = 0; i < bufferNum; i++)
                {
                    int numSamples = FileManager.FileManager.readIntSwapped(file, index);
                    index += 4;
                    int bufSize = FileManager.FileManager.readIntSwapped(file, index);
                    index += 4;
                    byte[] buf = FileManager.FileManager.readByteArray(file, index, bufSize);
                    index += bufSize;
                    data.Write(buf, 0, bufSize);
                    totalSamples += numSamples;
                }
                audioInfo.TotalSamples = totalSamples;

                /* ---------- fix for size of data chunk ---------- */
                long realAudioSize = data.Position - audioDataStart;
                data.Position = dataSizePos;
                data.Write(BitConverter.GetBytes((int)realAudioSize), 0, 4);
                data.Position = data.Length;

                /* ---------- fix for fact ---------- */
                if (audioInfo.Format == AudioFormat.ADPCM)
                {
                    ushort spb = (ushort)((basic.blockSize - 7 * basic.numChannels) * 2 + 2);
                    uint blocks = (uint)(realAudioSize / basic.blockSize);
                    factSamples = blocks * spb;
                }
                else if (audioInfo.Format == AudioFormat.Extensible)
                    factSamples = (uint)totalSamples;

                data.Position = factSamplesPos;
                data.Write(BitConverter.GetBytes(factSamples), 0, 4);
                data.Position = data.Length;

                /* ---------- final RIFF size ---------- */
                long riffSize = data.Length - 8;
                data.Position = 4;
                data.Write(BitConverter.GetBytes((int)riffSize), 0, 4);
                data.Position = 0;

                return data;
            }
            catch
            {
                data.SetLength(0);
                return data;
            }
        }

        private static void WriteRiffHeader(MemoryStream ms)
        {
            ms.Write(Encoding.ASCII.GetBytes("RIFF"), 0, 4);
            ms.Write(BitConverter.GetBytes(0), 0, 4);
            ms.Write(Encoding.ASCII.GetBytes("WAVE"), 0, 4);
        }
    }
}