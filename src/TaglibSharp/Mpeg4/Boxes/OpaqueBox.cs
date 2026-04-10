//
// OpaqueBox.cs: Provides a box implementation that preserves its
// on-disk bytes exactly, with no re-parsing or re-rendering.
//
// This is used for atoms like the Nero chapter box (chpl) whose
// internal format has multiple versions.  Re-rendering through the
// normal Box pipeline can silently corrupt the data (e.g. version
// and flags bytes).  By capturing the complete atom bytes on read
// and returning them verbatim on render, we guarantee a lossless
// round-trip.
//
// Copyright (C) 2026
//
// This library is free software; you can redistribute it and/or modify
// it  under the terms of the GNU Lesser General Public License version
// 2.1 as published by the Free Software Foundation.
//
// This library is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307
// USA
//

using System;

namespace TagLib.Mpeg4
{
	/// <summary>
	///    This class extends <see cref="Box" /> to provide a box that
	///    stores and renders its original on-disk bytes verbatim.
	///    No header or data re-serialization is performed.
	/// </summary>
	public class OpaqueBox : Box
	{
		#region Private Fields

		/// <summary>
		///    The complete on-disk atom bytes (header + data).
		/// </summary>
		readonly ByteVector raw_bytes;

		#endregion



		#region Constructors

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="OpaqueBox" /> by reading the complete atom from
		///    the file at the position described by the header.
		/// </summary>
		/// <param name="header">
		///    A <see cref="BoxHeader" /> object containing the header
		///    to use for the new instance.
		/// </param>
		/// <param name="file">
		///    A <see cref="TagLib.File" /> object to read the contents
		///    of the box from.
		/// </param>
		/// <param name="handler">
		///    A <see cref="IsoHandlerBox" /> object containing the
		///    handler that applies to the new instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="file" /> is <see langword="null" />.
		/// </exception>
		public OpaqueBox (BoxHeader header, TagLib.File file, IsoHandlerBox handler) : base (header, handler)
		{
			if (file == null)
				throw new ArgumentNullException (nameof (file));

			file.Seek (header.Position);
			raw_bytes = file.ReadBlock ((int)header.TotalBoxSize);
		}

		#endregion



		#region Protected Methods

		/// <summary>
		///    Renders the box by returning the original on-disk bytes.
		/// </summary>
		/// <param name="topData">
		///    Ignored.  The original bytes are returned as-is.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector" /> containing the exact bytes
		///    that were on disk when the box was read.
		/// </returns>
		protected override ByteVector Render (ByteVector topData)
		{
			return raw_bytes;
		}

		#endregion
	}
}
