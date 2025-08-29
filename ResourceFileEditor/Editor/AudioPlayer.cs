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
using ResourceFileEditor.Manager.Audio;
using ResourceFileEditor.utils;
using System;
using System.IO;
using System.Media;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace ResourceFileEditor.Editor
{
    sealed class AudioPlayer : Editor, IDisposable
    {
        private readonly ManagerImpl manager;
        private SoundPlayer? audioPlayer;
        private Button? playButton;
        private Label? timeLabel;
        private Label? infoLabel;
        private Timer? progressTimer;
        private DateTime startTime;
        private int totalDurationSec;
        private MemoryStream? audioStream;
        private double elapsedSamplesDouble;
        private ProgressBar? progressBar;

        private AudioInfo? audioInfo;

        public AudioPlayer(ManagerImpl manager)
        {
            this.manager = manager;
            totalDurationSec = 0;
        }

        public void start(Panel panel, TreeNode node)
        {
            string relativePath = PathParser.NodetoPath(node);
            Stream? file = manager.loadEntry(relativePath);
            if (file != null)
            {
                try
                {
                    file.Seek(0, SeekOrigin.Begin);

                    this.audioInfo = null;
                    if (relativePath.EndsWith("idwav", StringComparison.OrdinalIgnoreCase))
                    {
                        // Load the file and get the audio information
                        file = AudioManager.LoadFile(file, out this.audioInfo);
                        CalculateDuration(this.audioInfo!);
                    }

                    // Copy stream to MemoryStream for repeat playback
                    audioStream = new MemoryStream();
                    file.CopyTo(audioStream);
                    audioStream.Position = 0;

                    // Create a player
                    audioPlayer = new SoundPlayer(audioStream);

                    // Control panel
                    Panel playerPanel = new Panel
                    {
                        Width = panel.Width,
                        Height = panel.Height,
                        Dock = DockStyle.Fill
                    };
                    playerPanel.ParentChanged += diposePanel;

                    // Play/Pause button
                    playButton = new Button
                    {
                        Text = "▶ Play",
                        Location = new System.Drawing.Point(20, 20),
                        Width = 80
                    };
                    playButton.Click += playPause;

                    // Time indication
                    timeLabel = new Label
                    {
                        Location = new System.Drawing.Point(110, 25),
                        Width = 150,
                        Text = $"0:00 / {totalDurationSec / 60}:{totalDurationSec % 60:D2}"
                    };

                    // Format information
                    infoLabel = new Label
                    {
                        Location = new System.Drawing.Point(20, 60),
                        Width = panel.Width - 40,
                        Text = "Format: Unknown | Channels: ? | Sample Rate: ? Hz"
                    };

                    // Updating audio file information
                    if (audioInfo != null)
                    {
                        string formatStr = "Unknown";
                        switch(audioInfo.Format)
                        {
                            case AudioFormat.PCM:
                                formatStr = "PCM";
                                break;
                            case AudioFormat.ADPCM:
                                formatStr = "ADPCM";
                                break;
                            case AudioFormat.XMA2:
                                formatStr = "XMA2";
                                break;
                            case AudioFormat.Extensible:
                                formatStr = "Extensible";
                                break;
                        }

                        infoLabel.Text = $"Format: {formatStr} | Channels: {audioInfo.Channels} | Sample Rate: {audioInfo.SampleRate} Hz";
                    }

                    // Timer for time update
                    progressTimer = new Timer { Interval = 200 };
                    progressTimer.Tick += UpdateTimeLabel;

                    // Add items to the panel
                    playerPanel.Controls.AddRange(new Control[] { playButton, timeLabel, infoLabel });
                    panel.Controls.Add(playerPanel);

                    progressBar = new ProgressBar
                    {
                        Location = new System.Drawing.Point(20, 100),
                        Width = panel.Width - 40,
                        Minimum = 0,
                        Maximum = 100,
                        Value = 0,
                        Style = ProgressBarStyle.Continuous
                    };
                    playerPanel.Controls.Add(progressBar);
                }
                finally
                {
                    file.Dispose();
                }
            }
        }

        private void CalculateDuration(AudioInfo info)
        {
            if (info.IsXma2)
            {
                totalDurationSec = (int)Math.Round(info.Xma2PlayLength / (double)info.SampleRate);
            }
            else if (info.TotalSamples > 0)
            {
                totalDurationSec = (int)Math.Round(info.TotalSamples / (double)info.SampleRate);
            }
            else
            {
                totalDurationSec = 0;
            }

            // Set the minimum duration to 1 second, if it is zero
            if (totalDurationSec <= 0)
                totalDurationSec = 1;
        }

        private void UpdateTimeLabel(object? sender, EventArgs e)
        {
            if (audioPlayer == null || playButton == null || timeLabel == null || this.audioInfo == null || progressBar == null)
                return;

            double bytesPerSecond = this.audioInfo.SampleRate * this.audioInfo.Channels * (this.audioInfo.BitsPerSample / 8.0);
            double secondsPerTick = 0.2; //  Timer interval in seconds (200 ms)
            double expectedSamples = secondsPerTick * this.audioInfo.SampleRate;

            elapsedSamplesDouble += secondsPerTick * this.audioInfo.SampleRate;

            double currentSec = elapsedSamplesDouble / this.audioInfo.SampleRate;

            // Give a 0.4 second margin to end playback
            if (currentSec >= totalDurationSec + 0.4)
            {
                progressTimer?.Stop();
                audioPlayer.Stop();
                playButton.Text = "▶ Play";
                elapsedSamplesDouble = 0;
                timeLabel.Text = $"0:00 / {totalDurationSec / 60}:{totalDurationSec % 60:D2}";
                progressBar.Value = 0;
            }
            else
            {
                timeLabel.Text = $"{(int)(currentSec / 60)}:{(int)(currentSec % 60):D2} / {totalDurationSec / 60}:{totalDurationSec % 60:D2}";

                double percentDouble = (elapsedSamplesDouble / (audioInfo.SampleRate * totalDurationSec)) * 100.0;
                int percent = Math.Max(0, Math.Min(100, (int)Math.Round(percentDouble)));
                progressBar.Value = percent;
            }
         }

        private void playPause(object? sender, EventArgs e)
        {
            if (audioPlayer != null && playButton != null && audioStream != null)
            {
                if (playButton.Text.StartsWith('▶'))
                {
                    audioStream.Position = 0;
                    audioPlayer.Play();
                    playButton.Text = "⏹ Stop";
                    startTime = DateTime.Now;
                    elapsedSamplesDouble = 0;
                    progressTimer?.Start();
                }
                else
                {
                    audioPlayer.Stop();
                    playButton.Text = "▶ Play";
                    progressTimer?.Stop();
                    elapsedSamplesDouble = 0; // Reset sample counter
                    timeLabel!.Text = $"0:00 / {totalDurationSec / 60}:{totalDurationSec % 60:D2}";
                    if (progressBar != null) progressBar.Value = 0;
                }
            }
        }

        public void Dispose()
        {
            audioPlayer?.Stop();
            audioPlayer?.Dispose();
            audioStream?.Dispose();
            progressTimer?.Stop();
            progressTimer?.Dispose();
        }

        private void diposePanel(object? sender, EventArgs e)
        {
            if (sender is Panel p && p.Parent == null)
                Dispose();
        }
    }
}