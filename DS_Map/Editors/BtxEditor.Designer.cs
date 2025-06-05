namespace DSPRE.Editors
{
    partial class BtxEditor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.overworldList = new System.Windows.Forms.ComboBox();
            this.overworldPictureBox = new System.Windows.Forms.PictureBox();
            this.showBtxFileButton = new System.Windows.Forms.Button();
            this.exportImagePng = new System.Windows.Forms.Button();
            this.importImagePng = new System.Windows.Forms.Button();
            this.shinyCheckbox = new System.Windows.Forms.CheckBox();
            this.panel1 = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.overworldPictureBox)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(55, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Overworld";
            // 
            // overworldList
            // 
            this.overworldList.FormattingEnabled = true;
            this.overworldList.Location = new System.Drawing.Point(12, 29);
            this.overworldList.Name = "overworldList";
            this.overworldList.Size = new System.Drawing.Size(125, 21);
            this.overworldList.TabIndex = 1;
            this.overworldList.SelectedIndexChanged += new System.EventHandler(this.overworldList_SelectedIndexChanged);
            // 
            // overworldPictureBox
            // 
            this.overworldPictureBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.overworldPictureBox.Location = new System.Drawing.Point(0, 0);
            this.overworldPictureBox.Name = "overworldPictureBox";
            this.overworldPictureBox.Size = new System.Drawing.Size(88, 209);
            this.overworldPictureBox.TabIndex = 2;
            this.overworldPictureBox.TabStop = false;
            // 
            // showBtxFileButton
            // 
            this.showBtxFileButton.Enabled = false;
            this.showBtxFileButton.Image = global::DSPRE.Properties.Resources.lens;
            this.showBtxFileButton.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.showBtxFileButton.Location = new System.Drawing.Point(16, 175);
            this.showBtxFileButton.Name = "showBtxFileButton";
            this.showBtxFileButton.Size = new System.Drawing.Size(121, 23);
            this.showBtxFileButton.TabIndex = 3;
            this.showBtxFileButton.Text = "Show BTX File";
            this.showBtxFileButton.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.showBtxFileButton.UseVisualStyleBackColor = true;
            this.showBtxFileButton.Click += new System.EventHandler(this.showBtxFileButton_Click);
            // 
            // exportImagePng
            // 
            this.exportImagePng.Enabled = false;
            this.exportImagePng.Image = global::DSPRE.Properties.Resources.exportArrow;
            this.exportImagePng.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.exportImagePng.Location = new System.Drawing.Point(16, 117);
            this.exportImagePng.Name = "exportImagePng";
            this.exportImagePng.Size = new System.Drawing.Size(121, 23);
            this.exportImagePng.TabIndex = 4;
            this.exportImagePng.Text = "Export PNG";
            this.exportImagePng.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.exportImagePng.UseVisualStyleBackColor = true;
            this.exportImagePng.Click += new System.EventHandler(this.exportImagePng_Click);
            // 
            // importImagePng
            // 
            this.importImagePng.Enabled = false;
            this.importImagePng.Image = global::DSPRE.Properties.Resources.importArrow;
            this.importImagePng.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.importImagePng.Location = new System.Drawing.Point(16, 146);
            this.importImagePng.Name = "importImagePng";
            this.importImagePng.Size = new System.Drawing.Size(121, 23);
            this.importImagePng.TabIndex = 5;
            this.importImagePng.Text = "Import PNG";
            this.importImagePng.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.importImagePng.UseVisualStyleBackColor = true;
            this.importImagePng.Click += new System.EventHandler(this.importImagePng_Click);
            // 
            // shinyCheckbox
            // 
            this.shinyCheckbox.AutoSize = true;
            this.shinyCheckbox.Enabled = false;
            this.shinyCheckbox.Location = new System.Drawing.Point(13, 57);
            this.shinyCheckbox.Name = "shinyCheckbox";
            this.shinyCheckbox.Size = new System.Drawing.Size(52, 17);
            this.shinyCheckbox.TabIndex = 6;
            this.shinyCheckbox.Text = "Shiny";
            this.shinyCheckbox.UseVisualStyleBackColor = true;
            this.shinyCheckbox.CheckedChanged += new System.EventHandler(this.shinyCheckbox_CheckedChanged);
            // 
            // panel1
            // 
            this.panel1.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.panel1.AutoScroll = true;
            this.panel1.Controls.Add(this.overworldPictureBox);
            this.panel1.Location = new System.Drawing.Point(143, 1);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(88, 204);
            this.panel1.TabIndex = 7;
            // 
            // BtxEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(232, 209);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.shinyCheckbox);
            this.Controls.Add(this.importImagePng);
            this.Controls.Add(this.exportImagePng);
            this.Controls.Add(this.showBtxFileButton);
            this.Controls.Add(this.overworldList);
            this.Controls.Add(this.label1);
            this.MaximumSize = new System.Drawing.Size(350, 248);
            this.MinimumSize = new System.Drawing.Size(248, 248);
            this.Name = "BtxEditor";
            this.Text = "Overworld (BTX) Editor";
            ((System.ComponentModel.ISupportInitialize)(this.overworldPictureBox)).EndInit();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox overworldList;
        private System.Windows.Forms.PictureBox overworldPictureBox;
        private System.Windows.Forms.Button showBtxFileButton;
        private System.Windows.Forms.Button exportImagePng;
        private System.Windows.Forms.Button importImagePng;
        private System.Windows.Forms.CheckBox shinyCheckbox;
        private System.Windows.Forms.Panel panel1;
    }
}