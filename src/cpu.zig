const std = @import("std");
const m_rom = @import("rom.zig"); //.ROM;
const Debug = @import("debug.zig");
const m_io = @import("io.zig");
const builtin = @import("builtin");

pub inline fn get_u16(d: []const u8, pc: u16) u16 {
    return @as(u16, d[pc + 1]) << 8 | d[pc + 0];
}

pub inline fn set_u16(d: []u8, v: u16) void {
    d[0] = @truncate(v);
    d[1] = @truncate(v >> 8);
}

const InterruptFlags = packed struct {
    vblank: bool = false,
    lcd_stat: bool = false,
    timer: bool = false,
    serial: bool = false,
    joypad: bool = false,
    _padding: u3 = 0,
    pub inline fn g(d: *@This()) u8 {
        return @bitCast(d.*);
    }
    pub inline fn disable_all(d: *@This()) void {
        d.vblank = false;
        d.lcd_stat = false;
        d.timer = false;
        d.serial = false;
        d.joypad = false;
    }
    pub inline fn enable_all(d: *@This()) void {
        d.vblank = true;
        d.lcd_stat = true;
        d.timer = true;
        d.serial = true;
        d.joypad = true;
    }
};
const Register = packed struct(u16) {
    hi: u8 = 0,
    lo: u8 = 0,

    pub inline fn g(d: *@This()) u16 {
        return @bitCast(d.*);
    }

    pub inline fn s(d: @This(), v: u16) void {
        @as(*u16, @ptrCast(d)).* = v;
    }

    pub inline fn i(d: *@This()) *u16 {
        return @as(*u16, @ptrCast(d));
    }
};
const Registers = struct {
    af: packed struct(u16) {
        f: packed struct(u8) {
            _padding: u4 = 0, // bits 0, 1, 2, 3
            c: bool = false,
            h: bool = false,
            n: bool = false,
            z: bool = true,
            pub inline fn g(d: *@This()) *u8 {
                return @ptrCast(d); //, value: anytype)
            }
        } = .{},
        a: u8 = 0x11,

        pub inline fn g(d: *@This()) u16 {
            return @as(u16, @bitCast(d.*));
        }
    } = .{},
    bc: Register = .{ .lo = 0x00, .hi = 0x00 },
    de: Register = .{ .lo = 0x56, .hi = 0xff },
    hl: Register = .{ .lo = 0x0d, .hi = 0x00 },
    sp: u16 = 0xfffe, // Stack Pointer
    pc: u16 = 0x0100,

    pub fn debugPrint(d: *@This()) void {
        std.debug.print("AF[{x:0>4}] BC[{x:0>4}] HL[{x:0>4}] DE[{x:0>4}] SP[{x:0>4}]\n", .{ d.af.g(), d.bc.g(), d.hl.g(), d.de.g(), d.sp });
    }
};

var R: Registers = .{};
var RAM: [8200]u8 = undefined; //.mem.zeroes([8000]u8); // 0xC000 -> 0xDFFF
var ROM: m_rom.ROM = undefined;
var HRAM: [0xfffe - 0xff80]u8 = undefined;
var IO: m_io = undefined;
var INTERRUPT: packed struct {
    // TODO: Maybe remove packing here, not required
    enabled: InterruptFlags = .{},
    mem: InterruptFlags = .{},
} = .{};

pub fn init(alloc: std.mem.Allocator, loaded_rom: m_rom.ROM) !void {
    try Debug.init(alloc);
    ROM = loaded_rom;
    RAM = std.mem.zeroes(@TypeOf(RAM));
    HRAM = std.mem.zeroes(@TypeOf(HRAM));
    IO = m_io.init();
}

pub fn deinit() void {
    Debug.deinit();
}

var garbage: [100]u8 = std.mem.zeroes([100]u8);
fn address(addr: u16) []const u8 {
    var zz = garbage[0..];

    if (addr <= 0x7fff) {
        return ROM.data[0..];
    }

    // Interrupt Switches
    if (addr == 0xffff) {
        return @as([2]u8, @bitCast(INTERRUPT))[0..];
    } else if (addr == 0xff0f) {
        return @as([2]u8, @bitCast(INTERRUPT))[1..];
    }

    // Working RAM
    if (addr >= 0xc000 and addr <= 0xdfff) {
        return RAM[addr - 0xc000 ..];
    } else if (addr >= 0xe000 and addr <= 0xfdff) {
        return RAM[addr - 0xe000 ..]; // echo ram
    }

    // IO
    if (addr >= 0xff00 and addr <= 0xff7f) {
        return IO.buffer[addr - 0xff00 ..];
    }

    std.log.warn("access to {x} is being ignored since not implemented.", .{addr});
    return zz;
}

inline fn genericWideInstruction(target: u8, comptime opd: anytype) void {
    const registers = [_]*u8{ &R.bc.lo, &R.bc.hi, &R.de.lo, &R.de.hi, &R.hl.lo, &R.hl.hi, &garbage[0], &R.af.a };
    if (target == 6) {
        opd.c(u16, R.hl.i());
        return;
    }

    opd.c(u8, registers[target]);
}

fn wideInstruction() void {
    const operation = ROM.data[R.pc + 1];
    const op_id = 1 + operation >> 4;
    const op_target = operation & 0x0f;
    R.pc += 1;

    if (op_target <= 7) {
        switch (op_id) {
            0x3 => {
                genericWideInstruction(op_target, struct {
                    fn c(comptime T: type, v: *T) void {
                        const a = v.* & 0x0f;
                        const b = v.* & 0xf0;
                        v.* = b << 4 | a >> 4;
                    }
                });
            },
            0x4 => {
                genericWideInstruction(op_target, struct {
                    fn c(comptime T: type, v: *T) void {
                        R.af.f.z = ~(v.* & 0x1) == 1;
                    }
                });
            },
            else => {
                std.log.err("Unimplemented wide instruction ({x:0>2} : ID: {x}, TARGET: {x})", .{ operation, op_id, op_target });
                std.process.exit(0);
            },
        }
    }
}

// ROM is 0x0000 -> 0x3FFF
pub fn run() !void {
    while (R.pc < ROM.romSize()) {
        const op: u8 = ROM.data[R.pc];
        const op_args = get_u16(ROM.data, R.pc + 1);
        if (op != 0x00) {
            std.debug.print("{x:0>4}: ", .{R.pc});
        }
        const instr: Debug.Instr = try Debug.debugOpCode(op, op_args);
        if (op != 0x00) {
            // std.debug.print("{x:0>4}: ", .{R.pc});
            // instr = try Debug.debugOpCode(op, op_args);
            R.debugPrint();
            IO.gamelinkDbg();
        }

        if (!cycle(op)) {
            break;
        }

        R.pc += @truncate(instr.nbytes);
    }
}

fn cycle(op: u8) bool {
    switch (op) {
        0x00 => {},
        0x03 => R.bc.i().* += 1,
        0x31 => {
            R.sp = get_u16(ROM.data, R.pc + 1);
            //R.sp.lo = ROM.data[R.pc + 1];
            //R.sp.hi = ROM.data[R.pc + 2];
            // R.pc += 2;
        },
        0xc3 => {
            R.pc = get_u16(ROM.data, R.pc + 1) - 3;
        },
        0x20 => {
            if (R.af.f.z) {
                R.pc = R.pc + ROM.data[R.pc + 1];
            }
            // R.pc += 1;
        },
        0x28 => {
            if (R.af.f.z) {
                R.pc += ROM.data[R.pc + 1] + 1;
            } else {
                // R.pc += 1;
            }
        },
        0x2A => {
            R.af.a = address(R.hl.g())[0];
            R.hl.i().* += 1;
        },
        0x12 => {
            R.af.a = address(R.de.g())[0];
            // R.hl.i().* += 2;
        },
        0x1c => R.de.hi += 1,
        0x14 => R.de.lo += 1,
        0xaf => {
            R.af.a = 0; // xoring A with A will zero it
        },
        0x7D => {
            R.af.a = R.hl.hi;
        },
        0x7C => {
            R.af.a = R.hl.lo;
        },
        0x18 => {
            R.pc += ROM.data[R.pc + 1] + 1;
        },
        0xe5 => {
            R.sp -= 2;
            var a = @constCast(address(R.sp));
            set_u16(a, R.hl.g());
        },
        0xc5 => {
            R.sp -= 2;
            var a = @constCast(address(R.sp));
            a[0] = R.bc.lo;
            a[1] = R.bc.hi;
        },
        0xe1 => {
            const a = address(R.sp);
            R.hl.lo = a[0];
            R.hl.hi = a[1];
            R.sp += 2;
        },
        0xf5 => {
            R.sp -= 2;
            var a = @constCast(address(R.sp));
            set_u16(a, R.af.g());
        },
        0xf1 => {
            const a = address(R.sp);
            R.af.a = a[0];
            R.af.f.g().* = a[1];
            R.sp += 2;
        },
        0x23 => {
            R.hl.i().* += 1;
        },
        0x21 => {
            R.hl.i().* = get_u16(ROM.data, R.pc + 1);
        },
        0x01 => {
            R.bc.lo = ROM.data[R.pc + 1];
            R.bc.hi = ROM.data[R.pc + 2];
            // R.pc += 2;
        },
        0x0b => {
            R.bc.i().* -= 1;
        },
        0x78 => {
            R.af.a = R.bc.lo;
        },
        0x0e => {
            R.bc.hi = ROM.data[R.pc + 1];
            // R.pc += 1;
        },
        0x05 => {
            R.bc.i().* -= 1;
        },
        0x0d => {
            R.bc.hi -= 1;
        },
        0x06 => {
            R.bc.lo = ROM.data[R.pc + 1];
            // R.pc += 1;
        },
        0x32 => {
            @constCast(address(R.hl.g()))[0] = R.af.a;
            R.hl.i().* -= 1;
        },
        0x3e => {
            R.af.a = ROM.data[R.pc + 1];
            // R.pc += 1;
        },
        0xf3 => {
            INTERRUPT.enabled.disable_all();
        },
        0xfb => {
            INTERRUPT.enabled.enable_all();
        },
        0xe0 => {
            const addr: u16 = ROM.data[R.pc + 1];
            @constCast(address(0xff00 + addr))[0] = R.af.a;
            // R.pc += 1;
        },
        0xf0 => {
            const addr: u16 = ROM.data[R.pc + 1];
            R.af.a = address(0xff00 + addr)[0];
            // R.pc += 1;
        },
        0xe2 => {
            const addr: u16 = R.bc.hi;
            @constCast(address(0xff00 + addr))[0] = R.af.a;
            // R.pc += 1;
        },
        0xea => {
            const addr = get_u16(ROM.data, R.pc + 1);
            @constCast(address(addr))[0] = R.af.a;
            // R.pc += 2;
        },
        0xb1 => {
            R.af.a = R.af.a | R.bc.hi;
        },
        0x0c => {
            R.bc.hi += 1;
        },
        0xc9 => {
            const spa = address(R.sp);
            R.sp += 2;
            R.pc = get_u16(spa, 0);
        },
        0xcd => {
            R.sp -= 2;
            var spa = @constCast(address(R.sp));
            set_u16(spa, R.pc + 2); // pointing to next instruction after CALL
            const callee = get_u16(ROM.data, R.pc + 1);
            _ = callee;
            // R.pc = callee - 1;
        },
        0xfe => {
            const arg = ROM.data[R.pc + 1];
            R.af.f.n = true;
            R.af.f.z = R.af.a == arg;
            R.af.f.c = arg > R.af.a;
            R.af.f.h = (arg & 0x0f) > (R.af.a & 0x0f); // compare nibbles?
            // R.pc += 1;
        },
        0xcb => {
            // R.pc += 1;
            wideInstruction();
        },
        0x2f => {
            R.af.a = ~R.af.a;
        },
        0x47 => {
            R.bc.lo = R.af.a;
        },
        0x11 => {
            R.de.lo = ROM.data[R.pc + 1];
            R.de.hi = ROM.data[R.pc + 2];
            // R.pc += 2;
        },
        0xe6 => {
            R.af.a |= ROM.data[R.pc + 1];
            // R.pc += 1;
        },
        0x36 => {
            @constCast(address(R.hl.g()))[0] = ROM.data[R.pc + 1];
            // R.pc += 1;
        },
        else => {
            std.log.err("<-- INSTRUCTION NOT IMPLEMENTED @ {x:0>4} -->", .{R.pc}); //try Debug.debugOpCode(@as(u16, op) << 8 | rom.data[R.pc+1]);
            return false;
        },
    }
    return true;
}

const s = @This();
// test "cpu : compiles" {
//     try s.init(std.testing.allocator);
//     defer s.deinit();
// }

test "cpu : blargg cpu-instrs" {
    const rom = try m_rom.loadFile(std.testing.allocator, "gb-test-roms/cpu_instrs/individual/01-special.gb");
    defer rom.deinit();
    std.log.info("loading {s}", .{rom.name()});
    try s.init(std.testing.allocator, rom);
    try s.run();
    defer s.deinit();
}
