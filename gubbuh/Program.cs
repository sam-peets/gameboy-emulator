using System;
using System.IO;
using System.IO.Enumeration;
using System.Net;
using gubbuh.Memory;
using gubbuh.PPU;
using SFML.Graphics;
using SFML.Window;

namespace gubbuh
{
    
    static class Program {

        public static StreamWriter logFile = new StreamWriter("../../../../out.log");
        
        static void OnClose(object sender, EventArgs e) {
            // Close the window when OnClose event is received
            RenderWindow window = (RenderWindow)sender;
            window.Close();
        }

        static void Main()
        {
            
            
            //RomLoader romLoader = new RomLoader("../../../../gb-test-roms/cpu_instrs/individual/09-op r,r.gb");
            //RomLoader romLoader = new RomLoader("../../../../gb-test-roms/cpu_instrs/individual/06-ld r,r.gb");
            RomLoader romLoader = new RomLoader("../../../../gb-test-roms/cpu_instrs/individual/11-op a,(hl).gb");
            
            CPU.CPU cpu = new CPU.CPU(romLoader.mmu);

            
            
            // Create the main window
            RenderWindow app = new RenderWindow(new VideoMode(160, 144), "gubbuh");
            app.Closed += new EventHandler(OnClose);
            
            PPU.PPU ppu = new PPU.PPU(app);
            



            Color windowColor = new Color(0xff, 0xff, 0xff);
            app.SetFramerateLimit(60);
            // Start the game loop
            while (app.IsOpen) {
                // Process events
                app.DispatchEvents();
                for (int i = 0; i < 17556; i++)
                {
                    cpu.Clock();
                }
                
                // Clear screen
                app.Clear(windowColor);
                
                ppu.Render();
                
                // Update the window
                app.Display();
            } //End game loop
        } //End Main()
    } 
}