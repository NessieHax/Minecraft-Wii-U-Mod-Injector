using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Memory;

namespace Minecraft_Wii_U_Mod_Injector.CEMU
{
    public class CemuMemory
    {
        public Mem CemuMem = new();

        public void Attach(string process)
        {
            CemuMem.OpenProcess(process);
        }

        public void Detach()
        {
            CemuMem.CloseProcess();
        }

        public async Task<string> GetEntryPointAsync(string aobScan)
        {
            IEnumerable<long> aoBScanResults = await CemuMem.AoBScan(0x10000000000, 0x30000000000, aobScan, true, false);
            long singleAoBScanResult = aoBScanResults.FirstOrDefault();
            return singleAoBScanResult.ToString();
        }

        public void WriteBytes(string entryPoint, string address, string value)
        {
            long longAddr = Convert.ToInt64(long.Parse(entryPoint, System.Globalization.NumberStyles.HexNumber)) + Convert.ToInt64(long.Parse(address, System.Globalization.NumberStyles.HexNumber));
            string finalAdr = longAddr.ToString("X8");

            MessageBox.Show(finalAdr); //just for testing

            CemuMem.WriteMemory(finalAdr, "bytes", value);
        }
    }
}
