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
    public Flags F = new Flags(true, false, false, false); // zero negative half-carry carry
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
            F.byte_value = (byte)(value & 0x00f0);
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
        reset();
        no_modify_pc = false;
    }

    public void resetBGB()
    {
        PC = 0x0100;
        SP = 0xFFFE;
        AF = 0x1180;
        BC = 0x0000;
        DE = 0xFF56;
        HL = 0x000D;
        no_modify_pc = true;
    }

    public void reset()
    {
        PC = 0x0100;
        SP = 0xFFFE;
        A = 0x01;
        F.byte_value = 0xB0;
        B = 0x00;
        C = 0x13;
        D = 0x00;
        E = 0xD8;
        H = 0x01;
        L = 0x4D;
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
            6 => S.addr(HL),
            7 => A,
            _ => garbage
        };
    }

    void ld_rr(int ai, int bi)
    {
        if (ai == 0) // 0x4
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
        else if (ai == 1) // 0x5
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
        else if (ai == 2) // 0x6
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
        else if (ai == 3) // 0x7
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

    void sbc_reg(ref byte reg, int v) {
        short carry = F.carry ? (short)1 : (short)0;
        short sum = (short)(A - (short)v - carry);
        byte total = (byte)sum;
        F.zero = total == 0;
        F.half_carry = (short)(A & 0x0f) - (short)(v & 0x0f) - carry < 0;
        F.carry = sum < 0;
        F.negative = true;
        A = total;
    }

    void add_reg(ref byte register, byte value)
    {
        F.half_carry = (register & 0x0f) + (value & 0x0f) > 0x0f;
        int result = (int)(register) + (int)(value);
        register = (byte)result;
        F.zero = ((byte)result) == 0;
        F.negative = false;
        F.carry = result > 0xff;
    }

    ushort add_wide(int wr, int v)
    {
        var result = wr + v;
        F.carry = result > 0xffff;
        // F.zero = false;
        F.half_carry = (wr & 0xfff) > (result & 0xfff);
        F.negative = false;
        return (ushort)(result);
    }

    void inc_reg(ref byte register)
    {
        F.half_carry = (register & 0x0f) == 0x0f;
        register++;
        F.zero = register == 0;
        F.negative = false;
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

    void cb_rr(int op_operand, register_method left, register_method right)
    {
        var method = left;
        if (op_operand >= 7)
        {
            method = right;
        }
        switch (op_operand % 8)
        {
            case 0:
                method(ref B);
                break;
            case 1:
                method(ref C);
                break;
            case 2:
                method(ref D);
                break;
            case 3:
                method(ref E);
                break;
            case 4:
                method(ref H);
                break;
            case 5:
                method(ref L);
                break;
            case 6:
                method(ref S.addr(HL));
                break;
            case 7:
                method(ref A);
                break;
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
        F.half_carry = true;
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
        F.zero = reg == v;
        F.negative = true;
        F.half_carry = (v & 0x0f) > (reg & 0x0f);
        F.carry = v > reg;
    }

    // TODO: See if inlining helps any, https://stackoverflow.com/questions/473782/inline-functions-in-c
    void sub_reg(ref byte register, byte value)
    {
        F.negative = true;
        F.carry = value > register;
        F.half_carry = (value & 0x0f) > (register & 0x0f);// 1 > ... = false
        register -= value;
        F.zero = register == 0;
    }

    void adc_reg(ref byte reg, byte v) {
        short carry = F.carry ? (short)1 : (short)0;
        byte o1 = A;
        byte o2 = v;
        short result = (short)((short)A + (short)v + carry);
        A = (byte)result;
        F.zero = (byte)result == 0;
        F.negative = false;
        F.half_carry = (o1 & 0xf) + (o2 & 0xf) + (byte)(carry) > 0xf;
        F.carry = result > 0xff;
    }

    void dec_reg(ref byte register)
    {
        F.half_carry = (register & 0x0f) == 0;
        register = (byte)(register - 1);
        F.zero = register == 0;
        F.negative = true;
    }

    bool no_modify_pc = false;
    public void Tick()
    {
        cache_store();
        var op = S.addr(PC);
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
            case 0x36:
                S.addr(HL) = getByteAtPC();
                return;
            case 0x16:
                D = getByteAtPC();
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
            case 0x26:
                H = getByteAtPC();
                return;
            case 0x2d:
                dec_reg(ref L);
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
            case 0xde:
                sbc_reg(ref A, getByteAtPC());
                return;
            case 0x11:
                DE = getShortAtPC();
                return;
            case 0x08:
                var address = getShortAtPC();
                S.addr(address++) = (byte)(SP & 0x00ff);
                S.addr(address) = (byte)(SP >> 8);
                return;
            case 0x3b:
                SP--;
                return;
            case 0x39:
                // var ohl = HL;
                HL = add_wide(HL, SP);
                return;
            case 0xe8:
                {
                    var vi = (sbyte)getByteAtPC();
                    var res = (ushort)((int)vi + (int)SP);
                    var tmp = res ^ (ushort)vi ^ SP;
                    SP = res;
                    F.zero = false;
                    F.negative = false;
                    F.half_carry = (tmp & 0x10) == 0x10;
                    F.carry = (tmp & 0x100) == 0x100;
                    return;
                }
            case 0xf8:
                {
                    var vi = (sbyte)getByteAtPC();
                    var res = (ushort)((int)vi + (int)SP);
                    var tmp = res ^ (ushort)vi ^ SP;
                    HL = res;
                    F.zero = false;
                    F.negative = false;
                    F.half_carry = (tmp & 0x10) == 0x10;
                    F.carry = (tmp & 0x100) == 0x100;
                    return;
                }
            case 0xf9:
                SP = HL;
                return;
            // -- 
            case 0x12:
                // if (DE == 0xc800)
                S.addr(DE) = A;
                // S.addr(DE) = A;
                if (DE == 0xc800)
                    Console.WriteLine("LD DE, A WROTE {0:X2} to {1:X4}", A, DE);
                return;
            // --
            case 0x1c:
                inc_reg(ref E);//add_reg(ref E, 1);
                return;
            case 0x1d:
                dec_reg(ref E);//sub_reg(ref E, 1);
                return;
            case 0x0d:
                dec_reg(ref C);
                // sub_reg(ref C, 1);
                return;
            case 0x14:
                inc_reg(ref D);//add_reg(ref D, 1);
                return;
            case 0x24:
                inc_reg(ref H);
                // add_reg(ref H, 1);
                return;
            case 0x32:
                S.addr(HL--) = A;
                // A = S.addr(HL--);
                return;
            case 0x22:
                S.addr(HL++) = A;
                return;
            case 0x1a:
                A = S.addr(DE);
                return;
            case 0x0a:
                A = S.addr(BC);
                return;
            case 0x27:

                if (!F.negative)
                {
                    if (F.carry || A > 0x99)
                    {
                        F.carry = true;
                        A += 0x60;
                    }
                    if (F.half_carry || (A & 0x0f) > 0x09)
                    {
                        // F.half_carry = false;
                        A += 0x06;
                    }
                }
                else if (F.carry && F.half_carry)
                {
                    A += 0x9A;
                    // F.half_carry = false;
                }
                else if (F.carry)
                {
                    A += 0xA0;
                }
                else if (F.half_carry)
                {
                    A += 0xFA;
                    // F.half_carry = false;
                }

                F.half_carry = false;
                F.zero = A == 0;
                return;
            case 0x29:
                var ohl = HL;
                HL = add_wide(ohl, ohl);
                return;
            case 0xe0:
                S.addr((ushort)(0xff00 + getByteAtPC())) = A;
                return;
            case 0xf0:
                A = S.addr((ushort)(0xff00 + getByteAtPC()));
                return;
            case 0xfe:
                cp_reg(ref A, getByteAtPC());
                return;
            case 0x20:
                if (!F.zero)
                {
                    // JR
                    PC = (ushort)(PC + (sbyte)getByteAtPC());
                    // no_modify_pc = true;
                }
                return;
            case 0xe9:
                PC = HL;
                no_modify_pc = true;
                return;
            case 0x3c:
                inc_reg(ref A);
                return;
            case 0xc2:
                if (!F.zero)
                {
                    PC = getShortAtPC();
                    no_modify_pc = true;
                }
                return;
            case 0x04:
                inc_reg(ref B);
                return;
            case 0x0c:
                inc_reg(ref C);
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
                var c = F.carry ? 1 << 7 : 0;
                F.carry = (A & (1 << 0)) == 1;
                A >>= 1;
                A |= (byte)c;
                F.negative = false;
                F.zero = false;
                F.half_carry = false;
                return;
            case 0xee:
                xor_reg(ref A, getByteAtPC());
                return;
            case 0x25:
                dec_reg(ref H);
                return;
            case 0x3d:
                dec_reg(ref A);
                return;
            case 0xce:
                adc_reg(ref A, getByteAtPC());
                return;
            // case 0xc8:

            case 0x0e:
                C = getByteAtPC();
                return;
            case 0x2a:
                A = S.addr(HL);
                HL++;
                return;
            case 0x3a:
                A = S.addr(HL);
                HL--;
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
                no_modify_pc = true;
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
            case 0xc8:
                if (F.zero)
                {
                    PC = getShortAtSP();
                    no_modify_pc = true;
                }
                return;
            case 0xc0:
                if (F.zero)
                {
                    PC = getShortAtSP();
                    no_modify_pc = true;
                }
                return;
            case 0xd8:
                if (F.carry)
                {
                    PC = getShortAtSP();
                    no_modify_pc = true;
                }
                return;
            case 0xd0:
                if (!F.carry)
                {
                    PC = getShortAtSP();
                    no_modify_pc = true;
                }
                return;
            case 0x35:
                dec_reg(ref S.addr(HL));
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
                dec_reg(ref B);//sub_reg(ref B, 1);
                return;
            case 0x2c:
                inc_reg(ref L);
                // add_reg(ref L, 1);
                return;
            // case 0x0e:
            //     C = getByteAtPC();
            //     return;
            case 0xCB:
                wideInstruction();
                return;
            default:
                // Console.WriteLine("[!!] Unexpected instruction: {0,2:x}", op);
                break;
        }

        var op_id = op >> 4;
        var op_operand = op & 0x0f;

        switch (op_id)
        {
            case 0x4:
            case 0x5:
            case 0x6:
            case 0x7:
                ld_rr(op_id - 4, op_operand);
                return;
            case 0x8:
                al_rr(op_operand, (ref byte x) => add_reg(ref x, getRegByIdx(op_operand)), (ref byte x) => adc_reg(ref x, getRegByIdx(op_operand)));
                return;
            case 0x9:
                al_rr(op_operand, (ref byte x) => sub_reg(ref x, getRegByIdx(op_operand)), (ref byte x) => sbc_reg(ref x, getRegByIdx(op_operand)));
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
        found_unimplemented_instr = true;
    }


    void wideInstruction()
    {
        var opcode = getByteAtPC();
        var op_id = opcode >> 4;
        var op_operand = opcode & 0x0f;
        switch (op_id)
        {
            case 0x0:
                cb_rr(op_operand, (ref byte x) =>
                {
                    // RLC
                    F.negative = false;
                    F.half_carry = false;
                    F.carry = (x & 1 << 7) == 1;
                    x <<= 1;
                    x |= (byte)(F.carry ? 0x01 : 0);
                    F.zero = x == 0;
                },
                (ref byte x) =>
                {
                    // RRC
                    F.negative = false;
                    F.half_carry = false;
                    F.carry = (x & 1 << 0) == 1;
                    x >>= 1;
                    x |= (byte)(F.carry ? 1 << 7 : 0);
                    F.zero = x == 0;
                });
                return;
            case 0x1:
                cb_rr(op_operand, (ref byte x) =>
                {
                    // RL
                    F.negative = false;
                    F.half_carry = false;
                    var oc = F.carry;
                    F.carry = (x & 1 << 7) == 1;
                    x <<= 1;
                    x |= (byte)(oc ? 1 : 0);
                    F.zero = x == 0;
                },
                (ref byte x) =>
                {
                    // RR
                    F.negative = false;
                    F.half_carry = false;
                    var oc = F.carry;
                    F.carry = (x & 1 << 0) == 1;
                    x >>= 1;
                    x |= (byte)(oc ? 1 << 7 : 0);
                    F.zero = x == 0;
                });
                return;
            case 0x2:
                cb_rr(op_operand, (ref byte x) =>
                {
                    // SLA
                    F.negative = false;
                    F.half_carry = false;
                    var oc = F.carry;
                    F.carry = (x & 1 << 7) == 1;
                    x <<= 1;
                    // x &= 0xfe;
                    F.zero = x == 0;
                },
                (ref byte x) =>
                {
                    F.negative = false;
                    F.half_carry = false;
                    var oc = F.carry;
                    F.carry = (x & 1 << 0) == 1;
                    x = (byte)((x & 1 << 7) | x >> 1);
                    F.zero = x == 0;
                });
                return;
            case 0x3:
                cb_rr(op_operand, (ref byte x) =>
                {
                    // SWAP
                    F.negative = false;
                    F.half_carry = false;
                    F.carry = false;
                    x = (byte)(x >> 4 | x << 4);
                    F.zero = x == 0;
                },
                (ref byte x) =>
                {
                    // SRL
                    F.negative = false;
                    F.half_carry = false;
                    F.carry = (x & 1 << 0) == 1;
                    x >>= 1;
                    F.zero = x == 0;
                });
                return;
            case 0x4:
                cb_rr(op_operand, (ref byte x) =>
                {
                    // SWAP
                    F.negative = false;
                    F.half_carry = false;
                    F.carry = false;
                    x = (byte)(x >> 4 | x << 4);
                    F.zero = x == 0;
                },
                (ref byte x) =>
                {
                    // SRL
                    F.negative = false;
                    F.half_carry = false;
                    F.carry = (x & 1 << 0) == 1;
                    x >>= 1;
                    F.zero = x == 0;
                });
                return;
        }
    }

    public bool found_unimplemented_instr = false;
    public void incrementPC(int v)
    {
        if (!no_modify_pc)
        {
            PC += (ushort)v;
        }
        else
        {
            // Console.WriteLine("skipping increment cuz of jump / call / ret");
            no_modify_pc = false;
        }
    }
}