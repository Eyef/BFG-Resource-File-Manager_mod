using ResourceFileEditor.Manager;
using ResourceFileEditor.Manager.Font;
using ResourceFileEditor.utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Globalization;
using System.Windows.Forms;
using System.Reflection;

namespace ResourceFileEditor.Editor
{
    sealed class FontViewer : Editor
    {
        private readonly ManagerImpl manager;
        private static readonly CultureInfo culture = CultureInfo.InvariantCulture;

        public FontViewer(ManagerImpl manager)
        {
            this.manager = manager;
        }

        public void start(Panel panel, TreeNode node)
        {
            string relativePath = PathParser.NodetoPath(node);
            Stream? file = manager.loadEntry(relativePath);
            if (file == null) return;

            try
            {
                FontData fontData = FontManager.LoadFont(file);

                // Main container
                TableLayoutPanel mainPanel = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    RowCount = 2,
                    ColumnCount = 1,
                    RowStyles =
                    {
                        new RowStyle(SizeType.Absolute, 60f),
                        new RowStyle(SizeType.Percent, 100f)
                    }
                };

                // Information panel
                TextBox infoBox = new TextBox
                {
                    Multiline = true,
                    ReadOnly = true,
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 9),
                    BorderStyle = BorderStyle.None,
                    Text = $"Size: {fontData.Size}\r\nAscender: {fontData.Ascender}\r\nDescender: {fontData.Descender}\r\nGlyphs: {fontData.GlyphCount}",
                    ScrollBars = ScrollBars.None
                };

                // Glyph table
                DataGridView grid = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    ReadOnly = true,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                    Font = new Font("Consolas", 9),
                    RowHeadersVisible = false,
                    BackgroundColor = SystemColors.Window,
                    AutoGenerateColumns = false
                };

                // Secure DoubleBuffered configuration
                PropertyInfo? doubleBufferedProperty = typeof(Control).GetProperty("DoubleBuffered", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                doubleBufferedProperty?.SetValue(grid, true, null);

                // Setting up the columns
                grid.Columns.AddRange(new DataGridViewColumn[]
                {
                    new DataGridViewTextBoxColumn { Name = "Idx", HeaderText = "Idx", ValueType = typeof(int) },
                    new DataGridViewTextBoxColumn { Name = "Ch", HeaderText = "Ch", ValueType = typeof(string) },
                    new DataGridViewTextBoxColumn { Name = "Code", HeaderText = "Code", ValueType = typeof(uint) },
                    new DataGridViewTextBoxColumn { Name = "Width", HeaderText = "W", ValueType = typeof(byte) },
                    new DataGridViewTextBoxColumn { Name = "Height", HeaderText = "H", ValueType = typeof(byte) },
                    new DataGridViewTextBoxColumn { Name = "Top", HeaderText = "Top", ValueType = typeof(sbyte) },
                    new DataGridViewTextBoxColumn { Name = "Left", HeaderText = "L", ValueType = typeof(sbyte) },
                    new DataGridViewTextBoxColumn { Name = "XSkip", HeaderText = "XS", ValueType = typeof(byte) }
                });

                // Sorting handler
                grid.SortCompare += (sender, e) =>
                {
                    if (e.CellValue1 == null || e.CellValue2 == null)
                    {
                        e.SortResult = 0;
                        e.Handled = true;
                        return;
                    }

                    if (e.Column.ValueType == typeof(string))
                    {
                        e.SortResult = string.CompareOrdinal(
                            e.CellValue1.ToString(),
                            e.CellValue2.ToString());
                    }
                    else
                    {
                        double val1 = Convert.ToDouble(e.CellValue1, culture);
                        double val2 = Convert.ToDouble(e.CellValue2, culture);
                        e.SortResult = val1.CompareTo(val2);
                    }
                    e.Handled = true;
                };

                // Data filling
                if (fontData.Glyphs != null && fontData.CharCodes != null)
                {
                    for (int i = 0; i < fontData.GlyphCount; i++)
                    {
                        var glyph = fontData.Glyphs[i];
                        uint code = fontData.CharCodes[i];
                        char ch = (code >= 32 && code <= 0xFFFF && 
                                 !char.IsSurrogate((char)code) && 
                                 !char.IsControl((char)code)) 
                                ? (char)code : '.';

                        grid.Rows.Add(
                            i,
                            ch.ToString(),
                            code,
                            glyph.Width,
                            glyph.Height,
                            glyph.Top,
                            glyph.Left,
                            glyph.XSkip
                        );
                    }
                }

                // Adding items
                mainPanel.Controls.Add(infoBox, 0, 0);
                mainPanel.Controls.Add(grid, 0, 1);
                panel.Controls.Add(mainPanel);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading font: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                file.Dispose();
            }
        }
    }
}