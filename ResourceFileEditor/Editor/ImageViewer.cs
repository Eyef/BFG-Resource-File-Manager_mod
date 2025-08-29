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
using ResourceFileEditor.Manager;
using ResourceFileEditor.Manager.Image;
using ResourceFileEditor.utils;
using StbImageSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ResourceFileEditor.Editor
{
    sealed class ImageViewer : Editor
    {
        private ManagerImpl manager;
        private Bitmap? backgroundImage; // Storing link for release

        public ImageViewer(ManagerImpl manager)
        {
            this.manager = manager;
        }

        public void start(Panel panel, TreeNode node)
        {
            string relativePath = PathParser.NodetoPath(node);
            Stream? file = manager.loadEntry(relativePath);
            if (file != null)
            {
                // Save the original background of the form for recovery
                Color originalBackColor = panel.FindForm()?.BackColor ?? SystemColors.Control;

                // Clearing the previous elements
                panel.Controls.Clear();

                // Resetting the panel background
                panel.BackgroundImage = null;
                panel.BackColor = originalBackColor;

                // Create a TableLayoutPanel for the layout
                TableLayoutPanel layoutPanel = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 2,
                    AutoSize = false
                };
                layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); // Fixed height for infoLabel
                layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Remaining space for scrollPanel

                // Label with information
                Label infoLabel = new Label
                {
                    Dock = DockStyle.Fill,
                    Height = 30,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(5),
                    BackColor = Color.White // Opaque background
                };
                infoLabel.Font = new Font(infoLabel.Font.FontFamily, infoLabel.Font.Size, FontStyle.Bold);

                // Scrolling panel
                DoubleBufferedPanel scrollPanel = new DoubleBufferedPanel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    BackColor = Color.AntiqueWhite //Set AntiqueWhite for contrast
                };

                // Set the chequered background on the scrollPanel
                backgroundImage = GenerateBackgound();
                scrollPanel.BackgroundImage = backgroundImage;
                scrollPanel.BackgroundImageLayout = ImageLayout.Tile;

                // PictureBox for an image
                PictureBox pictureBox = new PictureBox
                {
                    SizeMode = PictureBoxSizeMode.AutoSize,
                    BackColor = Color.Transparent // Transparent image background
                };

                // Uploading an image and metadata
                Size imageSize = Size.Empty;
                if (relativePath.EndsWith(".bimage", StringComparison.InvariantCultureIgnoreCase))
                {
                    file.Position = 0;
                    ImageData imageData = ReadBImageMetadata(file);
                    Bitmap bitmap = loadBitmap(ImageManager.LoadImage(file)!);
                    pictureBox.Image = bitmap;
                    infoLabel.Text = $"Type: {(TextureType)imageData.texType}, " +
                                     $"Format: {(TextureFormat)imageData.format} ({(tTextureColor)imageData.colorFormat}), " +
                                     $"Size: {imageData.width}x{imageData.height}";
                    imageSize = new Size((int)imageData.width, (int)imageData.height);
                }
                else
                {
                    Bitmap bitmap = loadBitmap(file);
                    pictureBox.Image = bitmap;
                    infoLabel.Text = "Type: Unknown, Format: Unknown, Size: Unknown";
                    imageSize = bitmap.Size;
                }

                // Set the size of the scroll area
                scrollPanel.AutoScrollMinSize = imageSize;

                // Composition of elements
                scrollPanel.Controls.Add(pictureBox);
                layoutPanel.Controls.Add(infoLabel, 0, 0);
                layoutPanel.Controls.Add(scrollPanel, 0, 1);
                panel.Controls.Add(layoutPanel);

                // Handler for releasing image resources
                pictureBox.ParentChanged += new EventHandler(disposeImage);

                // Handler for clearing background and resources when deleting layoutPanel
                layoutPanel.ParentChanged += (s, e) =>
                {
                    if (s is TableLayoutPanel layout && layout.Parent == null)
                    {
                        if (scrollPanel.BackgroundImage != null)
                        {
                            scrollPanel.BackgroundImage = null;
                            backgroundImage?.Dispose();
                            backgroundImage = null;
                        }
                        panel.BackColor = originalBackColor;
                        scrollPanel.BackColor = originalBackColor; // Reset scrollPanel color
                        panel.Invalidate();
                    }
                };

                // Resize handler for correct redrawing
                panel.Resize += (s, e) =>
                {
                    panel.Invalidate();
                };

                // Scroll handlers to prevent artefacts
                scrollPanel.Scroll += (s, e) =>
                {
                    scrollPanel.Invalidate();
                };
                scrollPanel.MouseWheel += (s, e) =>
                {
                    scrollPanel.Invalidate();
                };

                // Forced redrawing of scrollPanel to display the background
                scrollPanel.Invalidate();
            }
        }

        private void disposeImage(object? sender, EventArgs e)
        {
            if (sender is PictureBox pb && pb.Parent == null)
            {
                pb.Image?.Dispose();
                pb.BackgroundImage?.Dispose();
                pb.Image = null;
                pb.BackgroundImage = null;
            }
        }

        public static Bitmap loadBitmap(Stream file)
        {
            ImageResult imageResult = ImageResult.FromStream(file, ColorComponents.RedGreenBlueAlpha);
            byte[] data = imageResult.Data;
            // Convert rgba to bgra
            for (int i = 0; i < imageResult.Width * imageResult.Height; ++i)
            {
                byte r = data[i * 4];
                byte g = data[i * 4 + 1];
                byte b = data[i * 4 + 2];
                byte a = data[i * 4 + 3];

                data[i * 4] = b;
                data[i * 4 + 1] = g;
                data[i * 4 + 2] = r;
                data[i * 4 + 3] = a;
            }
            // Create Bitmap
            Bitmap bmp = new Bitmap(imageResult.Width, imageResult.Height, PixelFormat.Format32bppArgb);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, imageResult.Width, imageResult.Height), ImageLockMode.WriteOnly,
                bmp.PixelFormat);

            Marshal.Copy(imageResult.Data, 0, bmpData.Scan0, bmpData.Stride * bmp.Height);
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        private static Bitmap GenerateBackgound()
        {
            int size = 20;
            Bitmap background = new Bitmap(size * 2, size * 2);
            using (Graphics G = Graphics.FromImage(background))
            {
                // Fill the entire background with AntiqueWhite.
                G.FillRectangle(new SolidBrush(Color.AntiqueWhite), 0, 0, size * 2, size * 2);
                // Drawing grey squares
                using (SolidBrush brush = new SolidBrush(Color.LightGray))
                {
                    G.FillRectangle(brush, 0, 0, size, size);
                    G.FillRectangle(brush, size, size, size, size);
                }
            }
            return background;
        }

        private sealed class ImageData
        {
            public UInt32 texType;
            public UInt32 format;
            public UInt32 colorFormat;
            public UInt32 width;
            public UInt32 height;
        }

        private static ImageData ReadBImageMetadata(Stream file)
        {
            ImageData data = new ImageData();
            int index = 0;

            // Skip timestamp
            index += 8;
            // Magic
            index += 4;
            data.texType = FileManager.FileManager.readUint32Swapped(file, index); index += 4;
            data.format = FileManager.FileManager.readUint32Swapped(file, index); index += 4;
            data.colorFormat = FileManager.FileManager.readUint32Swapped(file, index); index += 4;
            data.width = FileManager.FileManager.readUint32Swapped(file, index); index += 4;
            data.height = FileManager.FileManager.readUint32Swapped(file, index); index += 4;
            // Skip number of levels
            index += 4;

            return data;
        }
    }

    // Auxiliary class for double buffering
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint, true);
            this.UpdateStyles();
        }
    }
}