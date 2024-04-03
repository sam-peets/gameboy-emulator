using System;
using System.Runtime.InteropServices;

namespace gubbuh.Memory
{
    public class MMU
    {
        protected Block[] RomBanks;
        protected Block VRam;
        protected Block[] ExRam;
        protected Block WRam1;
        protected Block WRam2;
        protected Block Oam;
        protected Block HRam;

        private int ly = 0;
        public MMU(int romBanks, int exBanks)
        {
            RomBanks = new Block[romBanks];
            for (var i = 0; i < RomBanks.Length; i++)
            {
                RomBanks[i] = new Block(0x3fff + 1);
            }

            VRam = new Block(0x1fff + 1);

            ExRam = new Block[exBanks];
            for (var i = 0; i < ExRam.Length; i++)
            {
                ExRam[i] = new Block(0x1fff + 1);
            }

            WRam1 = new Block(0xfff + 1);
            WRam2 = new Block(0xfff + 1);

            Oam = new Block(0x9f + 1);

            HRam = new Block(0x7e + 1);
            
        }

        public byte Read(int pos)
        {
            if (pos <= 0x3fff)
            {
                return RomBanks[0].read(pos);
            }

            if (pos <= 0x7fff)
            {
                // TODO add support for bank switching
                return RomBanks[1].read(pos-0x4000);
            }

            if (pos <= 0x9fff)
            {
                return VRam.read(pos - 0x8000);
            }

            if (pos <= 0xbfff)
            {
                // TODO swapping
                return ExRam[0].read(pos - 0xa000);
            }

            if (pos <= 0xcfff)
            {
                return WRam1.read(pos - 0xc000);
            }

            if (pos <= 0xdfff)
            {
                return WRam2.read(pos - 0xd000);
            }

            if (pos <= 0xfdff)
            {
                // echo ram
                Console.WriteLine("read from echo ram at " + pos.ToString("x8") + ", double check this");
                Read(pos - 0x2000);
            }

            if (pos <= 0xfe9f)
            {
                return Oam.read(pos - 0xfe00);
            }

            if (pos <= 0xfeff)
            {
                Console.WriteLine("read from not usable range at " + pos.ToString("x8") + ", double check me");
                return 0xff;
            }

            if (pos <= 0xff7f)
            {
                switch (pos & 0xff)
                {
                    case 0x44:
                        // TODO implement
                        return 0x90;
                    case 0x00:
                        // TODO joypad register
                        return 0xff;
                    case 0x4d:
                        // todo this is a hack to get blargg tests to run
                        return 0xff;
                }
                Console.WriteLine("UNIMPLEMENTED IO REGISTER READ " + pos.ToString("x4"));
                return 0xff;
            }

            if (pos <= 0xfffe)
            {
                return HRam.read(pos - 0xff80);
            }

            if (pos == 0xffff)
            {
                Console.WriteLine("read from int enable 0xffff");
                return 0;
            }

            Console.WriteLine("out of bounds read");
            throw new Exception();
        }
        
        public void Write(int pos, byte val)
        {
            if (pos <= 0x3fff)
            {
                RomBanks[0].write(pos,val);
                return;
            }

            if (pos <= 0x7fff)
            {
                // TODO add support for bank switching
                RomBanks[1].write(pos-0x4000,val);
                return;

            }

            if (pos <= 0x9fff)
            {
                VRam.write(pos - 0x8000, val);
                return;

            }

            if (pos <= 0xbfff)
            {
                // TODO swapping
                ExRam[0].write(pos - 0xa000, val);
                return;

            }

            if (pos <= 0xcfff)
            {
                WRam1.write(pos - 0xc000, val);
                return;

            }

            if (pos <= 0xdfff)
            {
                
                WRam2.write(pos - 0xd000, val);
                return;

            }

            if (pos <= 0xfdff)
            {
                // echo ram
                Console.WriteLine("write to echo ram at " + pos.ToString("x8") + ", this should not happen");
                Write(pos - 0x2000, val);
                return;

            }

            if (pos <= 0xfe9f)
            {
                Oam.write(pos - 0xfe00, val);
                return;

            }

            if (pos <= 0xfeff)
            {
                Console.WriteLine("write to not usable range at " + pos.ToString("x8") + ", double check me");
                return;

                
            }

            if (pos <= 0xff7f)
            {
                switch (pos & 0xff)
                {
                    case 0x01:
                        // TODO implement serial
                        Console.Write((char)val);
                        break;
                }
                //Console.WriteLine("UNIMPLEMENTED IO REGISTER WRITE " + pos.ToString("x8"));
                return;

            }

            if (pos <= 0xfffe)
            {
                HRam.write(pos - 0xff80,val);
                return;

            }

            if (pos == 0xffff)
            {
                Console.WriteLine("write to int enable 0xffff");
                return;

                
            }

            Console.WriteLine("out of bounds write");
            throw new Exception();
        }
    }
}