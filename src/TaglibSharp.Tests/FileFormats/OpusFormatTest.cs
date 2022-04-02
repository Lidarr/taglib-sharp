using System.Linq;
using NUnit.Framework;
using TagLib;
using TagLib.Ogg;

namespace TaglibSharp.Tests.FileFormats
{
    [TestFixture]
    public class OpusFormatTest : IFormatTest
    {
        static readonly string sample_file = TestPath.Samples + "sample.opus";
        static readonly string tmp_file = TestPath.Samples + "tmpwrite.opus";
        TagLib.File file;

        [OneTimeSetUp]
        public void Init ()
        {
            file = TagLib.File.Create (sample_file);
        }

        [Test]
        public void ReadAudioProperties ()
        {
            StandardTests.ReadAudioProperties (file);
        }

        [Test]
        public void ReadTags ()
        {
            Assert.AreEqual ("Opus album", file.Tag.Album);
            Assert.AreEqual ("Opus artist", file.Tag.FirstPerformer);
            Assert.AreEqual ("Opus comment", file.Tag.Description);
            Assert.AreEqual ("Acid Punk", file.Tag.FirstGenre);
            Assert.AreEqual ("Opus title", file.Tag.Title);
            Assert.AreEqual (6, file.Tag.Track);
            Assert.AreEqual (7, file.Tag.TrackCount);
            Assert.AreEqual (1234, file.Tag.Year);
        }

        [Test]
        public void WriteStandardTags ()
        {
            StandardTests.WriteStandardTags (sample_file, tmp_file);
        }

        [Test]
        public void WriteExtendedTags ()
        {
            ExtendedTests.WriteExtendedTags (sample_file, tmp_file);
        }


        [Test]
        public void WriteStandardPictures ()
        {
            StandardTests.WriteStandardPictures (sample_file, tmp_file, ReadStyle.None);
        }

        [Test]
        [Ignore ("PictureLazy not supported yet")]
        public void WriteStandardPicturesLazy ()
        {
            StandardTests.WriteStandardPictures (sample_file, tmp_file, ReadStyle.PictureLazy);
        }


        [Test]
        public void TestCorruptionResistance ()
        {
            StandardTests.TestCorruptionResistance (TestPath.Samples + "corrupt/a.opus");
        }

        [Test]
        public void CheckInvariantStartPosition()
        {
            // There was a corruption bug in OPUS file writer, the root cause of which was
            // the Ogg Bitstream class requiring the first page of the media data to be read
            // in order to process the tags.  Then on write this media page is incorrectly
            // replaced with a page with absoluteGranularPosition = 0, which is not allowed.
            //
            // The sample file has the media beginning in the third page.  To test the fix
            // ensure that the InvariantStartPosition is the location of the third page.
            // Previously we read/wrote the third page and corrupted it, and InvariantStartPosition
            // was set to the start of the fourth page.
            //
            // In principle the comments packet can span multiple pages, so if the test file
            // is updated in future this may need adjusting.

            var p1 = file.Find("OggS", 0);
            var p2 = file.Find("OggS", p1 + 1);
            var p3 = file.Find("OggS", p2 + 1);

            Assert.AreEqual(p3, file.InvariantStartPosition);
        }

        [TestCase(10, false, 1, 10)]
        [TestCase(260, false, 2, 5)]
        [TestCase(255, false, 1, 255)]
        [TestCase(510, false, 2, 255)]
        [TestCase(510, true, 3, 0)]
        public void CheckLacingValues(int packet_length, bool packet_complete, int expected_lacing_lengtg, int expected_last_lace)
        {
            var page = CreatePageWithPacketLength(packet_length, packet_complete);

            var lacing_values = Enumerable.Repeat((byte)255, expected_lacing_lengtg - 1).ToList();
            lacing_values.Add((byte)expected_last_lace);
            var expected_lacing = new ByteVector(lacing_values.ToArray());

            Assert.AreEqual(page.Header.LacingValues, expected_lacing);
        }

        private Page CreatePageWithPacketLength(int length, bool packetComplete)
        {
            var page_packets = new ByteVectorCollection ();
            page_packets.Add(new ByteVector(new byte[length], length));
            
            var header = new PageHeader(0, 0, PageFlags.None, packetComplete);
            var page = new Page(page_packets, header);

            page.Render();
            
            return page;
        }
    }
}
