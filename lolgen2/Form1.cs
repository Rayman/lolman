using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;

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
                //Get the folder
                string folder = (new DirectoryInfo(textBoxFolder.Text + Path.DirectorySeparatorChar)).ToString();

                //Get the icon
                if (this.lolIcon == null)
                {
                    Bitmap bmp = this.pictureBox1.Image as Bitmap;
                    if (bmp != null)
                    {
                        backgroundWorker = new DirectorySummer(folder, textBoxGameName.Text, bmp);
                    }
                    else
                    {
                        backgroundWorker = new DirectorySummer(folder, textBoxGameName.Text);
                    }
                }
                else
                {
                    backgroundWorker = new DirectorySummer(folder, textBoxGameName.Text, this.lolIcon.ToBitmap());
                }

                //Add some event handlers
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

        private void textBoxFolder_Changed(object sender, EventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                //Search for a lol.info.xml, and get all info from it so the user don't have to
                DirectoryInfo info = new DirectoryInfo(textBoxFolder.Text);
                textBoxGameName.Text = info.Name;
                FileInfo file = new FileInfo(info.FullName + Path.DirectorySeparatorChar + "lol.info.xml");
                if (file.Exists)
                {
                    using (FileStream fs = file.OpenRead())
                    {
                        try
                        {
                            XmlDocument doc = new XmlDocument();
                            doc.Load(fs);
                            XmlElement root = doc.DocumentElement;
                            foreach (XmlNode child in root.ChildNodes)
                            {
                                if (child.Name == "name")
                                {
                                    textBoxGameName.Text = child.InnerText;
                                }
                                else if (child.Name == "icon")
                                {
                                    byte[] data;
                                    XmlAttribute att = child.Attributes["compression"];

                                    data = Convert.FromBase64String(child.InnerText);

                                    if (att != null && att.Value == "gzip")
                                    {
                                        data = GzipUtils.Decompress(data);
                                    }

                                    using (MemoryStream ms = new MemoryStream(data))
                                    {
                                        Bitmap bmp = new Bitmap(ms);
                                        pictureBox1.Image = bmp;
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            return;
                        }
                    }
                }
            }
        }
    }
}
