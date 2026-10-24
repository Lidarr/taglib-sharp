//
// AppleLosslessConfigurationBox.cs: Preserves the ALAC decoder configuration.
// Licensed under the GNU Lesser General Public License version 2.1.
//

namespace TagLib.Mpeg4
{
    /// <summary>Contains the full-box flags and ALACSpecificConfig bytes.</summary>
    public class AppleLosslessConfigurationBox : Box
    {
        /// <summary>Reads the decoder configuration at its declared data position.</summary>
        public AppleLosslessConfigurationBox (BoxHeader header, TagLib.File file, IsoHandlerBox handler)
            : base (header, handler)
        {
            if (file == null)
                throw new System.ArgumentNullException (nameof (file));
            if (DataSize < 28)
                throw new CorruptFileException ("Truncated ALAC decoder configuration.");
            // BoxHeader may read ahead beyond its eight-byte header. Seek to the
            // payload explicitly; retain every byte so tag writes preserve it.
            file.Seek (DataPosition);
            Data = file.ReadBlock (DataSize);
        }

        /// <summary>Gets or sets the decoder configuration, including version and flags.</summary>
        public override ByteVector Data { get; set; }
    }
}
