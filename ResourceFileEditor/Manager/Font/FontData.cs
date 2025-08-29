using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace ResourceFileEditor.Manager.Font
{
    sealed class FontData
    {
        public int Size { get; set; }
        public int Ascender { get; set; }
        public int Descender { get; set; }
        public int GlyphCount { get; set; }
        public List<GlyphInfo> Glyphs { get; set; } = new List<GlyphInfo>();
        public List<uint> CharCodes { get; set; } = new List<uint>();
    }

    sealed class GlyphInfo
    {
        public byte Width { get; set; }
        public byte Height { get; set; }
        public sbyte Top { get; set; }
        public sbyte Left { get; set; }
        public byte XSkip { get; set; }
        public ushort S { get; set; }
        public ushort T { get; set; }
    }
}