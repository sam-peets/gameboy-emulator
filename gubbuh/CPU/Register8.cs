namespace gubbuh.CPU
{
    public class Register8
    {
        private byte val;

        public Register8(byte val)
        {
            this.val = val;
        }

        public byte read()
        {
            return val;
        }

        public byte write(byte v)
        {
            val = v;
            return val;
        }
    }
}