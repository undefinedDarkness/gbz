struct Flags
{
    public byte byte_value = 0x00;

    public bool zero
    {
        get => (byte_value & 1 << 7) != 0;
        set
        {
            if (value)
                byte_value |= (byte)(1 << 7);
            else
            {
                byte mod = 1 << 7;
                byte_value &= (byte)~(mod);
            }
        }
    }

    public bool negative
    {
        get => (byte_value & 1 << 6) != 0;
        set
        {
            if (value)
                byte_value |= (byte)(1 << 6);
            else
            {
                byte mod = 1 << 6;
                byte_value &= (byte)~(mod);
            }
        }
    }

    public bool half_carry
    {
        get => (byte_value & 1 << 5) != 0;
        set
        {
            if (value)
                byte_value |= (byte)(1 << 5);
            else
            {
                byte mod = 1 << 5;
                byte_value &= (byte)~(mod);
            }
        }
    }

    public bool carry
    {
        get => (byte_value & 1 << 4) != 0;
        set
        {
            if (value)
                byte_value |= (byte)(1 << 4);
            else
            {
                byte mod = 1 << 4;
                byte_value &= (byte)~(mod);
            }
        }
    }

    public Flags(bool _z, bool _n, bool _hc, bool _c)
    {
        zero = _z; negative = _n; half_carry = _hc; carry = _c;
    }
}

class CPU
{
    Flags F = new Flags(true, false, false, false); // zero negative half-carry carry
    byte A = 0x11;
    byte garbage = 0xff;
    ushort[] registerCache = new ushort[8];
    void cache_store()
    {
        registerCache[0] = AF;
        registerCache[1] = BC;
        registerCache[2] = DE;
        registerCache[3] = HL;
        registerCache[4] = SP;
        registerCache[5] = PC;
    }

    public void rewind()
    {
        AF = registerCache[0];
        BC = registerCache[1];
        DE = registerCache[2];
        HL = registerCache[3];
        SP = registerCache[4];
        PC = registerCache[5];
    }

    public ushort AF
    {
        get => (ushort)(A << 8 | F.byte_value);
        private set
        {
            F.byte_value = (byte)(value & 0x00ff);
            A = (byte)(value >> 8);
        }
    }
    byte B = 0x00;
    byte C = 0x00;
    public ushort BC
    {
        get => (ushort)(B << 8 | C);
        private set
        {
            C = (byte)(value & 0x00ff);
            B = (byte)(value >> 8);
        }
    }
    byte D = 0xff;
    byte E = 0x56;
    public ushort DE
    {
        get => (ushort)(D << 8 | E);
        private set
        {
            E = (byte)(value & 0x00ff);
            D = (byte)(value >> 8);
        }
    }
    byte H = 0x00;
    byte L = 0x0D;
    public ushort HL
    {
        get => (ushort)(H << 8 | L);
        private set
        {
            L = (byte)(value & 0x00ff);
            H = (byte)(value >> 8);
        }
    }

    // ushort _PC = 0x0100;
    public UInt16 PC = 0x0100;

    public UInt16 SP { get; private set; } = 0xFFFE;
    STATE S;
    public CPU(STATE _s)
    {
        S = _s;
    }

    public void reset()
    {
        PC = 0x0100;
        SP = 0xFFFE;
        A = 0x11;
        B = 0x00;
        C = 0x00;
        D = 0xff;
        E = 0x56;
        H = 0x00;
        L = 0x0D;
        no_modify_pc = true;
    }
    public ushort getShortAtPC()
    {
        byte a = S.addr((ushort)(PC + 1));
        byte b = S.addr((ushort)(PC + 2));
        return (ushort)(b << 8 | a);
    }

    public byte getByteAtPC()
    {
        return S.addr((ushort)(PC + 1));
    }

    void writeShortAtSP(ushort v)
    {
        SP -= 2;
        S.addr(SP) = (byte)(v & 0x00ff);
        S.addr((ushort)(SP + 1)) = (byte)(v >> 8);
    }

    ushort getShortAtSP()
    {
        byte a = S.addr(SP);
        byte b = S.addr((ushort)(SP + 1));
        SP += 2;
        return (ushort)(b << 8 | a);
    }

    byte getRegByIdx(int idx)
    {
        return (idx % 8) switch
        {
            0 => B,
            1 => C,
            2 => D,
            3 => E,
            4 => H,
            5 => L,
            6 => (byte)HL,
            7 => A,
            _ => garbage
        };
    }

    void ld_rr(int ai, int bi)
    {
        // ai == 0 bi == 7
        if (ai == 0)
        {
            if (bi <= 7)
            {
                B = getRegByIdx(bi);
            }
            else
            {
                C = getRegByIdx(bi);
            }
        }
        else if (ai == 1)
        {
            if (bi <= 7)
            {
                D = getRegByIdx(bi);
            }
            else
            {
                E = getRegByIdx(bi);
            }
        }
        else if (ai == 2)
        {
            if (bi <= 7)
            {
                H = getRegByIdx(bi);
            }
            else
            {
                L = getRegByIdx(bi);
            }
        }
        else if (ai == 3) // 7 - 4 = 3
        {
            if (bi <= 7)
            {
                S.addr(HL) = getRegByIdx(bi);
            }
            else
            {
                A = getRegByIdx(bi);
            }
        }
    }

    void add_reg(ref byte register, byte value)
    {
        F.half_carry = (register & 0x0f) == 0x0f;
        F.negative = false;
        register += value;
        F.zero = register == 0;
    }
    delegate void register_method(ref byte i);
    void al_rr(int op_operand, register_method left, register_method right)
    {
        if (op_operand <= 7)
        {
            left(ref A);
        }
        else
        {
            right(ref A);
        }
    }

    void or_reg(ref byte reg, byte v)
    {
        F.negative = false;
        F.carry = false;
        F.half_carry = false;
        reg |= v;
        F.zero = reg == 0;
    }

    void and_reg(ref byte reg, byte v)
    {
        F.negative = false;
        F.carry = false;
        F.half_carry = false;
        reg &= v;
        F.zero = reg == 0;
    }

    void xor_reg(ref byte reg, byte v)
    {
        F.negative = false;
        F.carry = false;
        F.half_carry = false;
        reg ^= v;
        F.zero = reg == 0;
    }

    void cp_reg(ref byte reg, byte v)
    {
        var old = reg;
        sub_reg(ref reg, v);
        reg = old;
    }

    // TODO: See if inlining helps any, https://stackoverflow.com/questions/473782/inline-functions-in-c
    void sub_reg(ref byte register, byte value)
    {
        F.negative = true;
        F.carry = value > register;
        F.half_carry = (value & 0x0f) > (register & 0x0f);
        register -= value;
        F.zero = register == 0;
    }

    bool no_modify_pc = false;
    public void Tick()
    {
        cache_store();
        byte op = S.addr(PC);
        switch (op)
        {
            case 0x00:
                return;
            case 0x01:
                BC = getShortAtPC();
                return;
            case 0x18:
                // JUMP RELATIVE
                PC = (ushort)(PC + (sbyte)getByteAtPC());
                // no_modify_pc = true;
                return;
            case 0xc9:
                // JUMP RELATIVE
                PC = getShortAtSP();
                no_modify_pc = true;
                return;
            case 0xc3:
                PC = getShortAtPC();
                no_modify_pc = true;
                return;
            case 0x21:
                HL = (getShortAtPC());
                return;
            case 0x23:
                HL++;
                return;
            case 0x03:
                BC++;
                return;
            case 0x13:
                DE++;
                return;
            case 0x33:
                SP++;
                return;
            case 0x11:
                DE = getShortAtPC();
                return;
            case 0x12:
                S.addr(DE) = A;
                return;
            case 0x1c:
                add_reg(ref E, 1);
                return;
            case 0x1d:
                sub_reg(ref E, 1);
                return;
            case 0x0d:
                sub_reg(ref C, 1);
                return;
            case 0x14:
                add_reg(ref D, 1);
                return;
            case 0x24:
                add_reg(ref H, 1);
                return;
            case 0x32:
                A = S.addr(HL--);
                return;
            case 0x22:
                S.addr(H++) = A;
                return;
            case 0x1a:
                A = S.addr(DE);
                return;
            case 0x0a:
                A = S.addr(BC);
                return;

            case 0xe0:
                S.addr((ushort)(0xff00 + getByteAtPC())) = A;
                return;
            case 0xf0:
                A = S.addr((ushort)(0xff00 + getByteAtPC()));
                return;
            case 0xfe:
                cp_reg(ref A, A);
                return;
            case 0x20:
                if (!F.zero)
                {
                    // JR
                    PC = (ushort)(PC + (sbyte)getByteAtPC());
                    // no_modify_pc = true;
                }
                return;
            case 0x28:
                if (F.zero)
                {
                    // JR
                    PC = (ushort)(PC + (sbyte)(getByteAtPC()));
                    // no_modify_pc = true;
                }
                return;
            case 0x30:
                if (!F.carry)
                {
                    // JR
                    PC = (ushort)(PC + (sbyte)(getByteAtPC()));
                    // no_modify_pc = true;
                }
                return;
            case 0x1f:
                int b0 = A & 0x01;
                F.carry = b0 == 1;
                F.negative = false;
                F.half_carry = false;
                A >>= 1;
                F.zero = A == 0;
                return;
            case 0xce:
                add_reg(ref A, (byte)(getByteAtPC() + (F.carry ? 1 : 0)));
                return;
            case 0x0e:
                C = getByteAtPC();
                return;
            case 0x2a:
                A = S.addr(HL++);
                return;
            case 0x3a:
                A = S.addr(HL--);
                return;
            case 0x31:
                SP = getShortAtPC();
                return;
            case 0xff:
                // RST 38h
                writeShortAtSP(PC);
                PC = 0x0000 + 0x38;
                no_modify_pc = true;
                return;
            case 0xef:
                // RST 38h
                writeShortAtSP(PC);
                PC = 0x0000 + 0x28;
                no_modify_pc = true;
                return;
            case 0xe5:
                writeShortAtSP(HL);
                return;
            case 0xf5:
                writeShortAtSP(AF);
                return;
            case 0xd5:
                writeShortAtSP(DE);
                return;
            case 0xc5:
                writeShortAtSP(BC);
                return;
            case 0xe1:
                HL = getShortAtSP();
                return;
            case 0xf1:
                AF = getShortAtSP();
                return;
            case 0xd1:
                DE = getShortAtSP();
                return;
            case 0xc1:
                BC = getShortAtSP();
                return;
            case 0xdf:
                // RST 38h
                writeShortAtSP(PC);
                PC = 0x0000 + 0x18;
                no_modify_pc = true;
                return;
            case 0xcf:
                // RST 38h
                writeShortAtSP(PC);
                PC = 0x0000 + 0x08;
                no_modify_pc=true;
                return;
            case 0xEA:
                var addr = getShortAtPC();
                S.addr(addr) = A;
                return;
            case 0xf3:
                S.interrupts.enabled = false;
                return;
            case 0xfb:
                S.interrupts.enabled = true;
                return;
            case 0x3e:
                A = getByteAtPC();
                return;
            case 0x2e:
                L = getByteAtPC();
                return;
            case 0x1e:
                E = getByteAtPC();
                return;

            case 0xCD:
                writeShortAtSP((ushort)(PC + 3));
                PC = getShortAtPC();
                no_modify_pc = true;
                return;

            case 0xC4:
                if (!F.zero)
                {
                    writeShortAtSP((ushort)(PC + 3));
                    PC = getShortAtPC();
                    no_modify_pc = true;
                }
                return;
            case 0xc6:
                add_reg(ref A, getByteAtPC());
                return;
            case 0xd6:
                sub_reg(ref A, getByteAtPC());
                return;
            case 0xfa:
                A = S.addr(getShortAtPC());
                return;
            case 0xe6:
                and_reg(ref A, getByteAtPC());
                return;
            case 0xf6:
                or_reg(ref A, getByteAtPC());
                return;
            case 0x06:
                B = getByteAtPC();
                return;
            case 0x05:
                sub_reg(ref B, 1);
                return;
            case 0x2c:
                add_reg(ref L, 1);
                return;
            // case 0x0e:
            //     C = getByteAtPC();
            //     return;

            default:
                // Console.WriteLine("[!!] Unexpected instruction: {0,2:x}", op);
                break;
        }

        int op_id = op >> 4;
        int op_operand = op & 0x0f;

        switch (op_id)
        {
            case 0x4:
            case 0x5:
            case 0x6:
            case 0x7:
                ld_rr(op_id - 4, op_operand);
                return;
            case 0x8:
                al_rr(op_operand, (ref byte x) => add_reg(ref x, getRegByIdx(op_operand)), (ref byte x) => add_reg(ref x, (byte)(getRegByIdx(op_operand) + (F.carry ? 1 : 0))));
                return;
            case 0x9:
                al_rr(op_operand, (ref byte x) => sub_reg(ref x, getRegByIdx(op_operand)), (ref byte x) => sub_reg(ref x, (byte)(getRegByIdx(op_operand) + (F.carry ? 1 : 0))));
                return;
            case 0xB:
                al_rr(op_operand, (ref byte x) => or_reg(ref x, getRegByIdx(op_operand)), (ref byte x) => cp_reg(ref x, getRegByIdx(op_operand)));
                return;
            case 0xA:
                al_rr(op_operand, (ref byte x) => and_reg(ref x, getRegByIdx(op_operand)), (ref byte x) => xor_reg(ref x, getRegByIdx(op_operand)));
                return;
                // case 
        }

        Console.WriteLine("\x1b[31m[!!] Unexpected instruction: {0:x2}\x1b[0m", op);
    }
    public void incrementPC(int v)
    {
        if (!no_modify_pc)
        {
            PC += (ushort)v;
        } else {
            Console.WriteLine("skipping increment cuz of jump / call / ret");
            no_modify_pc = false;
        }
    }
}