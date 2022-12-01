using System;
using System.IO;

namespace gubbuh.Memory
{
    public class RomLoader
    {
        protected byte[] rom_array;
        public Rom rom;
        public MMU mmu;
        
        public RomLoader(string filename)
        {
            rom_array = File.ReadAllBytes(filename);
            rom = new Rom();
            rom.Title = System.Text.Encoding.Default.GetString(rom_array.Subsequence(0x134, 0x144-0x134));
            rom.Manufacturer = System.Text.Encoding.Default.GetString(rom_array.Subsequence(0x13f, 0x143-0x13f));
            rom.OldLicensee = rom_array[0x14b];
            if (rom.OldLicensee == 0x33)
                rom.Licensee = System.Text.Encoding.Default.GetString(rom_array.Subsequence(0x144, 0x146-0x144));
            rom.Sgb = rom_array[0x146] == 0x03;
            rom.CartType = (MBC) rom_array[0x147];
            rom.RomSize = (RomSize) rom_array[0x148];
            rom.ExRamSize = (ExRamSize) rom_array[0x149];
            rom.Destination = rom_array[0x14a];

            mmu = new MMU((int) Math.Pow(2, ((int)rom.RomSize + 1)), 1);
            
            for (int i = 0; i < rom_array.Length; i++)
            {
                mmu.Write(i,rom_array[i]);
            }
        }
        
        
    }
}