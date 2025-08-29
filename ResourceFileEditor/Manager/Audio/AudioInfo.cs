using System;

namespace ResourceFileEditor.Manager.Audio
{
/// <summary>
/// Represents the format of audio data.
/// </summary>
public enum AudioFormat
{
    /// <summary>
    /// Unknown format.
    /// </summary>
    Unknown,

    /// <summary>
    /// PCM - pulse-code modulation.
    /// </summary>
    PCM,

    /// <summary>
    /// ADPCM — adaptive delta pulse-code modulation.
    /// </summary>
    ADPCM,

    /// <summary>
    /// XMA2 is Microsoft's extended format for Xbox.
    /// </summary>
    XMA2,

    /// <summary>
    /// WAVEFORMATEXTENSIBLE - Extended WAV format.
    /// </summary>
    Extensible
}

    /// <summary>
    /// Contains information about audio file parameters for correct playback and duration calculation.
    /// </summary>
    public class AudioInfo
    {
        /// <summary>
        /// Gets or sets the audio sample rate (e.g. 44100 Hz).
        /// </summary>
        public int SampleRate { get; set; }

        /// <summary>
        /// Gets or sets the number of audio channels (mono = 1, stereo = 2).
        /// </summary>
        public int Channels { get; set; }

        /// <summary>
        /// Gets or sets the bit rate of the sample (usually 16 bits).
        /// </summary>
        public int BitsPerSample { get; set; }

        /// <summary>
        /// Gets or sets the total size of the audio data in bytes.
        /// </summary>
        public long DataSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the format is XMA2.
        /// </summary>
        public bool IsXma2 { get; set; }

        /// <summary>
        /// Gets or sets the number of samples to play in XMA2 format.
        /// Applies only if <see cref="IsXma2"/> == true.
        /// </summary>
        public uint Xma2PlayLength { get; set; }

        /// <summary>
        ///  Gets or sets the audio data format (PCM, ADPCM, XMA2, etc.).
        /// </summary>
        public AudioFormat Format { get; set; }
        /// <summary>
        /// A placeholder so you don't have to make up a description.
        /// </summary>
        public int TotalSamples { get; set; }
    }
}