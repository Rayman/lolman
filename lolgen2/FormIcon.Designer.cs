namespace LanOfLegends.lolgen2
{
    partial class FormIcon
    {
        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナで生成されたコード

        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.btnSelectIcon = new System.Windows.Forms.Button();
            this.txtSelectedIcon = new System.Windows.Forms.TextBox();
            this.lvwIcons = new System.Windows.Forms.ListView();
            this.label1 = new System.Windows.Forms.Label();
            this.cmbTileSize = new System.Windows.Forms.ComboBox();
            this.buttonSelectIcon = new System.Windows.Forms.Button();
            this.iconPickerDialog = new LanOfLegends.lolgen2.IconPickerDialog();
            this.SuspendLayout();
            // 
            // btnSelectIcon
            // 
            this.btnSelectIcon.Location = new System.Drawing.Point(12, 13);
            this.btnSelectIcon.Name = "btnSelectIcon";
            this.btnSelectIcon.Size = new System.Drawing.Size(75, 21);
            this.btnSelectIcon.TabIndex = 0;
            this.btnSelectIcon.Text = "Select Icon";
            this.btnSelectIcon.UseVisualStyleBackColor = true;
            this.btnSelectIcon.Click += new System.EventHandler(this.btnSelectIcon_Click);
            // 
            // txtSelectedIcon
            // 
            this.txtSelectedIcon.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSelectedIcon.Location = new System.Drawing.Point(93, 13);
            this.txtSelectedIcon.Name = "txtSelectedIcon";
            this.txtSelectedIcon.ReadOnly = true;
            this.txtSelectedIcon.Size = new System.Drawing.Size(376, 20);
            this.txtSelectedIcon.TabIndex = 1;
            // 
            // lvwIcons
            // 
            this.lvwIcons.Activation = System.Windows.Forms.ItemActivation.OneClick;
            this.lvwIcons.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.lvwIcons.HideSelection = false;
            this.lvwIcons.Location = new System.Drawing.Point(12, 40);
            this.lvwIcons.MultiSelect = false;
            this.lvwIcons.Name = "lvwIcons";
            this.lvwIcons.OwnerDraw = true;
            this.lvwIcons.ShowItemToolTips = true;
            this.lvwIcons.Size = new System.Drawing.Size(457, 181);
            this.lvwIcons.TabIndex = 2;
            this.lvwIcons.TileSize = new System.Drawing.Size(64, 64);
            this.lvwIcons.UseCompatibleStateImageBehavior = false;
            this.lvwIcons.View = System.Windows.Forms.View.Tile;
            this.lvwIcons.DrawItem += new System.Windows.Forms.DrawListViewItemEventHandler(this.lvwIcons_DrawItem);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.Location = new System.Drawing.Point(12, 228);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(75, 22);
            this.label1.TabIndex = 3;
            this.label1.Text = "Tile Size";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cmbTileSize
            // 
            this.cmbTileSize.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbTileSize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTileSize.FormattingEnabled = true;
            this.cmbTileSize.Location = new System.Drawing.Point(93, 228);
            this.cmbTileSize.Name = "cmbTileSize";
            this.cmbTileSize.Size = new System.Drawing.Size(273, 21);
            this.cmbTileSize.TabIndex = 4;
            this.cmbTileSize.SelectedIndexChanged += new System.EventHandler(this.cmbTileSize_SelectedIndexChanged);
            // 
            // buttonSelectIcon
            // 
            this.buttonSelectIcon.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSelectIcon.Location = new System.Drawing.Point(372, 228);
            this.buttonSelectIcon.Name = "buttonSelectIcon";
            this.buttonSelectIcon.Size = new System.Drawing.Size(97, 23);
            this.buttonSelectIcon.TabIndex = 5;
            this.buttonSelectIcon.Text = "Select";
            this.buttonSelectIcon.UseVisualStyleBackColor = true;
            this.buttonSelectIcon.Click += new System.EventHandler(this.buttonSelectIcon_Click);
            // 
            // FormIcon
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(481, 259);
            this.Controls.Add(this.buttonSelectIcon);
            this.Controls.Add(this.cmbTileSize);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lvwIcons);
            this.Controls.Add(this.txtSelectedIcon);
            this.Controls.Add(this.btnSelectIcon);
            this.Name = "FormIcon";
            this.Text = "Icon Selector";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSelectIcon;
        private System.Windows.Forms.TextBox txtSelectedIcon;
        private System.Windows.Forms.ListView lvwIcons;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cmbTileSize;
        private IconPickerDialog iconPickerDialog;
        private System.Windows.Forms.Button buttonSelectIcon;
    }
}

