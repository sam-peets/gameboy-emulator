namespace gubbuh.CPU
{
    public class Register16
    {
        public Register8 high;
        public Register8 low;

        public Register16(ushort value)
        {
            high = new Register8(0);
            low = new Register8(0);

            Write16(value);

        }

        public ushort inc()
        {
            Write16(Read16() + 1);
            return Read16();
        }

        public ushort inc(int n)
        {
            Write16(Read16() + n);
            return Read16();
        }

        public void WriteHigh(byte val)
        {
            high.write(val);
        }

        public void WriteLow(byte val)
        {
            low.write(val);
        }

        public byte ReadHigh()
        {
            return high.read();
        }

        public byte ReadLow()
        {
            return low.read();
        }

        public void Write16(int val)
        {
            high.write((byte) ((val & 0xff00) >> 8));
            low.write((byte) (val & 0x00ff));
        }

        public ushort Read16()
        {
            return (ushort) ((high.read() << 8) | low.read());

        }

        public ushort dec()
        {
            Write16(Read16() - 1);
            return Read16();
        }
        
        public ushort dec(int n)
        {
            Write16(Read16() - n);
            return Read16();
        }
    }
}
