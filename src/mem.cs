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
    byte[] WRAM1 = new byte[4096];
    byte[] WRAM2 = new byte[4096];
    byte[] HRAM = new byte[0xfffe - 0xff80];
    byte[] VRAM = new byte[0x9fff - 0x8000];
    byte[] IO = new byte[0xff7f - 0xff00];
    public Interrupts interrupts = new Interrupts();
    public Debugger? debug_hook = null;
    public bool had_invalid_access = false;
    
    public STATE (string rom_path) {
        Console.WriteLine("READING ROM: {0}", rom_path);
        ROM = System.IO.File.ReadAllBytes(rom_path);
    }

    byte garbage = 0;

    public void reset() {
        Array.Clear(WRAM1);
        Array.Clear(WRAM2);
        Array.Clear(HRAM);
        Array.Clear(VRAM);
        Array.Clear(IO);
    }

    public ref byte addrNoHook(ushort idx) {
        // if (idx == 0xc800) {
            // debug_hook.memAccessHook
        // }
        if (idx <= 0x7fff)
        {
            // Console.WriteLine("Returning ROM address : {0:X4}",)
            return ref ROM[idx];
        }

        if (idx >= 0xc000 && idx <= 0xcfff)
        {
            if (idx - 0xc000 >= WRAM1.Length) {
                Console.WriteLine("{0:X4} is in bounds for WRAM1 but does not fit", idx);
                return ref garbage;
            }
            return ref WRAM1[idx - 0xc000];
        }

        if (idx >= 0xd000 && idx <= 0xdfff)
        {
            return ref WRAM2[idx - 0xd000];
        }

        if (idx >= 0xff80 && idx <= 0xfffe)
        {
            return ref HRAM[idx - 0xff80];
        }

        if (idx >= 0x8000 && idx <= 0x9fff)
        {
            // Console.WriteLine("access to vram which is not implemented {0:x4}", idx);
            return ref VRAM[idx - 0x8000];
        }

        // -- IO --
        if (idx == 0xffff)
        {
            return ref interrupts.mask;
        }
        else if (idx == 0xff0f)
        {
            return ref interrupts.flags;
        }
        else if (idx == 0xff44)
        {
            // LY
            IO[0xff44 - 0xff00] = 0x90;
            return ref IO[idx - 0xff00];//90;
        }

        if (idx >= 0xff00 && idx <= 0xff7f)
        {
            // Console.WriteLine("access to io memory which is not implemented! {0:x4}", idx);
            return ref IO[idx - 0xff00];
        }

        Console.WriteLine("\x1b[1maccess to {0:x4} not implemented\x1b[0m", idx);
        had_invalid_access = true;
        return ref garbage;
    }

    public ref byte addr(ushort idx) {
        if (debug_hook != null)
            debug_hook.memAccessHook(idx);

        return ref addrNoHook(idx);
    }
}