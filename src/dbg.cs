class Instruction {
    public string? opcode {get;set;}
    public string? prefix {get;set;}
    public string? mnemonic {get;set;}
    public string[]? operands {get;set;}
    public int bytes {get;set;}
    public int cycles {get;set;}
    public string[]? flagsZNHC {get;set;}
}

class Debugger {
    Instruction[] instructions = new Instruction[16 * 16];
    STATE S;
    CPU C;
    public Debugger(STATE _s, CPU _c) {
        S = _s;
        C = _c;
        var opcodes_json = System.IO.File.ReadAllText("src/opcodes.json").Split('\n');   
        foreach (string opcode in opcodes_json) {
            var instruction = System.Text.Json.JsonSerializer.Deserialize<Instruction>(opcode);
            if (instruction.prefix == null) {
                instructions[System.Convert.ToInt32(instruction.opcode, 16)] = instruction;
            }
            // instructions.Insert(System.Convert.ToInt32(instruction.opcode, 16), instruction);
        }
    }

    string color(string opname) {
        return opname switch {
            "JP" or "JR" => $"\x1b[33m{opname}\x1b[0m",
            "CALL" or "RET" => $"\x1b[34m{opname}\x1b[0m",
            "POP" or "PUSH" => $"\x1b[35m{opname}\x1b[0m",
            "LD" or "LDI" or "LDH" => $"\x1b[32m{opname}\x1b[0m",
            "ADD" or "ADC" or "SUB" or "SBC" or "OR" or "AND" or "XOR" or "INC" or "DEC" => $"\x1b[36m{opname}\x1b[0m",
            _ => opname
        };
    }

    // bool full_jump = false;
    byte opcode = 0x00;
    bool stepDebug = true;
    bool noprintNoOp = true;
    HashSet<ushort> breakpoints = new HashSet<ushort>();
    HashSet<byte> breakinstrs = new HashSet<byte>();

    void printMemoryAtAddress(int addr) {
        for (int i = 0; i < 10; i++) {
            for (int j = 0; j < 16; j++) {
                Console.Write($"{S.addr((ushort)(addr + j + i*16)).ToString("X2")} ");
            }
            Console.WriteLine();
        }
    } 
    bool stop_at_invalid_access = true;
    public void DebugTick() {
        opcode = S.addr(C.PC);
        bool inBreakpoint = breakpoints.Contains(C.PC) || breakinstrs.Contains(opcode);
        if (inBreakpoint) {
            stepDebug = true;
        }
        

        // if (!stepDebug && noprintNoOp && opcode == 0x00)
        //     return;
        var name = instructions[opcode].mnemonic;
        // full_jump = name == "JP" || name == "CALL" || name == "RET";

        // if (!stepDebug)
        //     return;

        if (name.StartsWith("LD") && S.had_invalid_access && stop_at_invalid_access) {
            stepDebug = true;
            S.had_invalid_access = false;
        }

        string operands = "";
        if (instructions[opcode].operands != null) {
            foreach (string operand in instructions[opcode].operands) {
                operands += " ";
                if (operand == "a16" || operand == "(a16)") {
                    operands += $"${C.getShortAtPC().ToString("X4")}"; //C.getShortAtPC().ToString("X4");
                } else if (operand == "a8" || operand == "(a8)") {
                    operands += $"${( name == "LDH" ? 0xff00 + C.getByteAtPC() : 0x00000 + C.getByteAtPC() ).ToString("X2")}";
                } else if (operand == "r8") { 
                    operands += $"${(C.PC + (sbyte)C.getByteAtPC() + instructions[opcode].bytes).ToString("X4")}";
                } else if (operand == "d16") {
                    operands += $"0x{C.getShortAtPC().ToString("X4")}";
                } else if (operand == "d8") {
                    operands += $"0x{C.getByteAtPC().ToString("X2")}";
                } else {
                    operands += operand;
                }
            }
        }
        string leftFmt = string.Format("{0:x4}: {3:x2} {1}{2}", inBreakpoint ? $"\x1b[31m{C.PC.ToString("x4")}\x1b[0m" : C.PC, color(instructions[opcode].mnemonic), operands, opcode);
        string rightFmt = string.Format("AF:{0:X4} BC:{1:X4} DE:{2:X4} HL:{4:X4} SP:{3:X4}", C.AF, C.BC, C.DE, C.SP, C.HL).PadLeft(160 - (4 + 2 + 2 + 1 + operands.Length));
        System.Console.WriteLine("{0}{1}", leftFmt, rightFmt);

        if (!stepDebug)
            return;

takeInput:
        var userinput = Console.ReadLine()!.Split(" ");
        if (userinput[0] == "c" || userinput[0] == "n") {
            return;
        } else if (userinput[0] == "b") {
            breakpoints.Add(System.Convert.ToUInt16(userinput[1], 16));
        } else if (userinput[0] == "bins") {
            breakinstrs.Add(System.Convert.ToByte(userinput[1], 16));
        }else if (userinput[0] == "ex") {
            System.Environment.Exit(0);
        } else if (userinput[0] == "cont" || userinput[0] == "next") {
            stepDebug = false;
            return;
        } else if (userinput[0] == "rmb") {
            breakpoints.Remove(System.Convert.ToUInt16(userinput[1], 16));
        } else if (userinput[0] == "rmbins") {
            breakinstrs.Remove(System.Convert.ToByte(userinput[1], 16));
        } else if (userinput[0] == "mhead") {
            int addr = userinput[1] switch {
                "wram" => 0xc000,
                "rom" => 0,
                "pc" or "PC" => C.PC,
                _ => System.Convert.ToInt32(userinput[1], 16)
            };
            printMemoryAtAddress(addr);
        } else if (userinput[0] == "reset") {
            C.reset();
            full_jump = true;
            return;
        } else if (userinput[0] == "rewind") {
            C.rewind();
            return;
        } else if (userinput[0] == "stopinvalidacc") {
            stop_at_invalid_access = !stop_at_invalid_access; // TODO: make this work correctly
        } else if (userinput[0] == "nextaddr") {
            Console.WriteLine($"${ (C.PC + instructions[opcode].bytes).ToString("x4") }");
        } else if (userinput[0] == "help") {
            Console.WriteLine("-- RTFM --");
        }
        goto takeInput;
    }

    public void IncrementPC() {
        // if (!C.no_modify_pc) {
            C.incrementPC(instructions[opcode].bytes);
            // full_jump = false;
        // }
    }
}