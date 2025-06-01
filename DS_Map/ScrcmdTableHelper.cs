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

namespace DSPRE
{
    public partial class ScrcmdTableHelper : Form
    {
        private readonly string _filePath;
        private readonly int _offsetStart;
        private readonly int _offsetEnd;
        public ScrcmdTableHelper()
        {
            InitializeComponent();
            _filePath = Filesystem.GetSynthOerlayPath(65);
            _offsetStart = Convert.ToInt32("17270", 16);
            _offsetEnd = Convert.ToInt32("17FF4", 16);


        }

        private void ScrcmdTableHelper_Load(object sender, EventArgs e)
        {
            if (!File.Exists(_filePath))
            {
                MessageBox.Show("File not found.");
                return;
            }

            if (_offsetEnd <= _offsetStart || _offsetEnd > new FileInfo(_filePath).Length)
            {
                MessageBox.Show("Invalid Offsets.");
                return;
            }

            using (FileStream fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(_offsetStart, SeekOrigin.Begin);
                int size = _offsetEnd - _offsetStart;

                if (size % 4 != 0)
                {
                    MessageBox.Show("Size non 4 multiple. Can't find addresses.");
                    return;
                }

                int count = size / 4;
                List<string> addresses = new List<string>();

                for (int i = 0; i < count; i++)
                {
                    byte[] buffer = new byte[4];
                    fs.Read(buffer, 0, 4);

                    // Petites ou grandes endian ?
                    uint address = BitConverter.ToUInt32(buffer, 0); // Big endian (02030430 → 0x02030430)
                                                                                         // uint address = BitConverter.ToUInt32(buffer, 0); // Little endian

                    addresses.Add($"0x{address-1:X8}");
                }

                PopulateGrid(addresses);
            }
        }

        private void PopulateGrid(List<string> addresses)
        {
            dataGridView2.Columns.Clear();

            dataGridView2.Columns.Add("CommandID", "Command ID");
            dataGridView2.Columns.Add("Offset", "Offset (hex)");
            dataGridView2.Columns.Add("Address", "Address");

            for (int i = 0; i < addresses.Count; i++)
            {
                int offset = _offsetStart + i * 4;
                string commandId = (i).ToString("X4"); // 0001, 0002, etc.

                dataGridView2.Rows.Add(commandId, $"0x{offset:X6}", addresses[i]);
            }
        }

        private void FindCommandId()
        {
            string cmdId = cmdIdTextbox.Text.Trim().ToUpper();

            if (string.IsNullOrEmpty(cmdId) || cmdId.Length != 4)
            {
                MessageBox.Show("Please enter a valid command ID (ex: 000A).");
                return;
            }

            foreach (DataGridViewRow row in dataGridView2.Rows)
            {
                if (row.Cells["CommandID"].Value?.ToString()?.ToUpper() == cmdId)
                {
                    // Sélectionner la ligne
                    row.Selected = true;
                    dataGridView2.CurrentCell = row.Cells[0];

                    // Faire défiler jusqu'à la ligne
                    dataGridView2.FirstDisplayedScrollingRowIndex = row.Index;
                    return;
                }
            }

            MessageBox.Show($"Command ID {cmdId} not found.");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FindCommandId();
        }

        private void dataGridView2_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
              if (e.RowIndex < 0) return; // Ignore clicks on the header row

            DataGridViewRow selectedRow = dataGridView2.Rows[e.RowIndex];

            string cmdId = selectedRow.Cells["CommandID"].Value?.ToString();
            string offset = selectedRow.Cells["Offset"].Value?.ToString();
            string address = selectedRow.Cells["Address"].Value?.ToString();

            

            AddressHelper form = new AddressHelper(Convert.ToInt32(address, 16));
            form.Show();
        }
    }
}
