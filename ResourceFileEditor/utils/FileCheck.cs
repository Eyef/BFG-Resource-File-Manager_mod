/*
===========================================================================

BFG Resource File Manager GPL Source Code
Copyright (C) 2021 George Kalampokis

This file is part of the BFG Resource File Manager GPL Source Code ("BFG Resource File Manager Source Code").

BFG Resource File Manager Source Code is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

BFG Resource File Manager Source Code is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with BFG Resource File Manager Source Code.  If not, see <http://www.gnu.org/licenses/>.

===========================================================================
*/
using System;
using System.IO;

namespace ResourceFileEditor.utils
{
    public enum FileTypes
    {
        IMAGE, UNKNOWN, AUDIO, FONT
    }

    sealed class FileCheck
    {
        public static Boolean isFile(string name)
        {
            return name.Contains('.');
        }

        public static Boolean isExportableToStandard(string name)
        {
            return !name.Contains('.') || 
                   name.EndsWith("idwav", StringComparison.OrdinalIgnoreCase) || 
                   name.EndsWith("bimage", StringComparison.OrdinalIgnoreCase);
        }

        public static FileTypes getFileType(Stream file, string filename)
        {
            string fileext = Path.GetExtension(filename);
            if (string.IsNullOrEmpty(fileext)) return FileTypes.UNKNOWN;

            fileext = fileext.TrimStart('.');
            string fileName = Path.GetFileName(filename);
            
            if (fileext.Equals("dat", StringComparison.OrdinalIgnoreCase))
            {
                if (fileName.Equals("old_12.dat", StringComparison.OrdinalIgnoreCase) ||
                    fileName.Equals("old_24.dat", StringComparison.OrdinalIgnoreCase) ||
                    fileName.Equals("old_48.dat", StringComparison.OrdinalIgnoreCase) ||
                    fileName.Equals("fontimage_12.dat", StringComparison.OrdinalIgnoreCase) ||
                    fileName.Equals("fontimage_24.dat", StringComparison.OrdinalIgnoreCase) ||
                    fileName.Equals("fontimage_48.dat", StringComparison.OrdinalIgnoreCase))
                {
                    return FileTypes.UNKNOWN;
                }
                return FileTypes.FONT;
            }

            switch (fileext.ToLowerInvariant())
            {
                case "tga":
                case "bimage":
                case "jpg":
                case "png":
                    return FileTypes.IMAGE;
                case "wav":
                case "idwav":
                    return FileTypes.AUDIO;
                default:
                    return FileTypes.UNKNOWN;
            }
        }

        public static string getPathSeparator()
        {
            return Path.DirectorySeparatorChar.ToString();
        }
    }
}