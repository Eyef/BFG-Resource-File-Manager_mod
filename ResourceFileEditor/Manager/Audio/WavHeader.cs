namespace ResourceFileEditor.Manager.Audio
{
    public class WavHeader
    {
        public string RiffChunkId { get; set; } = "";
        public uint RiffChunkSize { get; set; }
        public string Format { get; set; } = "";

        public string FmtSubchunkId { get; set; } = "";
        public uint FmtSubchunkSize { get; set; }
        public ushort AudioFormat { get; set; }
        public ushort NumChannels { get; set; }
        public uint SampleRate { get; set; }
        public uint ByteRate { get; set; }
        public ushort BlockAlign { get; set; }
        public ushort BitsPerSample { get; set; }

        public string DataChunkId { get; set; } = "";
        public uint DataChunkSize { get; set; }

        public double DurationInSeconds()
        {
            return (double)DataChunkSize / ((double)SampleRate * BlockAlign);
        }
    }
}