namespace gubbuh.CPU
{
    public class FlagRegister : Register16
    {
        public FlagRegister(ushort value) : base(value)
        {
            
        }

       
        
        public bool getZ()
        {
            return (low.read() & 0b1000_0000) == 0b1000_0000;
        }
        
        public bool getN()
        {
            return (low.read() & 0b0100_0000) == 0b0100_0000;
        }
        
        public bool getH()
        {
            return (low.read() & 0b0010_0000) == 0b0010_0000;
        }
        
        public bool getC()
        {
            return (low.read() & 0b0001_0000) == 0b0001_0000;
        }

        public void setZ(bool b)
        {
            if (b)
            {
                low.write((byte) (low.read() | (1 << 7)));
            }
            else
            {
                low.write((byte) (low.read() & ~(1 << 7)));
            }
        }
        
       public void setN(bool b)
               {
                   if (b)
                   {
                       low.write((byte) (low.read() | (1 << 6)));
                   }
                   else
                   {
                       low.write((byte) (low.read() & ~(1 << 6)));
                   }
               } 
       public void setH(bool b)
               {
                   if (b)
                   {
                       low.write((byte) (low.read() | (1 << 5)));
                   }
                   else
                   {
                       low.write((byte) (low.read() & ~(1 << 5)));
                   }
               }
       
       public void setC(bool b)
               {
                   if (b)
                   {
                       low.write((byte) (low.read() | (1 << 4)));
                   }
                   else
                   {
                       low.write((byte) (low.read() & ~(1 << 4)));
                   }
               }
        
        
         
    }
}