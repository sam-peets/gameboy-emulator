using System;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using gubbuh.Memory;

namespace gubbuh.CPU
{
    public class CPU
    {
        private FlagRegister AF;
        private Register16 BC;
        private Register16 DE;
        private Register16 HL;
        private Register16 SP;
        private Register16 PC;
        private MMU mmu;

        private bool delayDi = false;
        private bool delayEi = false;
        private int delayCycles;
        private int goodCycles = 0;
        private bool IME = false;
        public CPU(MMU mmu)
        {
            AF = new FlagRegister(0);
            AF.high.write(1);
            AF.low.write(0xb0);
            BC = new Register16(0x0013);
            DE = new Register16(0x00d8);
            HL = new Register16(0x014d);
            SP = new Register16(0xfffe);
            PC = new Register16(0x100);
            this.mmu = mmu;

        }

        public void PrintStatus()
        {
            line = "A: " + AF.high.read().ToString("X2") + " F: " + AF.low.read().ToString("X2") + " B: " +
                   BC.high.read().ToString("X2") + " C: " + BC.low.read().ToString("X2") + " D: " +
                   DE.high.read().ToString("X2") + " E: " + DE.low.read().ToString("X2") + " H: " +
                   HL.high.read().ToString("X2") + " L: " + HL.low.read().ToString("X2") + " SP: " +
                   SP.Read16().ToString("X4") + " PC: 00:" + PC.Read16().ToString("X4") + " (" +
                   mmu.Read(PC.Read16() + 0).ToString("X2") + " " + mmu.Read(PC.Read16() + 1).ToString("X2") + " " + mmu.Read(PC.Read16() + 2).ToString("X2") + " " + mmu.Read(PC.Read16() + 3).ToString("X2") + ")\n";
            Console.Write(line);
        }
        
        private int r;
        private int v;
        private int a;
        private byte b;

        private string line;
        public void Clock()
        {
            
            if (delayCycles > 0)
                delayCycles--;
            if (delayCycles != 0)
                return;

            if (delayDi)
            {
                IME = false;
                delayDi = false;
            }
                
            if (delayEi)
            {
                IME = true;
                delayEi = true;
            }
            
            byte opcode = mmu.Read(PC.Read16());
            goodCycles++;
            
            switch (opcode)
            {
                case 0x00:
                    // nop
                    NOP();
                    break;
                case 0xc3:
                    // jp nn
                    JP(true);
                    break;
                
                case 0xc2:
                    JP(!AF.getZ());
                    break;
                
                case 0xd2:
                    JP(!AF.getC());
                    break;
                
                case 0xca:
                    JP(AF.getZ());
                    break;
                
                case 0xda:
                    JP(AF.getC());
                    break;
                
                case 0x02:
                    mmu.Write(BC.Read16(), AF.ReadHigh());
                    delayCycles += 2;
                    PC.inc();
                    break;
                
                case 0x12:
                    mmu.Write(DE.Read16(), AF.ReadHigh());
                    delayCycles += 2;
                    PC.inc();
                    break;
                
                case 0x0a:
                    AF.WriteHigh(mmu.Read(BC.Read16()));
                    delayCycles += 2;
                    PC.inc();
                    break;
                
                case 0x1a:
                    AF.WriteHigh(mmu.Read(BC.Read16()));
                    delayCycles += 2;
                    PC.inc();
                    break;

                case 0xa8: case 0xa9: case 0xaa: case 0xab: case 0xac: case 0xad: case 0xaf:
                    // xor a, r8
                    r = opcode & 0b00_000_111;
                    XOR(GetR8FromCode(r));
                    break;
                
                case 0x01: case 0x11: case 0x21:
                    // ld r16, u16
                    r = (opcode & 0b00_110_000) >> 4;
                    LD(GetR16FromCode(r));
                    break;
                
                case 0x31:
                    LD(SP);
                    break;
                
                case 0x06: case 0x16: case 0x26: case 0x0e: case 0x1e: case 0x2e: case 0x3e:
                    // ld r8, u8
                    r = (opcode & 0b00_111_000) >> 3;
                    LD(GetR8FromCode(r));
                    break;
                
                case 0x22:
                    // ld (hl+), a
                    LD_HLI_A();
                    break;
                
                case 0x32:
                    // ld (hl-), a
                    LD_HLD_A();
                    break;
                
                case 0x18:
                    // jr i8
                    JR(true);
                    break;

                case 0x04: case 0x14: case 0x24: case 0x0c: case 0x1c: case 0x2c: case 0x3c:
                    r = (opcode & 0b00_111_000) >> 3;
                    INC(GetR8FromCode(r));
                    break;
                
                case 0x05: case 0x15: case 0x25: case 0x0d: case 0x1d: case 0x2d: case 0x3d:
                    r = (opcode & 0b00_111_000) >> 3;
                    DEC(GetR8FromCode(r));
                    break;
                
                case 0x20:
                    // jr nz, i8
                    JR(AF.getZ() == false);
                    break;
                
                case 0xf3:
                    DI();
                    break;
                
                case 0xfb:
                    EI();
                    break;
                
                case 0xe0:
                    LD_IO_A_u8();
                    break;
                
                case 0xf0:
                    LD_A_IO_u8();
                    break;
                
                case 0xee:
                    b = mmu.Read(PC.Read16() + 1);
                    XOR(b);
                    PC.inc();
                    break;

                case 0xae:
                    XOR(mmu.Read(HL.Read16()));
                    break;
                
                case 0xfe:
                    b = mmu.Read(PC.Read16() + 1);
                    CP(b);
                    PC.inc();
                    break;
                    
               case 0xbe:
                    CP(mmu.Read(HL.Read16()));
                    break;
               
               case 0x36:
                   LD_HL_u8();
                   break;
               
               case 0xea:
                   LD_u16_A();
                   break;
               
               case 0xfa:
                   LD_A_u16();
                   break;
              
               case 0x2a:
                   LD_A_HLI();
                   break;
               
               case 0xc6:
                   byte u8 = mmu.Read(PC.Read16() + 1);
                   ADD(u8);
                   PC.inc();
                   delayCycles++;
                   break;
               
               case 0x86:
                   ADD(mmu.Read(HL.Read16()));
                   break;
               
               case 0x3a:
                   LD_A_HLD();
                   break;
               
               case 0xf2:
                   LD_A_IO_C();
                   break;
               
               case 0xe2:
                   LD_IO_A_C();
                   break;
               
               case 0x70: case 0x71: case 0x72: case 0x73: case 0x74: case 0x75: case 0x77:
                   LD_HL_R(GetR8FromCode(opcode&0b_00_000_111));
                   break;
               
               case 0xcd:
                   CALL();
                   break;
               
               case 0xcc:
                   CALL(AF.getZ());
                   break;
               
               case 0xdc:
                   CALL(AF.getC());
                   break;
               
               case 0xc4:
                   CALL(!AF.getZ());
                   break;
               
               case 0xc0:
                   RET(!AF.getZ());
                   break;
               
               case 0xd0:
                   RET(!AF.getC());
                   break;
               
               case 0xc8:
                   RET(AF.getZ());
                   break;
               
               case 0xd8:
                   RET(AF.getC());
                   break;
               
               case 0xc9:
                   RET();
                   break;
               
               case 0xd4:
                   CALL(!AF.getC());
                   break;
               
               case 0x0b: case 0x1b: case 0x2b:
                   r = (opcode & 0b00_11_0000) >> 4;
                   DEC(GetR16FromCode(r));
                   break;
               
               case 0x3b:
                   DEC(SP);
                   break;
               
               case 0x40: case 0x41: case 0x42: case 0x43: case 0x44: case 0x45: case 0x47: case 0x48: case 0x49: case 0x4a: case 0x4b: case 0x4c: case 0x4d: case 0x4f:
               case 0x50: case 0x51: case 0x52: case 0x53: case 0x54: case 0x55: case 0x57: case 0x58: case 0x59: case 0x5a: case 0x5b: case 0x5c: case 0x5d: case 0x5f:
               case 0x60: case 0x61: case 0x62: case 0x63: case 0x64: case 0x65: case 0x67: case 0x68: case 0x69: case 0x6a: case 0x6b: case 0x6c: case 0x6d: case 0x6f:
               case 0x78: case 0x79: case 0x7a: case 0x7b: case 0x7c: case 0x7d: case 0x7f:
                   int r1 = (opcode & 0b_00_111_000) >> 3;
                   int r2 = (opcode & 0b_00_000_111);
                   LD(GetR8FromCode(r1),GetR8FromCode(r2));
                   break;
               
               case 0xb0: case 0xb1: case 0xb2: case 0xb3: case 0xb4: case 0xb5: case 0xb7:
                   r = opcode & 0b00_000_111;
                   OR(GetR8FromCode(r));
                   break;
               
               case 0x2f:
                   CPL();
                   break;
      
                case 0xe6:
                    b = mmu.Read(PC.Read16() + 1);
                    AND(b);
                    PC.inc();
                    return;
                
                case 0xa6:
                    b = mmu.Read(HL.Read16());
                    AND(b);
                    return;
                
                case 0xa0: case 0xa1: case 0xa2: case 0xa3: case 0xa4: case 0xa5: case 0xa7:
                    r = opcode & 0b_00_000_111;
                    AND(GetR8FromCode(r));
                    return;
                
                case 0xc7: case 0xd7: case 0xe7: case 0xf7: case 0xcf: case 0xdf: case 0xef: case 0xff:
                    byte t = (byte) ((opcode & 0b00_111_000) >> 3);
                    RST(t);
                    break;
                
                case 0x80: case 0x81: case 0x82: case 0x83: case 0x84: case 0x85: case 0x87:
                    r = opcode & 0b00_000_111;
                    ADD(GetR8FromCode(r));
                    break;
                
                case 0xe9:
                    JP_HL();
                    break;

                case 0xb6:
                    OR(mmu.Read(HL.Read16()));
                    break;
                
                case 0xf6:
                    OR(mmu.Read(PC.Read16()+1));
                    PC.inc();
                    break;
                    
                
                case 0x09: case 0x19: case 0x29: case 0x39:
                    r = (opcode & 0b00_110_000) >> 4;
                    switch (r)
                    {
                        case 0b00:
                            ADD(BC);
                            break;
                        case 0b01:
                            ADD(DE);
                            break;
                        case 0b10:
                            ADD(HL);
                            break;
                        case 0b11:
                            ADD(SP);
                            break;
                    }
                    break;

                
                case 0x46: case 0x56: case 0x66: case 0x4e: case 0x5e: case 0x6e: case 0x7e:
                    r = (opcode & 0b_00_111_000) >> 3;
                    b = mmu.Read(HL.Read16());
                    LD(GetR8FromCode(r), b);
                    return;

                case 0xc1: case 0xd1: case 0xe1: case 0xf1:
                    r = (opcode & 0b_00_110_000) >> 4;
                    POP(GetR16FromCode(r));
                    break;

                case 0x03: case 0x13: case 0x23: case 0x33:
                    r = (opcode & 0b_00_110_000) >> 4;
                    switch (r)
                    {
                        case 0b00:
                            INC(BC);
                            break;
                        case 0b01:
                            INC(DE);
                            break;
                        case 0b10:
                            INC(HL);
                            break;
                        case 0b11:
                            INC(SP);
                            break;
                    }

                    break;
                
                case 0xc5: case 0xd5: case 0xe5: case 0xf5:
                    r = (opcode & 0b_00_110_000) >> 4;
                    PUSH(GetR16FromCode(r));
                    break;

                case 0xb8: case 0xb9: case 0xba: case 0xbb: case 0xbc: case 0xbd: case 0xbf:
                    CP(GetR8FromCode(opcode & 0b_00_000_111));
                    break;
                    
                case 0x07:
                    RLC(AF.high);
                    PC.dec();
                    AF.setZ(false);
                    delayCycles--;
                    break;
                
                case 0x08:
                    ushort a = (ushort)(mmu.Read(PC.Read16() + 1) | (mmu.Read(PC.Read16() + 2) << 8));
                    mmu.Write(a, SP.ReadLow());
                    mmu.Write(a + 1, SP.ReadHigh());
                    PC.inc(3);
                    delayCycles += 5;
                    break;
                
                case 0xf9:
                    SP.Write16(HL.Read16());
                    PC.inc();
                    delayCycles += 2;
                    break;
                
                case 0x37:
                    AF.setC(true);
                    AF.setN(false);
                    AF.setH(false);
                    delayCycles++;
                    PC.inc();
                    break;
                
                case 0x3f:
                    AF.setC(!AF.getC());
                    AF.setN(false);
                    AF.setH(false);
                    delayCycles++;
                    PC.inc();
                    break;
                
                case 0x28:
                    JR(AF.getZ());
                    break;
                
                case 0x30:
                    JR(!AF.getC());
                    break;
                
                case 0x38:
                    JR(AF.getC());
                    break;
                
                case 0x35:
                    DEC();
                    break;
                
                case 0x34:
                    INC();
                    break;
                
                case 0xd6:
                    SUB(mmu.Read(PC.Read16()+1));
                    PC.inc();
                    break;
                
                case 0x90: case 0x91: case 0x92: case 0x93: case 0x94: case 0x95: case 0x97:
                    SUB(GetR8FromCode(opcode&0b00_000_111));
                    break;
                
                case 0x96:
                    SUB(mmu.Read(HL.Read16()));
                    break;
                
                case 0x1f:
                    RR(AF.high);
                    AF.setZ(false);
                    delayCycles--;
                    PC.dec();
                    break;
                
                case 0x88: case 0x89: case 0x8a: case 0x8b: case 0x8c: case 0x8d: case 0x8f:
                    ADC(GetR8FromCode(opcode&0b00_000_111));
                    break;
                
                case 0xce:
                    ADC(mmu.Read(PC.Read16() + 1));
                    break;
                
                case 0x8e:
                    ADC(mmu.Read(HL.Read16()));
                    PC.dec();
                    break;

                case 0xcb:
                    opcode = mmu.Read(PC.Read16() + 1);
                    switch (opcode)
                    {
                        case 0x37:
                            r = opcode & 0b00_000_111;
                            SWAP(GetR8FromCode(r));
                            break;
                        
                        case 0x80: case 0x81: case 0x82: case 0x83: case 0x84: case 0x85: case 0x87: case 0x88: case 0x89: case 0x8a: case 0x8b: case 0x8c: case 0x8d: case 0x8f:
                        case 0x90: case 0x91: case 0x92: case 0x93: case 0x94: case 0x95: case 0x97: case 0x98: case 0x99: case 0x9a: case 0x9b: case 0x9c: case 0x9d: case 0x9f:
                        case 0xa0: case 0xa1: case 0xa2: case 0xa3: case 0xa4: case 0xa5: case 0xa7: case 0xa8: case 0xa9: case 0xaa: case 0xab: case 0xac: case 0xad: case 0xaf:
                        case 0xb0: case 0xb1: case 0xb2: case 0xb3: case 0xb4: case 0xb5: case 0xb7: case 0xb8: case 0xb9: case 0xba: case 0xbb: case 0xbc: case 0xbd: case 0xbf:
                            r = opcode & 0b_00_000_111;
                            b = (byte) ((opcode & 0b_00_111_000) >> 3);
                            CB_RES(b, GetR8FromCode(r));
                            break;
                        
                        case 0x38: case 0x39: case 0x3a: case 0x3b: case 0x3c: case 0x3d: case 0x3f:
                            SRL(GetR8FromCode(opcode & 0b_00_000_111));
                            break;
                        
                        case 0x3e:
                            SRL(HL.Read16());
                            break;
                        
                        case 0x18: case 0x19: case 0x1a: case 0x1b: case 0x1c: case 0x1d: case 0x1f:
                            RR(GetR8FromCode(opcode & 0b_00_000_111));
                            break;
                        
                        case 0x1e:
                            RR(HL.Read16());
                            break;
                        
                        case 0x00: case 0x01: case 0x02: case 0x03: case 0x04: case 0x05: case 0x07:
                            RLC(GetR8FromCode(opcode&0b00_000_111));
                            break;
                        
                        case 0x06:
                            RLC(HL.Read16());
                            break;


                        default:
                            Console.WriteLine("Unimplemented CB opcode " + opcode.ToString("x2") + " at " +
                                              PC.Read16().ToString("x4"));
                            Console.WriteLine("ran " + goodCycles + " cycles before crash");
                            Environment.Exit(0);
                            break;
                    }

                    break;
                
               
               default:
                    Console.WriteLine("Unimplemented opcode " + opcode.ToString("x2") + " at " +
                                      PC.Read16().ToString("x4"));
                    Console.WriteLine("ran " + goodCycles + " cycles before crash");
                    System.Environment.Exit(0);
                    break;
            }
        }

        public void NOP()
        {
            // nop
            PC.inc();
            delayCycles += 1;
        }

        public void ADC(Register8 r8)
        {
            byte c = (byte)(AF.getC() ? 1 : 0);
            AF.setH((((r8.read() & 0xf) + (AF.ReadHigh() & 0xf) + c) & 0x10) == 0x10);
            AF.setC((r8.read() + AF.ReadHigh() + c) > 0xff);
            AF.setN(false);
            AF.WriteHigh((byte) (AF.ReadHigh() + r8.read() + c));
            AF.setZ(AF.ReadHigh() == 0);
            PC.inc();
            delayCycles += 1;
        }

        public void ADC(byte u8)
        {
            byte c = (byte)(AF.getC() ? 1 : 0);
            AF.setH((((u8 & 0xf) + (AF.ReadHigh() & 0xf) + c) & 0x10) == 0x10);
            AF.setC((u8 + AF.ReadHigh() + c) > 0xff);
            AF.setN(false);
            AF.WriteHigh((byte) (AF.ReadHigh() + u8 + c));
            AF.setZ(AF.ReadHigh() == 0);
            PC.inc(2);
            delayCycles += 2;
        }
        
        public void ADD(Register16 r16)
        {
            AF.setH((((HL.Read16() & 0x00FF) + (r16.Read16() & 0x00FF)) & 0x0100) == 0x0100);
            AF.setC((r16.Read16() + HL.Read16()) > 0xffff);
            AF.setN(false);
            HL.Write16(HL.Read16() + r16.Read16());
            AF.setZ(HL.Read16() == 0);
            PC.inc();
            delayCycles += 2;
        }

        public void LD_HL_R(Register8 r8)
        {
            mmu.Write(HL.Read16(), r8.read());
            delayCycles += 2;
            PC.inc();
        }
        
        public void POP(Register16 r16)
        {
            r16.WriteLow(mmu.Read(SP.Read16()));
            r16.WriteHigh(mmu.Read(SP.Read16() + 1));
            SP.inc(2);
            delayCycles += 3;
            PC.inc();

        }

        
        
        public void CB_RES(byte pos, Register8 r8)
        {
            int mask = 1 << pos;
            r8.write((byte) (r8.read() & ~mask));
            delayCycles += 2;
            PC.inc(2);
        }
        
        public void PUSH(Register16 r16)
        {
            mmu.Write(SP.Read16() - 1, r16.ReadHigh());
            mmu.Write(SP.Read16() - 2, r16.ReadLow());
            SP.dec(2);
            delayCycles += 4;
            PC.inc();
        }
        
        public void RST(byte t)
        {
            PC.inc(1);
            mmu.Write(SP.Read16() - 1, PC.ReadHigh());
            mmu.Write(SP.Read16() - 2, PC.ReadLow());
            SP.dec(2);
            PC.Write16(t*0x8);
            delayCycles += 4;
        }

        public void ADD(Register8 r8)
        {
            AF.setH((((r8.read() & 0xf) + (AF.ReadHigh() & 0xf)) & 0x10) == 0x10);
            AF.setC((r8.read() + AF.ReadHigh()) > 0xff);
            AF.setN(false);
            AF.WriteHigh((byte) (AF.ReadHigh() + r8.read()));
            AF.setZ(AF.ReadHigh() == 0);
            PC.inc();
            delayCycles += 1;
        }

        public void ADD(byte u8)
        {
            AF.setH((((u8 & 0xf) + (AF.ReadHigh() & 0xf)) & 0x10) == 0x10);
            AF.setC((u8 + AF.ReadHigh()) > 0xff);
            AF.setN(false);
            AF.WriteHigh((byte) (AF.ReadHigh() + u8));
            AF.setZ(AF.ReadHigh() == 0);
            PC.inc();
            delayCycles += 1;
        }

        public void SUB(byte u8)
        {
            AF.setN(true);
            AF.setH((((AF.ReadHigh() & 0xf) - (u8 & 0xf)) & 0x10) == 0x10);
            AF.setC((AF.ReadHigh() - u8) < 0);
            AF.setZ(AF.ReadHigh() == 0);
            AF.WriteHigh((byte) (AF.ReadHigh() - u8));
            PC.inc(1);
            delayCycles += 2;
        }
        
        

        public void SRL(Register8 r8)
        {
            AF.setN(false);
            AF.setH(false);
            AF.setC((r8.read()&1) == 1);
            r8.write((byte)(r8.read() >> 1));
            AF.setZ(r8.read() == 0);
            delayCycles += 2;
            PC.inc(2);
        }

        public void SRL(ushort a)
        {
            AF.setN(false);
            AF.setH(false);
            AF.setC((mmu.Read(a)&1) == 1);
            mmu.Write(a, (byte)(mmu.Read(a) >> 1));
            AF.setZ(mmu.Read(a) == 0);
            delayCycles += 4;
            PC.inc(2);

        }

        

        public void RLC(Register8 r8)
        {
            AF.setN(false);
            AF.setH(false);
            byte t = (byte)((r8.read() & 0b10000000) >> 7);
            AF.setC(t == 1);
            r8.write((byte)((r8.read() << 1) | t));
            AF.setZ(r8.read() == 0);
            delayCycles += 2;
            PC.inc(2);

        }
        
        public void RLC(ushort a)
        {
            AF.setN(false);
            AF.setH(false);
            byte t = (byte)((mmu.Read(a) & 0b10000000) >> 7);
            AF.setC(t == 1);
            mmu.Write(a,(byte)((mmu.Read(a) << 1) | t));
            AF.setZ(mmu.Read(a) == 0);
            delayCycles += 4;
            PC.inc(2);
        }
        
        public void RR(Register8 r8)
        {
            AF.setN(false);
            AF.setH(false);
            byte m0 = (byte)(r8.read() & 1);
            byte c = (byte)(AF.getC() ? 1 : 0);
            r8.write((byte)((r8.read() >> 1) | (c << 7)));
            AF.setZ(r8.read() == 0);
            delayCycles += 2;
            AF.setC(m0 == 1);
            PC.inc(2);
        }

        public void RR(ushort a)
        {
            AF.setN(false);
            AF.setH(false);
            byte m0 = (byte)(mmu.Read(a) & 1);
            byte c = (byte)(AF.getC() ? 1 : 0);
            
            mmu.Write(a, (byte)((mmu.Read(a) >> 1)  | (c << 7)));
            AF.setZ(mmu.Read(a) == 0);
            delayCycles += 4;
            AF.setC(m0 == 1);
            PC.inc(2);
        }
        
        public void SUB(Register8 r8)
        {
            AF.setN(true);
            AF.setH((((AF.ReadHigh() & 0xf) - (r8.read() & 0xf)) & 0x10) == 0x10);
            AF.setC((AF.ReadHigh() - r8.read()) < 0);
            AF.setZ(AF.ReadHigh() == 0);
            AF.WriteHigh((byte) (AF.ReadHigh() - r8.read()));
            PC.inc();
            delayCycles += 1;
        }

        

        public void LD(Register8 r8, byte b)
        {
            r8.write(b);
            PC.inc();
            delayCycles += 2;
            
        }
        
        public void JP(bool cond)
        {
            // jp nn
            if (cond)
            {
                a = (ushort) (mmu.Read(PC.Read16() + 1) | (mmu.Read(PC.Read16() + 2) << 8));
                PC.Write16(a);
                delayCycles += 4;
            }
            else
            {
                PC.inc(3);
                delayCycles += 3;
            }
            
        }

        public void JR(bool cond)
        {
            if (cond)
            {
                a = (sbyte) mmu.Read(PC.Read16() + 1);
                PC.inc(2);
                PC.Write16(PC.Read16() + a);
                delayCycles += 3;
                return;
            }

            PC.inc(2);
            delayCycles += 2;

        }

        public void JP_HL()
        {
            PC.Write16(HL.Read16());
            delayCycles += 1;
        }

        public void SWAP(Register8 r)
        {
            byte lo = (byte) ((r.read() & 0x0f) << 4);
            byte hi = (byte) ((r.read() & 0xf0) >> 4);
            r.write((byte) (lo | hi));
            AF.setZ(r.read() == 0);
            AF.setH(false);
            AF.setC(false);
            AF.setN(false);
            PC.inc(2);
            delayCycles += 2;
        }

        
        
        public void AND(Register8 r8)
        {
            AF.WriteHigh((byte) (AF.ReadHigh() & r8.read()));
            AF.setZ(AF.ReadHigh() == 0);
            AF.setH(false);
            AF.setC(true);
            AF.setN(false);
            PC.inc();
            delayCycles += 1;
        }
        
        public void XOR(Register8 r)
        {
            // xor a, r8
            AF.high.write((byte) (AF.high.read() ^ r.read()));
            AF.setZ(AF.ReadHigh() == 0);
            AF.setN(false);
            AF.setH(false);
            AF.setC(false);
            delayCycles += 1;
            PC.inc();
        }
        

        public void LD_u16_A()
        {
            
            a = mmu.Read(PC.Read16() + 1) | (mmu.Read(PC.Read16() + 2) << 0x8);
            mmu.Write(a, AF.ReadHigh());
            delayCycles += 4;
            PC.inc(3);
           
        }

        public void RET()
        {
            PC.WriteLow(mmu.Read(SP.Read16()));
            PC.WriteHigh(mmu.Read(SP.Read16()+1));
            SP.inc(2);
            delayCycles += 4;
        }

        public void RET(bool cond)
        {
            if (cond)
            {
                RET();
                delayCycles+=1;
            }
            else
            {
                PC.inc();
                delayCycles += 2;
            }
        }
        
        public void DEC(Register16 r)
        {
            r.dec();
            PC.inc();
            delayCycles += 2;
        }

        public void CALL()
        {
            a = mmu.Read(PC.Read16() + 1) | (mmu.Read(PC.Read16() + 2) << 0x8);
            PC.inc(3);
            mmu.Write(SP.Read16()-1, PC.ReadHigh());
            mmu.Write(SP.Read16()-2, PC.ReadLow());
            PC.Write16(a);
            SP.dec(2);
            delayCycles += 6;
        }

        public void CPL()
        {
            AF.setN(true);
            AF.setH(true);
            AF.WriteHigh((byte) ((~AF.ReadHigh()) & 0xff));
            PC.inc(1);
            delayCycles += 1;
        }

        public void LD(Register8 r1, Register8 r2)
        {
            r1.write(r2.read());
            delayCycles += 1;
            PC.inc();
        }

        public void OR(Register8 r)
        {
            AF.WriteHigh((byte) (AF.ReadHigh() | r.read()));
            AF.setZ(AF.ReadHigh() == 0); 
            AF.setH(false);
            AF.setC(false);
            AF.setN(false);
            PC.inc();
            delayCycles += 1;
        }

        public void OR(byte u8)
        {
            AF.WriteHigh((byte) (AF.ReadHigh() | u8));
            AF.setZ(AF.ReadHigh() == 0); 
            AF.setH(false);
            AF.setC(false);
            AF.setN(false);
            PC.inc();
            delayCycles += 2;
        }

        public void AND(byte b)
        {
            AF.WriteHigh((byte) (AF.ReadHigh() & b));
            AF.setZ(AF.ReadHigh() == 0);
            AF.setH(false);
            AF.setC(true);
            AF.setN(false);
            PC.inc();
            delayCycles += 2;
        }
        
        public void CALL(bool cond)
        {
            if (cond)
            {
                CALL();
            }
            else
            {
                delayCycles += 3;
                PC.inc(3);
            }
        }
        
        public void LD_A_u16()
        {
            a = mmu.Read(PC.Read16() + 1) | (mmu.Read(PC.Read16() + 2) << 8);
            
            mmu.Write(a, AF.ReadHigh());
            delayCycles += 4;
            PC.inc(3);
        }   

        public void XOR(byte b)
        {
            AF.high.write((byte) (AF.high.read() ^ b));
            AF.setZ(AF.ReadHigh() == 0);
            AF.setN(false);
            AF.setH(false);
            AF.setC(false);
            delayCycles += 2;
            PC.inc(1);
        }

        public void CP(byte b)
        {
            AF.setH((((AF.ReadHigh() & 0xf) - (b & 0xf)) & 0x10) == 0x10);
            AF.setZ(AF.ReadHigh() == b);
            AF.setC(b > AF.ReadHigh());
            AF.setN(true);
            delayCycles += 2;
            PC.inc();
        }
        
        public void CP(Register8 r8)
        {
            AF.setH((((AF.ReadHigh() & 0xf) - (r8.read() & 0xf)) & 0x10) == 0x10);
            AF.setZ(AF.ReadHigh() == r8.read());
            AF.setC(r8.read() > AF.ReadHigh());
            AF.setN(true);
            delayCycles += 1;
            PC.inc();
        }

        public void LD_HLI_A()
        {
            // ld (hl+), a
            mmu.Write(HL.Read16(), AF.high.read());
            HL.inc();
            delayCycles += 2;
            PC.inc(1);
        }

       public void LD_HLD_A()
       {
           // ld (hl-), a
           mmu.Write(HL.Read16(), AF.high.read());
           HL.dec();
           delayCycles += 2; 
           PC.inc(1);
       }

       public void LD_A_HLI()
       {
           AF.WriteHigh(mmu.Read(HL.Read16()));
           HL.inc();
           delayCycles += 2;
           PC.inc();
       }
        
       public void LD_A_HLD()
       {
           AF.WriteHigh(mmu.Read(HL.Read16()));
           HL.dec();
           delayCycles += 2;
           PC.inc();
       }
       public void LD_HL_u8()
       {
           b = mmu.Read(PC.Read16() + 1);
           mmu.Write(HL.Read16(), b);
           PC.inc(2);
           delayCycles += 3;
       }

       public void INC(Register8 r)
       {
           AF.setH((((r.read() & 0xf) + (1 & 0xf)) & 0x10) == 0x10);
           r.write((byte) ((r.read() + 1) & 0xff));
           AF.setZ(r.read() == 0);
           AF.setN(false);
           PC.inc();
           delayCycles++; 
       }

       public void INC(Register16 r16)
       {
           r16.inc();
           delayCycles += 2;
           PC.inc();
       }
       
       public void DEC(Register8 r)
       {
           AF.setH((((r.read() & 0xf) - (1 & 0xf)) & 0x10) == 0x10);
           r.write((byte) (r.read() - 1));
           AF.setZ(r.read() == 0);
           AF.setN(true);
           PC.inc();
           delayCycles++;

       }

       public void DEC()
       {
           AF.setH((((mmu.Read(HL.Read16()) & 0xf) - (1 & 0xf)) & 0x10) == 0x10);
           mmu.Write(HL.Read16(), (byte) (mmu.Read(HL.Read16())-1));
           AF.setZ(mmu.Read(HL.Read16()) == 0);
           AF.setN(true);
           PC.inc();
           delayCycles += 3;
       }
       
       public void INC()
       {
           AF.setH((((mmu.Read(HL.Read16()) & 0xf) + (1 & 0xf)) & 0x10) == 0x10);
           mmu.Write(HL.Read16(), (byte) (mmu.Read(HL.Read16())+1));
           AF.setZ(mmu.Read(HL.Read16()) == 0);
           AF.setN(false);
           PC.inc();
           delayCycles += 3;
       }
        
        public void LD(Register16 r)
        {
            // ld r16, u16
            v = (mmu.Read(PC.Read16() + 1) | (mmu.Read(PC.Read16() + 2) << 8));
            r.Write16(v);
            delayCycles += 3; 
            PC.inc(3);
        }

        public void LD_IO_A_C()
        {
            mmu.Write(0xff00+BC.ReadLow(), AF.ReadHigh());
            PC.inc();
            delayCycles += 2;
        }
        
        public void LD_A_IO_C()
        {
            AF.WriteHigh(mmu.Read(0xff00+BC.ReadLow()));
            PC.inc();
            delayCycles += 2;
        }
        public void LD(Register8 r)
        {
            // ld r8, u8
            v = (mmu.Read(PC.Read16() + 1));
            r.write((byte) v);
            delayCycles += 2;
            PC.inc(2);
        }

        public void DI()
        {
            delayDi = true;
            delayCycles += 1;
            Console.WriteLine("DI");
            PC.inc();
        }

        public void EI()
        {
            delayEi = true;
            delayCycles += 1;
            PC.inc();
            Console.WriteLine("EI");
        }

        public void LD_IO_A_u8()
        {
            v = mmu.Read(PC.Read16() + 1);
            mmu.Write(0xff00 + v, AF.ReadHigh());
            delayCycles += 3;
            PC.inc(2);
        }
        
        public void LD_A_IO_u8()
        {
             v = mmu.Read(PC.Read16() + 1);
             AF.WriteHigh(mmu.Read(0xff00 + v));
             delayCycles += 3;
             PC.inc(2);
        }       
        
        public Register8 GetR8FromCode(int code)
        {
            switch (code)
            {
                case 0b111:
                    return AF.high;
                case 0b000:
                    return BC.high;
                case 0b001:
                    return BC.low;
                case 0b010:
                    return DE.high;
                case 0b011:
                    return DE.low;
                case 0b100:
                    return HL.high;
                case 0b101:
                    return HL.low;

            }

            throw new ArgumentOutOfRangeException();
        }

        public Register16 GetR16FromCode(int code)
        {
            switch (code) {
                case 0b00:
                    return BC;
                case 0b01:
                    return DE;
                case 0b10:
                    return HL;
                case 0b11:
                    return AF;
            }
            
            throw new ArgumentOutOfRangeException();
        }

        
    }
}