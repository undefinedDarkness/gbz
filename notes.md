# CPU
- [Operation Code Table](https://izik1.github.io/gbops/index.html)
- [Implementation 1](https://github.com/CTurt/Cinoop/blob/master/source/cpu.c) (C, Kinda inaccurate)
- [Implementation 2](https://github.com/HFO4/gameboy.live/blob/master/gb/opcodes.go) (Go, Accurate)
- [SM83 Instruction Decoding](https://cdn.discordapp.com/attachments/465586075830845475/742438340078469150/SM83_decoding.pdf) Haven't used it myself
- [Execution Logs](https://github.com/wheremyfoodat/Gameboy-logs) Very useful to debug instruction implementations
- [BGB Debugger](https://bgb.bircd.org/) Also very useful to step through instructions
- [emudev.de](http://emudev.de/gameboy-emulator/interrupts-and-timers/) Nice guide and  concise

## Interrupts
For interrupts to work, they first need to be **enabled** at all (operations EI & DI) and then the specific interrupts needs to have its bit set in the interrupt enable mask at `0xffff`



## Progress
- [X] Test 1 Special
- [ ] Test 2 Interrupt
- [X] Test 3 op sp,hl
- [X] Test 4 op r,imm
- [X] Test 5 op rp
- [X] Test 6 ld r,r
- [X] Test 7 jr,jp,call,ret,rst
- [X] Test 8 misc instrs
- [X] Test 9 op r,r
- [ ] Test 10 bitops
- [ ] Test 11 op a,(hl)

## Registers
- AF - Accumalator & Flags Register
- BC, DE, HL - General Purpose Register
- PC - Program Counter

When used as 16bit registers, Example for BC, B is the high byte and C is the low byte
```
[xxxx][xxxxx]
[HIGH][ LOW ]
```

F Layout:
- Bit 0,1,2,3: Unused (0)
- Bit 4: Carry Flag
- Bit 5: Half Carry Flag
- Bit 6: Subtraction Flag
- Bit 7: Zero Flag

Source: [Pandocs](https://gbdev.io/pandocs/CPU_Registers_and_Flags.html)
