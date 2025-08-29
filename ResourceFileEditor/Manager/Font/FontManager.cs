using ResourceFileEditor.Manager;
using ResourceFileEditor.Manager.Font;
using ResourceFileEditor.FileManager;
using ResourceFileEditor.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

namespace ResourceFileEditor.Manager.Font
{
    sealed class FontManager
    {
        private static readonly UInt32 FONT_MAGIC = 0x6964662A; // "*fdi" in big-endian

        public static FontData LoadFont(Stream file)
        {
            try
            {
                Debug.WriteLine("[FontManager] Starting font loading...");
                
                // Read the entire file into memory
                file.Seek(0, SeekOrigin.Begin);
                byte[] fileData = new byte[file.Length];
                int bytesRead = 0;
                while (bytesRead < fileData.Length)
                {
                    int read = file.Read(fileData, bytesRead, fileData.Length - bytesRead);
                    if (read == 0) throw new EndOfStreamException();
                    bytesRead += read;
                }

                // Checking the magic number (big-endian)
                UInt32 magic = (UInt32)(
                    (fileData[0] << 24) |
                    (fileData[1] << 16) |
                    (fileData[2] << 8) |
                    fileData[3]
                );
                Debug.WriteLine($"[FontManager] Magic number: 0x{magic:X8} (expected: 0x{FONT_MAGIC:X8})");

                if (magic != FONT_MAGIC)
                    throw new InvalidDataException($"Invalid font DAT file magic number (got 0x{magic:X8}, expected 0x{FONT_MAGIC:X8})");

                FontData fontData = new FontData();

                // Reading the header
                // size - big-endian ushort (2 bytes)
                fontData.Size = (fileData[4] << 8) | fileData[5];
                
                // asc - big-endian short (2 bytes)
                fontData.Ascender = (short)((fileData[6] << 8) | fileData[7]);
                
                // desc - big-endian short (2 bytes) with secure conversion
                int descValue = (fileData[8] << 8) | fileData[9];
                if (descValue > short.MaxValue) 
                    descValue -= 65536; // Conversion to negative short
                fontData.Descender = (short)descValue;
                
                // glyphCount - big-endian short (2 bytes)
                fontData.GlyphCount = (short)((fileData[10] << 8) | fileData[11]);

                Debug.WriteLine($"[FontManager] Font info - Size: {fontData.Size}, Asc: {fontData.Ascender}, Desc: {fontData.Descender}, Glyphs: {fontData.GlyphCount}");

                // Checking for reasonable values
                if (fontData.GlyphCount < 0 || fontData.GlyphCount > 20000)
                    throw new InvalidDataException($"Invalid glyph count: {fontData.GlyphCount}");

                // Reading glyphs (little-endian)
                int offset = 12;
                for (int i = 0; i < fontData.GlyphCount; i++)
                {
                    if (offset + 10 > fileData.Length)
                        throw new InvalidDataException("Unexpected end of file while reading glyphs");

                    GlyphInfo glyph = new GlyphInfo
                    {
                        Width = fileData[offset],
                        Height = fileData[offset + 1],
                        Top = unchecked((sbyte)fileData[offset + 2]),
                        Left = unchecked((sbyte)fileData[offset + 3]),
                        XSkip = fileData[offset + 4],
                        S = (ushort)(fileData[offset + 5] | (fileData[offset + 6] << 8)),
                        T = (ushort)(fileData[offset + 7] | (fileData[offset + 8] << 8))
                    };
                    fontData.Glyphs.Add(glyph);
                    offset += 10;
                }

                // Read character table (little-endian uint)
                for (int i = 0; i < fontData.GlyphCount; i++)
                {
                    if (offset + 4 > fileData.Length)
                        throw new InvalidDataException("Unexpected end of file while reading char codes");

                    uint code = (uint)(
                        fileData[offset + 3] << 24 |
                        fileData[offset + 2] << 16 |
                        fileData[offset + 1] << 8 |
                        fileData[offset]
                    );
                    fontData.CharCodes.Add(code);
                    offset += 4;
                }

                Debug.WriteLine("[FontManager] Font loaded successfully");
                return fontData;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FontManager] Error loading font: {ex}");
                throw;
            }
        }
    }
}