class Interrupts {
    public bool enabled = false;
    public byte mask = 0;
    public byte flags = 0;
    public bool vblank() { return enabled ? (mask & 1 << 0) == 1 : false; }
    public bool lcd_stat() { return enabled ? (mask & 1 << 1 ) == 1 : false; }
    public bool timer() { return enabled ? (mask & 1 << 2 ) == 1 : false; }
    public bool serial() { return enabled ? (mask & 1 << 3) == 1 : false;  }
    public bool joypad() { return enabled ? (mask & 1 << 4) == 1 : false; }
}

class STATE {
    byte[] ROM;
    byte[] WRAM = new byte[8000 + 100];
    byte[] HRAM = new byte[0xfffe - 0xff80 + 100];
    byte[] VRAM = new byte[0x9fff - 0x8000 + 100];
    byte[] IO = new byte[0xff7f - 0xff00 + 100];
    public Interrupts interrupts = new Interrupts();
    public bool had_invalid_access = false;
    
    public STATE (string rom_path) {
        Console.WriteLine("READING ROM: {0}", rom_path);
        ROM = System.IO.File.ReadAllBytes(rom_path);
        // addr(0xff00 + 44) = 0x90;
    }

    byte garbage = 0;

    public ref byte addr(ushort idx) {
        if (idx <= 0x7fff) {
            return ref ROM[idx];
        } 

        if (idx >= 0xc000 && idx <= 0xcfff) {
            return ref WRAM[idx - 0xc000];
        } 

        if (idx >= 0xd000 && idx <= 0xdfff) {
            return ref WRAM[idx - 0xd000];
        }

        if (idx >= 0xff80 && idx <= 0xfffe) {
            return ref HRAM[idx - 0xff80];
        }

        if (idx >= 0x8000 && idx <= 0x9fff) {
            // Console.WriteLine("\x1b[1maccess to vram - graphics not implemented\x1b[0m");
            return ref VRAM[idx - 0x8000];
        }

        // -- IO --

        if (idx == 0xffff) {
            return ref interrupts.mask;
        } else if (idx == 0xff0f) {
            return ref interrupts.flags;
        } else if (idx == 0xff44) {
            // LY
            IO[0xff44 - 0xff00] = 0x90;
            return ref IO[idx - 0xff00];//90;
        }

        if (idx >= 0xff00 && idx <= 0xff7f)
        {
            // Console.WriteLine("\x1b[1mwrite to io memory - no io is implemented\x1b[0m");
            // had_invalid_access = true;
            return ref IO[idx - 0xff00];
        }

        Console.WriteLine("\x1b[1maccess to {0:x4} not implemented\x1b[0m", idx);
        had_invalid_access = true;
        return ref garbage;
    }
}