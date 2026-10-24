using System;
using System.Linq;
using System.IO;
using System.Text;
using NUnit.Framework;
using TagLib;

namespace TaglibSharp.Tests.TaggingFormats
{
    [TestFixture]
    public class AppleLosslessTest
    {
        [TestCase (16, 44100)]
        [TestCase (16, 48000)]
        [TestCase (16, 88200)]
        [TestCase (16, 96000)]
        [TestCase (16, 176400)]
        [TestCase (16, 192000)]
        [TestCase (24, 44100)]
        [TestCase (24, 48000)]
        [TestCase (24, 88200)]
        [TestCase (24, 96000)]
        [TestCase (24, 176400)]
        [TestCase (24, 192000)]
        [TestCase (20, 96000)]
        [TestCase (32, 192000)]
        public void ReadsDecoderConfigurationInsteadOfLegacyPlaceholders (int bits, int rate)
        {
            using (var file = Open (Build (bits, rate))) {
                Assert.IsFalse (file.PossiblyCorrupt);
                Assert.AreEqual (bits, file.Properties.BitsPerSample);
                Assert.AreEqual (rate, file.Properties.AudioSampleRate);
                Assert.AreEqual (2, file.Properties.AudioChannels);
                Assert.AreEqual (bits, ((TagLib.Mpeg4.IsoAudioSampleEntry)file.Properties.Codecs.First ()).AudioSampleSize);
            }
        }

        [TestCase (1)]
        [TestCase (2)]
        [TestCase (6)]
        [TestCase (8)]
        public void ReadsChannelCountFromDecoderConfiguration (int channels)
        {
            using (var file = Open (Build (24, 96000, channels: channels))) {
                Assert.IsFalse (file.PossiblyCorrupt);
                Assert.AreEqual (channels, file.Properties.AudioChannels);
            }
        }

        [TestCase (0)]
        [TestCase (1)]
        [TestCase (2)]
        public void ReadsQuickTimeSoundDescriptionVersions (int soundVersion)
        {
            using (var file = Open (Build (24, 192000, soundVersion: soundVersion))) {
                Assert.IsFalse (file.PossiblyCorrupt);
                Assert.AreEqual (24, file.Properties.BitsPerSample);
                Assert.AreEqual (192000, file.Properties.AudioSampleRate);
            }
        }

        [TestCase (false, false)]
        [TestCase (true, false)]
        [TestCase (false, true)]
        [TestCase (true, true)]
        public void ComputesEncodedBitrateFromTheAudioTrack (bool fixedSamples, bool mediaVersion1)
        {
            // The cookie deliberately claims the uncompressed PCM bitrate. The
            // movie duration differs from the audio track and must not be used.
            using (var file = Open (Build (24, 96000, fixedSamples: fixedSamples, mediaVersion1: mediaVersion1))) {
                Assert.IsFalse (file.PossiblyCorrupt);
                Assert.AreEqual (24, file.Properties.AudioBitrate);
            }
        }

        [TestCase (0)]
        [TestCase (10)]
        [TestCase (27)]
        public void DoesNotGuessBitDepthFromTruncatedConfiguration (int configLength)
        {
            using (var file = Open (Build (24, 96000, configLength: configLength))) {
                Assert.IsTrue (file.PossiblyCorrupt);
                Assert.AreEqual (0, file.Properties.BitsPerSample);
            }
        }

        [TestCase (8, 96000, 2)]
        [TestCase (24, 0, 2)]
        [TestCase (24, 96000, 0)]
        [TestCase (24, 96000, 9)]
        public void RejectsInvalidDecoderConfiguration (int bits, int rate, int channels)
        {
            using (var file = Open (Build (bits, rate, channels: channels))) {
                Assert.IsTrue (file.PossiblyCorrupt);
                Assert.AreEqual (0, file.Properties.BitsPerSample);
            }
        }

        [Test]
        public void DoesNotInventLosslessBitDepthForAac ()
        {
            using (var file = Open (Build (24, 96000, codec: "mp4a"))) {
                Assert.IsFalse (file.PossiblyCorrupt);
                Assert.AreEqual (0, file.Properties.BitsPerSample);
                Assert.AreEqual (48000, file.Properties.AudioSampleRate);
                Assert.IsFalse (file.Properties.Codecs.First () is ILosslessAudioCodec);
            }
        }

        [TestCase ("missing-config")]
        [TestCase ("bad-config-version")]
        [TestCase ("bad-compatible-version")]
        [TestCase ("bad-child-size")]
        [TestCase ("bad-media-version")]
        [TestCase ("short-media-header")]
        [TestCase ("short-sample-header")]
        [TestCase ("short-sample-table")]
        public void ReportsMalformedMetadataWithoutGuessingProperties (string scenario)
        {
            using (var file = Open (Build (24, 96000, scenario: scenario))) {
                Assert.IsTrue (file.PossiblyCorrupt, scenario);
                Assert.IsNotEmpty (file.CorruptionReasons.ToArray (), scenario);
            }
        }

        [TestCase ("missing-media-header")]
        [TestCase ("missing-sample-table")]
        [TestCase ("root-description")]
        [TestCase ("missing-media-parent")]
        [TestCase ("missing-sample-parent")]
        [TestCase ("zero-duration")]
        [TestCase ("zero-timescale")]
        [TestCase ("zero-samples")]
        [TestCase ("overflow-bitrate")]
        public void RetainsConfigurationBitrateWhenTrackStatisticsAreUnavailable (string scenario)
        {
            // The configuration value is a fallback only. A missing table or
            // incomplete track must not be mistaken for a zero-bitrate stream.
            using (var file = Open (Build (24, 96000, scenario: scenario))) {
                Assert.IsFalse (file.PossiblyCorrupt, scenario);
                Assert.AreEqual (24, file.Properties.BitsPerSample, scenario);
                Assert.AreEqual (4608, file.Properties.AudioBitrate, scenario);
            }
        }

        [Test]
        public void RejectsNullFileForConfigurationBox ()
        {
            Assert.Throws<ArgumentNullException> (() =>
                new TagLib.Mpeg4.AppleLosslessConfigurationBox (
                    new TagLib.Mpeg4.BoxHeader ("alac"), null, null));
        }

        [Test]
        public void ReportsAStreamThatEndsDuringTheSampleTableRead ()
        {
            // Simulate an underlying file that is truncated after its box
            // headers have been read, not just a size field that was wrong.
            var bytes = Build (24, 96000);
            int sampleData = FindType (bytes, "stsz") + 16;
            using (var file = new TagLib.Mpeg4.File (
                new ShortSampleTableFile (bytes, sampleData), ReadStyle.Average)) {
                Assert.IsTrue (file.PossiblyCorrupt);
                Assert.IsTrue (file.CorruptionReasons.Any (x => x.Contains ("Truncated MP4 sample sizes")));
            }
        }

        sealed class ShortSampleTableFile : TagLib.File.IFileAbstraction
        {
            readonly Stream stream;
            public ShortSampleTableFile (byte[] bytes, int sampleData)
            {
                stream = new ShortSampleTableStream (bytes, sampleData);
            }
            public string Name => "short-sample-table.m4a";
            public Stream ReadStream => stream;
            public Stream WriteStream => stream;
            public void CloseStream (Stream value) { }
        }

        sealed class ShortSampleTableStream : MemoryStream
        {
            readonly int sampleData;
            public ShortSampleTableStream (byte[] bytes, int sampleData) : base (bytes)
            {
                this.sampleData = sampleData;
            }
            public override int Read (byte[] buffer, int offset, int count)
            {
                if (Position == sampleData && count == 12)
                    return 0;
                return base.Read (buffer, offset, count);
            }
        }

        static int FindType (byte[] bytes, string type)
        {
            return Encoding.ASCII.GetString (bytes).IndexOf (type, StringComparison.Ordinal);
        }

        static TagLib.Mpeg4.File Open (byte[] bytes)
        {
            return new TagLib.Mpeg4.File (new MemoryFileAbstraction (bytes.Length + 4096, bytes), ReadStyle.Average);
        }

        // Metadata-only MP4 fixture: no recordings, external tools or media payload.
        static byte[] Build (int bits, int rate, int channels = 2, int soundVersion = 0,
                             bool fixedSamples = false, bool mediaVersion1 = false,
                             int configLength = 28, string codec = "alac", string scenario = null)
        {
            var cookie = new byte[28];
            Put32 (cookie, 4, 4096);
            cookie[9] = (byte)bits;
            cookie[10] = 40;
            cookie[11] = 10;
            cookie[12] = 14;
            cookie[13] = (byte)channels;
            Put32 (cookie, 20, (uint)(Math.Max (rate, 0) * Math.Max (bits, 0) * Math.Max (channels, 0)));
            Put32 (cookie, 24, (uint)rate);
            var sound = new byte[28 + (soundVersion == 1 ? 16 : soundVersion == 2 ? 36 : 0)];
            sound[7] = 1;
            sound[9] = (byte)soundVersion;
            sound[17] = 2;
            sound[19] = 16;
            Put32 (sound, 24, 48000u << 16);
            if (scenario == "bad-config-version") cookie[0] = 1;
            if (scenario == "bad-compatible-version") cookie[8] = 1;
            var config = scenario == "missing-config" ? new byte[0] : Box ("alac", cookie.Take (configLength).ToArray ());
            var entry = Box (codec, Join (sound, config));
            var stsd = Box ("stsd", Join (new byte[4], UInt32 (1), entry));
            var stsz = Box ("stsz", fixedSamples
                ? Join (new byte[4], UInt32 (500), UInt32 (12))
                : Join (new byte[8], UInt32 (3), UInt32 (1000), UInt32 (2000), UInt32 (3000)));
            var mdhd = new byte[mediaVersion1 ? 36 : 24];
            if (mediaVersion1) {
                mdhd[0] = 1;
                Put32 (mdhd, 20, 1000);
                Put32 (mdhd, 28, 2000);
            } else {
                Put32 (mdhd, 12, 1000);
                Put32 (mdhd, 16, 2000);
            }
            if (scenario == "bad-media-version") mdhd[0] = 2;
            if (scenario == "short-media-header") mdhd = new byte[10];
            if (scenario == "zero-duration") Put32 (mdhd, 16, 0);
            if (scenario == "zero-timescale") Put32 (mdhd, 12, 0);
            if (scenario == "zero-samples") Put32 (stsz, 16, 0);
            if (scenario == "short-sample-header") stsz = Box ("stsz", new byte[8]);
            if (scenario == "short-sample-table") Put32 (stsz, 16, 4);
            if (scenario == "overflow-bitrate") {
                // Large but representable MP4 counters must not wrap into a
                // negative integer bitrate or replace the configuration fallback.
                Put32 (mdhd, 12, uint.MaxValue);
                Put32 (mdhd, 16, 1);
                Put32 (stsz, 12, uint.MaxValue);
                Put32 (stsz, 16, uint.MaxValue);
            }
            var hdlr = new byte[25];
            Encoding.ASCII.GetBytes ("soun").CopyTo (hdlr, 8);
            var mediaHeader = scenario == "missing-media-header" ? new byte[0] : Box ("mdhd", mdhd);
            var sampleTable = scenario == "missing-sample-table" ? new byte[0] : stsz;
            var sampleChildren = Join (stsd, sampleTable);
            var mdia = Box ("mdia", Join (mediaHeader, Box ("hdlr", hdlr), Box ("minf", Box ("stbl", sampleChildren))));
            // Put the malformed sibling after the sample description so the
            // bounded child lookup, rather than the outer parser, validates it.
            if (scenario == "bad-child-size") mdia = Box ("mdia", Join (Box ("hdlr", hdlr), Box ("minf", Box ("stbl", sampleChildren)), mediaHeader));
            if (scenario == "root-description") mdia = Join (Box ("hdlr", hdlr), stsd);
            if (scenario == "missing-media-parent") mdia = Box ("stbl", Join (Box ("hdlr", hdlr), sampleChildren));
            if (scenario == "missing-sample-parent") mdia = Box ("mdia", Join (mediaHeader, Box ("hdlr", hdlr), sampleChildren));
            var mvhd = new byte[100];
            Put32 (mvhd, 12, 1000);
            Put32 (mvhd, 16, 3000);
            var ftyp = Box ("ftyp", Encoding.ASCII.GetBytes ("M4A \0\0\0\0M4A isom"));
            var bytes = scenario == "root-description"
                ? Join (ftyp, Box ("mvhd", mvhd), mdia)
                : Join (ftyp, Box ("moov", Join (Box ("mvhd", mvhd), Box ("trak", mdia))));
            if (scenario == "bad-child-size") Put32 (bytes, FindType (bytes, "mdhd") - 4, 4);
            return bytes;
        }

        static byte[] Box (string type, byte[] data)
        {
            return Join (UInt32 ((uint)data.Length + 8), Encoding.ASCII.GetBytes (type), data);
        }

        static byte[] Join (params byte[][] arrays)
        {
            return arrays.SelectMany (x => x).ToArray ();
        }

        static byte[] UInt32 (uint value)
        {
            return new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value };
        }

        static void Put32 (byte[] data, int offset, uint value)
        {
            UInt32 (value).CopyTo (data, offset);
        }
    }
}
