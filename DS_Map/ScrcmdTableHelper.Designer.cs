namespace DSPRE
{
    partial class ScrcmdTableHelper
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
            this.dataGridView2 = new System.Windows.Forms.DataGridView();
            this.button2 = new System.Windows.Forms.Button();
            this.cmdIdTextbox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView2
            // 
            this.dataGridView2.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView2.Location = new System.Drawing.Point(12, 58);
            this.dataGridView2.Name = "dataGridView2";
            this.dataGridView2.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView2.Size = new System.Drawing.Size(366, 258);
            this.dataGridView2.TabIndex = 0;
            this.dataGridView2.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView2_CellDoubleClick);
            // 
            // button2
            // 
            this.button2.Image = global::DSPRE.Properties.Resources.wideLensImage;
            this.button2.Location = new System.Drawing.Point(283, 13);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(95, 39);
            this.button2.TabIndex = 1;
            this.button2.Text = "Search";
            this.button2.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // cmdIdTextbox
            // 
            this.cmdIdTextbox.Location = new System.Drawing.Point(12, 29);
            this.cmdIdTextbox.Name = "cmdIdTextbox";
            this.cmdIdTextbox.Size = new System.Drawing.Size(133, 20);
            this.cmdIdTextbox.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 13);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(68, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Command ID";
            // 
            // ScrcmdTableHelper
            // 
            this.ClientSize = new System.Drawing.Size(390, 328);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cmdIdTextbox);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.dataGridView2);
            this.MaximumSize = new System.Drawing.Size(406, 367);
            this.MinimumSize = new System.Drawing.Size(406, 367);
            this.Name = "ScrcmdTableHelper";
            this.Text = "Scrcmd Table Helper";
            this.Load += new System.EventHandler(this.ScrcmdTableHelper_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.DataGridView dataGridView2;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox cmdIdTextbox;
        private System.Windows.Forms.Label label2;
    }
}