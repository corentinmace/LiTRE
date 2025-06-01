using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms;
using static DSPRE.DSUtils;

namespace DSPRE
{
    public partial class AddressHelper : Form
    {

        private int overlaysSize = OverlayUtils.OverlayTable.GetNumberOfOverlays();
        private int ARM9LoadAddress = 0x02000000;
        private int baseAddress = 0;

        public AddressHelper(int address = 0)
        {
            InitializeComponent();
            baseAddress = address;
            if(baseAddress != 0)
            {
                searchAddress(address);
                AddressInput.Text = $"0x{address :X8}";
            }
        }

        private void searchAddressButton_Click(object sender, EventArgs e)
        {
            searchAddress(Convert.ToInt32(AddressInput.Text, 16));
        }

        private List<int> getOverlayNumberFromAddress(int address)
        {
            List<int> overlayNumbers = new List<int>();


            for (int i = 0; i < overlaysSize - 1; i++)
            {
                uint currentOvlAddress = OverlayUtils.OverlayTable.GetRAMAddress(i);
                bool checkOverlayN = currentOvlAddress >= address;
                bool checkOverlayN1 = address < (currentOvlAddress + OverlayUtils.OverlayTable.GetUncompressedSize(i));
                
                if (checkOverlayN && checkOverlayN1)
                {
                    overlayNumbers.Add(i);
                }
            }


            return overlayNumbers;
        }

        private string getOffsetInOverlay(int address, int ovlNumber)
        {
            return $"0x{OverlayUtils.OverlayTable.GetRAMAddress(ovlNumber) - address:X4}";
        }

        private void AddressHelper_Load(object sender, EventArgs e)
        {

        }

        private void searchAddress(int address) {
            addressesGrid.Rows.Clear();
            try
            {
                
                List<int> foundInOvl = getOverlayNumberFromAddress(address);
                for (int i = 0; i < foundInOvl.Count; i++)
                {
                    int ovl = foundInOvl[i];

                    addressesGrid.Rows.Add("Overlay " + ovl, getOffsetInOverlay(address, ovl));
                }


                bool addressToARMLoad = address >= ARM9LoadAddress;
                bool addressToARMEnd = address < OverlayUtils.OverlayTable.GetRAMAddress(0);

                if (addressToARMLoad && addressToARMEnd)
                {
                    addressesGrid.Rows.Clear();
                    addressesGrid.Rows.Add("ARM9", $"0x{(address - ARM9LoadAddress):X4}");
                }



                if (address >= RomInfo.synthOverlayLoadAddress)
                {
                    addressesGrid.Rows.Add("SynthOVL", $"0x{address - RomInfo.synthOverlayLoadAddress:X4}");
                }
            }
            catch
            {
                MessageBox.Show("No overlay found");
            }
        }
    }


}
