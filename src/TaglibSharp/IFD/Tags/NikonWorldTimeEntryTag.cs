//
// NikonWorldTimeEntryTag.cs:
//
// Author:
//   Ruben Vermeersch (ruben@savanne.be)
//
// Copyright (C) 2010 Ruben Vermeersch
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

namespace TagLib.IFD.Tags
{
    /// <summary>
    ///    Nikon world time entry tags.
    ///    Based on:
    ///    http://exiv2.org/tags-nikon.html
    /// </summary>
    public enum NikonWorldTimeEntryTag : ushort
    {

        /// <summary>
        ///    Timezone. (Hex: 0X0000)
        /// </summary>
        Timezone = 0,

        /// <summary>
        ///    Daylight savings. (Hex: 0X0002)
        /// </summary>
        DaylightSavings = 2,

        /// <summary>
        ///    Date display format. (Hex: 0X0003)
        /// </summary>
        DateDisplayFormat = 3,

    }
}
