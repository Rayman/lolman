using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Collections.Generic;

namespace LanOfLegends.lolgen2
{
    public partial class FormIcon : Form
    {
        private class TileSizeComboBoxItem
        {
            public Size Size { get; private set; }

            public TileSizeComboBoxItem(Size size)
            {
                this.Size = size;
            }

            public override string ToString()
            {
                return String.Format("{0} x {1}", this.Size.Width, this.Size.Height);
            }
        }

        private class IconListViewItem : ListViewItem
        {
            public Icon Icon { get; set; }
        }

        private static readonly Padding TilePadding = new Padding(2, 1, 2, 1);

        public FormIcon()
        {
            InitializeComponent();

            // Initialize ComboBox
            for (int s = 16; s <= 256; s *= 2)
            {
                TileSizeComboBoxItem item = new TileSizeComboBoxItem(new Size(s, s));
                this.cmbTileSize.Items.Add(item);
            }

            this.cmbTileSize.SelectedIndex = 1;
        }

        private int GetIconBitDepth(Icon icon)
        {
            if (icon == null)
            {
                throw new ArgumentNullException("icon");
            }

            byte[] data = null;
            using (MemoryStream stream = new MemoryStream())
            {
                icon.Save(stream);
                data = stream.ToArray();
            }

            return BitConverter.ToInt16(data, 12);
        }

        private void ClearAllIcons()
        {
            if (this.lvwIcons.Tag is Icon)
            {
                ((Icon)this.lvwIcons.Tag).Dispose();
                this.lvwIcons.Tag = null;
            }

            foreach (ListViewItem item in this.lvwIcons.Items)
            {
                if (item is IconListViewItem)
                {
                    //Dont dispose lolIcon plx
                    IconListViewItem temp;
                    if ((temp = item as IconListViewItem) != null)
                        if (temp.Icon == ((Form1)this.Owner).lolIcon)
                            continue;
                    //Else dispose!
                    ((IconListViewItem)item).Icon.Dispose();
                }
            }

            this.lvwIcons.Items.Clear();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            ClearAllIcons();
        }

        private void btnSelectIcon_Click(object sender, EventArgs e)
        {
            DialogResult result = this.iconPickerDialog.ShowDialog(this);
            if (result != DialogResult.OK)
            {
                return;
            }

            this.txtSelectedIcon.Text = this.iconPickerDialog.Filename + ", " + this.iconPickerDialog.IconIndex.ToString();

            Icon icon = null;
            Icon[] splitIcons = null;
            try
            {
                if (Path.GetExtension(this.iconPickerDialog.Filename).ToLower() == ".ico")
                {
                    icon = new Icon(this.iconPickerDialog.Filename);
                }
                else
                {
                    using (IconExtractor extractor = new IconExtractor(this.iconPickerDialog.Filename))
                    {
                        icon = extractor.GetIcon(this.iconPickerDialog.IconIndex);
                    }
                }

                splitIcons = IconExtractor.SplitIcon(icon);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "IconExtractor Demo - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Update Icons.
            this.Icon = icon;
            //this.notifyIcon.Icon = icon;

            // Update ListView
            this.lvwIcons.BeginUpdate();
            ClearAllIcons();

            this.lvwIcons.Tag = icon;
            foreach (Icon i in splitIcons)
            {
                IconListViewItem item = new IconListViewItem();
                item.ToolTipText = String.Format("{0} x {1}, {2}bits", i.Width, i.Height, GetIconBitDepth(i));
                item.Icon = i;

                this.lvwIcons.Items.Add(item);
            }

            this.lvwIcons.EndUpdate();
        }

        private void cmbTileSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            TileSizeComboBoxItem item = ((ComboBox)sender).SelectedItem as TileSizeComboBoxItem;
            if (item == null)
            {
                return;
            }

            // Change TileSize of the ListView
            this.lvwIcons.BeginUpdate();

            this.lvwIcons.TileSize
                = new Size(item.Size.Width + TilePadding.Horizontal, item.Size.Height + TilePadding.Vertical);
            if (this.lvwIcons.Items.Count != 0)
            {
                this.lvwIcons.RedrawItems(0, this.lvwIcons.Items.Count - 1, false);
            }

            this.lvwIcons.EndUpdate();
        }

        private void lvwIcons_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            IconListViewItem item = e.Item as IconListViewItem;
            if (item == null)
            {
                e.DrawDefault = true;
                return;
            }

            // Draw item
            e.DrawBackground();
            e.Graphics.DrawRectangle(SystemPens.ControlLight, e.Bounds);

            int x = e.Bounds.X + (e.Bounds.Width - item.Icon.Width) / 2;
            int y = e.Bounds.Y + (e.Bounds.Height - item.Icon.Height) / 2;
            Rectangle rect = new Rectangle(x, y, item.Icon.Width, item.Icon.Height);
            Region clipReg = new Region(e.Bounds);
            e.Graphics.Clip = clipReg;
            e.Graphics.DrawIcon(item.Icon, rect);
        }

        public Icon lolIcon { get; set; }

        private void buttonSelectIcon_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.ListView.SelectedListViewItemCollection select = this.lvwIcons.SelectedItems;
            if (select.Count != 1)
            {
                MessageBox.Show("You must select one icon!");
                return;
            }
            IconListViewItem selection = select[0] as IconListViewItem;
            if (selection != null)
            {
                Form1 parent = (Form1)this.Owner;
                parent.lolIcon = selection.Icon;
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("Error, no icon selected");
            }
        }
    }
}
