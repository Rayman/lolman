﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LanOfLegends.lolgen2
{
    public partial class Form1 : Form
    {
        private DirectorySummer backgroundWorker;

        public Form1()
        {
            InitializeComponent();
        }

        private void buttonSelectFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "Select the folder of the game";
            dialog.ShowNewFolderButton = false;
            dialog.Tag = "asdf";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string folder = (new DirectoryInfo(dialog.SelectedPath)).FullName;
                textBoxFolder.Text = folder;
            }
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            try
            {
                string folder = (new DirectoryInfo(textBoxFolder.Text + Path.DirectorySeparatorChar)).ToString();
                backgroundWorker = new DirectorySummer(folder, textBoxGameName.Text, this.lolIcon);
                backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(summerProgressChanged);
                backgroundWorker.RunWorkerCompleted += delegate(object send, RunWorkerCompletedEventArgs args)
                {
                    MessageBox.Show("Done!");
                };
                backgroundWorker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        void summerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.progressBar1.Value = e.ProgressPercentage;
            this.labelStatus.Text = e.UserState as string;
        }

        internal Icon lolIcon;

        private void buttonGetIcon_Click(object sender, EventArgs e)
        {
            FormIcon dialog = new FormIcon();
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                if (this.lolIcon != null)
                {
                    pictureBox1.Image = this.lolIcon.ToBitmap();
                }
            }
        }
    }
}
