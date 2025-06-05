using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DSPRE.Editors
{
    public partial class TextFormatter : Form
    {
        public TextFormatter()
        {
            InitializeComponent();
            
        }

        private void redColorButton_click(object sender, EventArgs e)
        {
            if (inputTextbox.SelectedText.Length == 0) return;
            inputTextbox.Text = inputTextbox.Text.Replace(inputTextbox.SelectedText,
            ColorFormatter.AddColorCode(inputTextbox.SelectedText, "red", 4));
        }

        private void greenColorButton_Click(object sender, EventArgs e)
        {
            if (inputTextbox.SelectedText.Length == 0) return;
            inputTextbox.Text = inputTextbox.Text.Replace(inputTextbox.SelectedText,
            ColorFormatter.AddColorCode(inputTextbox.SelectedText, "green", 4));
        }

        private void blueColorButton_Click(object sender, EventArgs e)
        {
            if (inputTextbox.SelectedText.Length == 0) return;
            inputTextbox.Text = inputTextbox.Text.Replace(inputTextbox.SelectedText,
            ColorFormatter.AddColorCode(inputTextbox.SelectedText, "blue", 4));
        }

        private void inputTextbox_KeyUp(object sender, KeyEventArgs e)
        {
            outputBox.Text = TextFormatterUtils.TextToDSPRE(inputTextbox.Text, 4, true);
        }
    }
}
