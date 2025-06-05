using DSPRE.LibNDSFormats;
using Ekona.Images;
using MS.WindowsAPICodePack.Internal;
using NSMBe4.DSFileSystem;
using NSMBe4.NSBMD;
using ScintillaNET;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Shapes;
using static DSPRE.RomInfo;

namespace DSPRE.Editors
{
    public partial class BtxEditor : Form
    {

        private Bitmap bm;

        private byte[] BTXFile;

        public BtxEditor()
        {
            RomInfo.SetOWtable();
            RomInfo.Set3DOverworldsDict();
            RomInfo.ReadOWTable();
            InitializeComponent();
            overworldList.Items.Clear();

            overworldList.Items.Clear();
            foreach (ushort key in RomInfo.OverworldTable.Keys)
            {
                overworldList.Items.Add("OW Entry " + key);
            }
        }
       

        private void overworldList_SelectedIndexChanged(object sender, EventArgs e)
        {
            int selection = overworldList.SelectedIndex;
            if (overworldList.SelectedIndex < 0)
            {
                return;
            } else
            {
                showBtxFileButton.Enabled = true;
                exportImagePng.Enabled = true;
                importImagePng.Enabled = true;
            }
        
            ushort overlayTableEntryID = (ushort)RomInfo.OverworldTable.Keys.ElementAt(overworldList.SelectedIndex);
            uint spriteID = RomInfo.OverworldTable[overlayTableEntryID].spriteID;
            string path = RomInfo.gameDirs[DirNames.OWSprites].unpackedDir + "\\" + spriteID.ToString("D4");

            if(System.IO.File.Exists(path))
            {
                BTXFile = System.IO.File.ReadAllBytes(path);
                bm = BTX0.Read(BTXFile);
                if (bm != null)
                {
                    if (BTX0.PaletteSize == 64 && BTX0.PaletteCount == 2)
                    {
                        shinyCheckbox.Enabled = true;
                    } else
                    {
                        shinyCheckbox.Enabled = false;
                    }
                    overworldPictureBox.Width = bm.Width;
                    overworldPictureBox.Height = bm.Height;
                    overworldPictureBox.Image = bm;

                }
                else
                {
                    MessageBox.Show("This file is not supported.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
            } else
            {
                overworldPictureBox.Image = (Bitmap)Properties.Resources.ResourceManager.GetObject("overworldUnreadable"); 
            }
           
        }

        private void shinyCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (shinyCheckbox.Checked)
            {
                BTX0.PaletteIndex = 1u;
            }
            else
            {
                BTX0.PaletteIndex = 0u;
            }
            if (shinyCheckbox.Enabled)
            {
                bm = BTX0.Read(BTXFile);
                overworldPictureBox.Image = bm;
            }
        }

        private void exportImagePng_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Save As";
            saveFileDialog.Filter = "PNG (*.png)|*.png";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                bm.Save(saveFileDialog.FileName);
            }
        }

        private void importImagePng_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Open";
            openFileDialog.Filter = "PNG (*.png)|*.png";
            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            Bitmap bitmap = new Bitmap(openFileDialog.FileName);
            if (bm.Width == bitmap.Width && bm.Height == bitmap.Height)
            {
                if (GetColorCount(bitmap) <= BTX0.ColorCount)
                {
                    BTXFile = BTX0.Write(BTXFile, bitmap);
                    bm = BTX0.Read(BTXFile);
                    overworldPictureBox.Image = bm;

                    ushort overlayTableEntryID = (ushort)RomInfo.OverworldTable.Keys.ElementAt(overworldList.SelectedIndex);
                    uint spriteID = RomInfo.OverworldTable[overlayTableEntryID].spriteID;
                    string path = RomInfo.gameDirs[DirNames.OWSprites].unpackedDir + "\\" + spriteID.ToString("D4");
                    try { 
                        System.IO.File.WriteAllBytes(path, BTXFile);
                        MessageBox.Show($"BTX {spriteID} has been saved successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    } catch (Exception err)
                    {
                        MessageBox.Show($"Error while saving the file: {err.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                }
                else
                {
                    MessageBox.Show("Too many colors!\nBTX: " + BTX0.ColorCount + "\nPNG: " + GetColorCount(bitmap), "Information", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
            }
            else
            {
                MessageBox.Show("Not the same size!\nBTX: " + bm.Width + "x" + bm.Height + "\nPNG: " + bitmap.Width + "x" + bitmap.Height, "Information", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }

        private uint GetColorCount(Bitmap temp)
        {
            HashSet<Color> hashSet = new HashSet<Color>();
            uint num = 0u;
            uint num2 = 0u;
            for (int i = 0; i < temp.Width * temp.Height; i++)
            {
                hashSet.Add(temp.GetPixel((int)num, (int)num2));
                num++;
                if (num >= temp.Width)
                {
                    num = 0u;
                    num2++;
                }
            }
            return (uint)hashSet.Count;
        }

        private void showBtxFileButton_Click(object sender, EventArgs e)
        {
            ushort overlayTableEntryID = (ushort)RomInfo.OverworldTable.Keys.ElementAt(overworldList.SelectedIndex);
            uint spriteID = RomInfo.OverworldTable[overlayTableEntryID].spriteID;
            string path = RomInfo.gameDirs[DirNames.OWSprites].unpackedDir + "\\" + spriteID.ToString("D4");
            Helpers.ExplorerSelect(path);
        }
    }
}
