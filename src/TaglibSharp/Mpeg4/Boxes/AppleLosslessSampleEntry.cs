//
// AppleLosslessSampleEntry.cs: Reads the ALACSpecificConfig in an Apple Lossless sample entry.
//
// This library is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License version
// 2.1 as published by the Free Software Foundation.
//

using System;

namespace TagLib.Mpeg4
{
    /// <summary>
    ///    Reads Apple Lossless audio properties from the decoder configuration,
    ///    rather than the legacy sound description's placeholder values.
    /// </summary>
    // Properties.BitsPerSample queries ILosslessAudioCodec, not AudioSampleSize.
    // A separate subtype exposes that contract only for ALAC; AAC remains lossy.
    public class AppleLosslessSampleEntry : IsoAudioSampleEntry, ILosslessAudioCodec
    {
        readonly int bits_per_sample;
        readonly int sample_rate;
        readonly int channels;
        int bitrate;

        /// <summary>
        ///    Reads an Apple Lossless sample entry and its mandatory decoder configuration.
        /// </summary>
        public AppleLosslessSampleEntry (BoxHeader header, TagLib.File file, IsoHandlerBox handler)
            : base (header, file, handler)
        {
            // The child 'alac' box has a four-byte version/flags field followed
            // by the 24-byte, big-endian ALACSpecificConfig documented by Apple.
            var data = GetChild ("alac")?.Data;
            if (data == null || data.Count < 28)
                throw new CorruptFileException ("ALAC decoder configuration is missing or truncated.");

            // Offsets include the full-box version/flags: compatibleVersion at 8,
            // bitDepth at 9, channels at 13, avgBitRate at 20 and sampleRate at 24.
            // See Apple's ALACMagicCookieDescription.txt (macosforge/alac).
            bits_per_sample = data[9];
            channels = data[13];
            var rate = data.Mid (24, 4).ToUInt ();
            if (data.Mid (0, 4).ToUInt () != 0 || data[8] != 0 ||
                (bits_per_sample != 16 && bits_per_sample != 20 && bits_per_sample != 24 && bits_per_sample != 32) ||
                channels < 1 || channels > 8 || rate == 0 || rate > int.MaxValue)
                throw new CorruptFileException ("Invalid or unsupported ALAC decoder configuration.");

            sample_rate = (int)rate;
            bitrate = (int)(data.Mid (20, 4).ToUInt () / 1000);
        }

        /// <summary>Gets the encoded stream's average bitrate in kilobits per second.</summary>
        public override int AudioBitrate { get { return bitrate; } }

        /// <summary>Gets the full sample rate, which may exceed the sound description's 16-bit limit.</summary>
        public override int AudioSampleRate { get { return sample_rate; } }

        /// <summary>Gets the channel count from the decoder configuration.</summary>
        public override int AudioChannels { get { return channels; } }

        /// <summary>Gets the lossless audio bit depth.</summary>
        public int BitsPerSample { get { return bits_per_sample; } }

        /// <summary>Gets the actual sample size rather than the legacy sound description's placeholder.</summary>
        public override int AudioSampleSize { get { return bits_per_sample; } }

        // Some encoders put the uncompressed PCM bitrate in ALACSpecificConfig.
        // Prefer the audio sample sizes and media duration when they are available.
        internal void SetAverageBitrate (ulong bytes, ulong duration, uint timescale)
        {
            if (duration == 0 || timescale == 0 || bytes == 0)
                return;
            // Floating-point arithmetic avoids overflowing the product for long
            // streams; truncate to integer kbps, as IAudioCodec expects.
            double value = bytes * 8.0 * timescale / duration / 1000;
            if (value > 0 && value <= int.MaxValue)
                bitrate = (int)value;
        }
    }
}
