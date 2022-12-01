namespace gubbuh.Memory
{
    public class Block
    {
        private byte[] data;

        public Block(int len)
        {
            data = new byte[len];
        }

        public byte read(int pos)
        {
            return data[pos];
        }

        public byte write(int pos, byte b)
        {
            data[pos] = b;
            return b;
        }
    }
}