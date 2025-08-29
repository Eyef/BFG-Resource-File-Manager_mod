﻿/*
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
along with Doom 3 BFG Edition Source Code.  If not, see <http://www.gnu.org/licenses/>.

===========================================================================
*/
using ResourceFileEditor.Editor;
using ResourceFileEditor.Manager;
using ResourceFileEditor.utils;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ResourceFileEditor
{
    sealed partial class ManagerUi : Form
    {
        private ManagerImpl manager;
        private readonly string DEFAULT_TITLE = "BFG Resource File Manager";

        private readonly double warningPercent = (((double)1 * 1024 * 1024 * 1024) / UInt32.MaxValue) * 100;
        private readonly EditorFactory editorFactory;

        public Label? extractProgressLabel { get; set; }
        public ProgressBar? extractProgressBar { get; set; }
        public ManagerUi()
        {
            InitializeComponent();
            this.Text = DEFAULT_TITLE;
            this.manager = new ManagerImpl(this);
            toolStripStatusLabel1.Text = "";
            toolStripStatusLabel2.Text = "";
            toolStripStatusLabel3.Text = "";
            Boolean hasNodes = treeView1.Nodes.Count > 0;
            saveFileToolStripMenuItem.Enabled = hasNodes;
            entryToolStripMenuItem.Enabled = hasNodes;
            editorFactory = new EditorFactory(manager);
        }

        public void OpenFile(Stream file)
        {
            Stream myStream;
            if ((myStream = file) != null)
            {
                treeView1.Nodes.Clear();
                treeView1.Nodes.Add(new TreeNode("root"));
                manager.CloseFile();
                manager.loadFile(myStream);

                treeView1.SelectedNode = treeView1.Nodes[0];
                treeView1.Nodes[0].Expand();
                Boolean hasNodes = treeView1.Nodes.Count > 0;
                saveFileToolStripMenuItem.Enabled = hasNodes;
                entryToolStripMenuItem.Enabled = hasNodes;
            }
        }

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "DOOM 3 BFG Edition resource files (*.resources)|*.resources";
            ofd.Title = "Load File";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                this.OpenFile(ofd.OpenFile());
            }
        }

        public TreeView GetTreeView()
        {
            return this.treeView1;
        }

        private void saveFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.Nodes.Count > 0)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "DOOM 3 BFG Edition resource files(*.resources)| *.resources";
                sfd.Title = "Save Resource File";
                sfd.FileName = manager.GetResourceFileName();
                if (sfd.FileName == "" || !sfd.FileName.EndsWith(".resources", StringComparison.InvariantCulture))
                {
                    sfd.ShowDialog();
                } else
                {
                    sfd.FileName = manager.GetResourceFullPath();
                }

                if (sfd.FileName != "")
                {
                    if (sfd.FileName == manager.GetResourceFullPath())
                    {
                        File.Copy(manager.GetResourceFullPath()!, manager.GetResourceFullPath() + ".bak", true);
                        File.Delete(sfd.FileName);
                    }
                    Stream file = File.Open(sfd.FileName, FileMode.OpenOrCreate);
                    manager.writeFile(file);
                }
            }
        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Stream myStream;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Load File";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                if ((myStream = ofd.OpenFile()) != null)
                {
                    TreeNode node = treeView1.SelectedNode;
                    importData(myStream, node);
                }
            }
        }

        private void addFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddEntry_logic();
        }

        private void createFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            manager.CloseFile();
            treeView1.Nodes.Clear();
            treeView1.Nodes.Add("root");
            treeView1.SelectedNode = treeView1.Nodes[0];
            manager.CreateFile();
            Boolean hasNodes = treeView1.Nodes.Count > 0;
            saveFileToolStripMenuItem.Enabled = hasNodes;
            entryToolStripMenuItem.Enabled = hasNodes;
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            treeView1.SelectedNode = e.Node;
            if (e.Button == MouseButtons.Right)
            {
                addFolderContextMenuItem.Visible = !FileCheck.isFile(e.Node.Text);
                deleteEntryContextMenuItem.Visible = true;
                deleteEntryContextMenuItem.Text = FileCheck.isFile(e.Node.Text) ? "Delete Entry" : "Delete Folder";
                extractEntryContextMenuItem.Visible = true;
                extractEntryContextMenuItem.Text = FileCheck.isFile(e.Node.Text) ? "Extract Entry" : "Extract Folder";
                exportToStandardFormatToolStripMenuItem.Visible = FileCheck.isExportableToStandard(e.Node.Text);
                addContextMenuItem.Visible = !FileCheck.isFile(e.Node.Text);
                contextMenuStrip1.Show(treeView1, e.Location);
            }
        }

        private void addEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddEntry_logic();
        }

        private void AddEntry_logic()
        {
            string name = string.Empty;
            DialogResult dialogResult = InputBox("Add entry", "Please add Entry name", ref name);

            if (dialogResult != DialogResult.OK || string.IsNullOrWhiteSpace(name))
                return;
            TreeNode node = treeView1.SelectedNode;
            if (node == null)
            {
                MessageBox.Show("Please create a File", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            TreeNode subNode = new TreeNode(name);
            if (FileCheck.isFile(node.Text))
            {
                MessageBox.Show("You have Selected a file. Please select a folder", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            node.Nodes.Add(subNode);
        }

        private void deleteEntryToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            DeleteEntry_logic(treeView1.SelectedNode);
        }

        private void deleteEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteEntry_logic(treeView1.SelectedNode);
        }

        private void DeleteEntry_logic(TreeNode node, bool removeNode = true)
        {   
            if (FileCheck.isFile(node.Text))
            {
                string relativePath = PathParser.NodetoPath(node);
                manager.DeleteEntry(relativePath);
                updateToolStripBar(toolStripStatusLabel1.Text);
            }
            else
            {
                int nomofchilds = node.Nodes.Count;
                for(int i=0; i < nomofchilds; i++)
                {
                    DeleteEntry_logic(node.Nodes[i], false);
                }
            }
            if (removeNode)
            {
                if (node.Parent == null)
                {
                    treeView1.Nodes.Remove(node);
                }
                else
                {
                    node.Parent.Nodes.Remove(node);
                }
            }
        }

        private async void extractEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await ExtractEntry_logicAsync();
        }

        private async Task ExtractEntry_logicAsync()
        {
            TreeNode? node = treeView1.SelectedNode;
            if (node == null)
            {
                MessageBox.Show("Please select a folder", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string relativePath = PathParser.NodetoPath(node);
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "Select Folder to extract the file into";
            
            // Adding a check for the dialog result
            if (fbd.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(fbd.SelectedPath))
            {
                if (FileCheck.isFile(relativePath))
                {
                    await Task.Run(() => manager.ExtractEntry(relativePath, fbd.SelectedPath));
                }
                else
                {
                    Task _ = Task.Run(() => manager.ExtractFolder(relativePath, fbd.SelectedPath));
                    this.ShowProgressBar("Extracting Files");
                }
            }
        }

        private static string ConvertBytesToString(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int repeats = 0;
            long finalSize = bytes;
            while (finalSize > 1024)
            {
                finalSize = finalSize / 1024;
                repeats++;
            }
            string result = finalSize + sizes[repeats];
            return result;
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            addFolderToolStripMenuItem.Visible = !FileCheck.isFile(e.Node!.Text);
            deleteEntryToolStripMenuItem.Visible = true;
            deleteEntryToolStripMenuItem.Text = FileCheck.isFile(e.Node.Text) ? "Delete Entry" : "Delete Folder";
            extractEntryToolStripMenuItem.Visible = true;
            extractEntryToolStripMenuItem.Text = FileCheck.isFile(e.Node.Text) ? "Extract Entry" : "Extract Folder";
            addToolStripMenuItem.Visible = !FileCheck.isFile(e.Node.Text);
            exportToStandardFormatToolStripMenuItem1.Visible = FileCheck.isExportableToStandard(e.Node.Text);
            splitContainer1.Panel2.Controls.Clear();
            if (FileCheck.isFile(e.Node.Text))
            {
                string relativePath = PathParser.NodetoPath(e.Node);
                toolStripStatusLabel3.Text = e.Node.Text + " file Size: " + ConvertBytesToString(manager.GetFileSize(relativePath));
                //GK: Get FileType from name extension and determine how to load it
                Stream? file = manager.loadEntry(PathParser.NodetoPath(e.Node));
                if (file != null)
                {
                    editorFactory.openEditor(FileCheck.getFileType(file, e.Node.Text), splitContainer1.Panel2, e.Node);
                }

            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Close();
            }catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK);
            }
        }

        private void updateToolStripBar(string? fileName)
        {
            toolStripStatusLabel1.Text = fileName;
            if (fileName != null)
            {
                long bytes = manager.GetResourceFileSize();
                double covered = ((double)bytes) / UInt32.MaxValue;
                toolStripProgressBar1.Value = (int)(covered * 100);
                if (toolStripProgressBar1.Value >= warningPercent)
                {
                    toolStripProgressBar1.ForeColor = Color.Yellow;
                }
                else
                {
                    toolStripProgressBar1.ForeColor = Color.Green;
                }
                toolStripStatusLabel2.Text = ConvertBytesToString(bytes) + "/" + ConvertBytesToString(UInt32.MaxValue);
            } else
            {
                toolStripProgressBar1.Value = 0;
                toolStripProgressBar1.ForeColor = Color.Green;
                toolStripStatusLabel2.Text = null;
                toolStripStatusLabel1.Text = null;
                toolStripStatusLabel3.Text = null;
            }
        }

        public void executeSave()
        {
            this.saveFileToolStripMenuItem.PerformClick();
        }

        public void clearEditor()
        {
            this.splitContainer1.Panel2.Controls.Clear();
        }

        public void UpdateTitle(string? resourceName, bool isDirty)
        {
            if (resourceName != null)
            {
                this.Text = resourceName + (isDirty ? "*" : "") + " - " + DEFAULT_TITLE;
            } else
            {
                this.Text = DEFAULT_TITLE;
            }
            updateToolStripBar(resourceName);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch(ModifierKeys)
            {
                case Keys.Control:
                    switch(e.KeyCode)
                    {
                        case Keys.S:
                            executeSave();
                            break;
                    }
                    break;
            }
        }

        private void ManagerUi_FormClosed(object sender, FormClosedEventArgs e)
        {
            manager.CloseFile();
        }

        private void treeView1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop) && manager.GetResourceFileSize() > 0)
            {
                string[]? files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                importFilePaths(files);
            }
        }

        private void treeView1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop) && manager.GetResourceFileSize() > 0)
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void importFilePaths(string[]? files, string? parentDirectory = null)
        {
            if (files != null)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    Console.WriteLine(files[i]);
                    string relativeName = files[i].Substring(files[i].LastIndexOf(FileCheck.getPathSeparator(), StringComparison.InvariantCulture) + 1);
                    if (FileCheck.isFile(relativeName))
                    {
                        Stream file = File.OpenRead(files[i]);
                        TreeNode node = treeView1.SelectedNode;
                        importData(file, node, parentDirectory);
                    }
                    else
                    {
                        string[] directories = Directory.GetFiles(files[i], "*.*", SearchOption.AllDirectories);
                        importFilePaths(directories, relativeName);
                    }
                }
            }
        }

        private void importData(Stream myStream, TreeNode node, string? parentDirectory = null)
        {
            if (node == null)
            {
                MessageBox.Show("Please select a folder", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            string fullPath = ((FileStream)myStream).Name;
            string relativePath = fullPath.Substring(fullPath.LastIndexOf(FileCheck.getPathSeparator() + (parentDirectory != null ? parentDirectory : ""), StringComparison.InvariantCulture) + 1);
            if (Environment.OSVersion.Platform != PlatformID.Unix && Environment.OSVersion.Platform != PlatformID.MacOSX)
            {
                relativePath = relativePath.Replace("\\", "/");
            }
            string relativeDirectory = PathParser.NodetoPath(node);
            if (FileCheck.isFile(relativeDirectory))
            {
                MessageBox.Show("You have Selected a file. Please select a folder", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            relativePath = relativeDirectory + relativePath;
            manager.AddFile(myStream, relativePath);
            updateToolStripBar(toolStripStatusLabel1.Text);
        }

        private async void exportToStandardFormatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await ExportToStandard_logic();
        }

        private async Task ExportToStandard_logic()
        {
            TreeNode node = treeView1.SelectedNode;
            if (node == null)
            {
                MessageBox.Show("Please select a folder", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string relativePath = PathParser.NodetoPath(node);
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "Select Folder to export the file into";
            
            // Adding a check for the dialog result
            if (fbd.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(fbd.SelectedPath))
            {
                if (FileCheck.isFile(relativePath))
                {
                    await Task.Run(() => manager.ExportEntry(relativePath, fbd.SelectedPath));
                }
                else
                {
                    Task _ = Task.Run(() => manager.ExtractAndExportFolder(relativePath, fbd.SelectedPath));
                    this.ShowProgressBar("Exporting Files");
                }
            }
        }

        private async void exportToStandardFormatToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            await ExportToStandard_logic();
        }

        private static DialogResult InputBox(string title, string promptText, ref string value)
        {
            Form form = new Form
            {
                Text = title,
                ClientSize = new Size(380, 120),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen,
                MinimizeBox = false,
                MaximizeBox = false
            };
            Label label = new Label
            {
                AutoSize = true,
                Text = promptText
            };
            label.SetBounds(36, 36, 150, 13);
            TextBox textBox = new TextBox();
            textBox.SetBounds(36, 50, 300, 20);
            Button buttonOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK
            };
            buttonOk.SetBounds(36, 74, 60, 30);
            Button buttonCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel
            };
            buttonCancel.SetBounds(105, 74, 60, 30);

            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }

        private void CloseProgressForm(object? sender, EventArgs e)
        {
            this.extractProgressBar = null;
            this.extractProgressLabel = null;
        }

        private void ShowProgressBar(string title)
        {
            Form form = new Form
            {
                Text = title,
                ClientSize = new Size(380, 120),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen,
                MinimizeBox = false,
                MaximizeBox = false
            };
            this.extractProgressLabel = new Label
            {
                AutoSize = true
            };
            extractProgressLabel.SetBounds(36, 36, 150, 13);
            this.extractProgressBar = new ProgressBar();
            extractProgressBar.SetBounds(36, 50, 300, 20);

            form.Controls.AddRange([extractProgressLabel, extractProgressBar]);

            form.FormClosed += CloseProgressForm;

            form.ShowDialog();

        }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string version = "1.0.7";
            string author = "MadDeCoDeR";
            string modded_by = "Y-Dr. Now";
            string githubLink = "github.com/MadDeCoDeR/BFG-Resource-File-Manager/";
            
            MessageBox.Show(
                $"BFG Resource File Manager\nVersion: {version}\nAuthor: {author}\nModded by: {modded_by}\nGitHub: {githubLink}",
                "About",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }

}
