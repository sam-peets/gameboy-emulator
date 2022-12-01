using SFML.Graphics;

namespace gubbuh.PPU
{
    public class PPU
    {
        public Texture PPUScreen;
        private byte[] byteScreen;
        private RenderWindow app;

        public PPU(RenderWindow renderWindow)
        {
            PPUScreen = new Texture(160,144);
            byteScreen = new byte[160 * 144 * 4];
            this.app = renderWindow;
        }

        public void Render()
        {
            PPUScreen.Update(byteScreen);
            Sprite ssprite = new Sprite(PPUScreen);
            app.Draw(ssprite);
        }


        private int dot = 0;
        private int mode = 2;
        public int LY = 0;
        
        
        public void Clock()
        {
            switch (mode)
            {
                case 0:
                    // hblank
                    dot++;
                    if (dot == 456)
                    {
                        dot = 0;
                        mode = 2;
                        LY++;
                    }
                    break;

                case 1:
                    // vblank
                    dot++;
                    if (dot == 456)
                    {
                        if (LY == 153)
                        {
                            dot = 0;
                            mode = 2;
                            LY = 0;
                        }
                        else
                        {
                            dot = 0;
                            LY++;
                        }
                    }
                    break;
                
                case 2:
                    break;
                
                
            }
        }

        public void WritePixel(Colour c, int x, int y)
        {
            byte r = (byte)(((uint)c & 0xff000000) >> 24);
            byte g = (byte)(((uint)c & 0x00ff0000) >> 16);
            byte b = (byte)(((uint)c & 0x0000ff00) >> 8);
            byte a = (byte)(((uint)c & 0x000000ff));

            byteScreen[(y * 160 * 4) + (x * 4)] = r;
            byteScreen[(y * 160 * 4) + (x * 4) + 1] = g;
            byteScreen[(y * 160 * 4) + (x * 4) + 2] = b;
            byteScreen[(y * 160 * 4) + (x * 4) + 3] = a;
            
        }
    }
}