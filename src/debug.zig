const std = @import("std");

var opcode_data: std.json.Value = undefined;
var alloc: std.mem.Allocator = undefined;
var parsed: std.json.Parsed(std.json.Value) = undefined;

// TODO: Update with https://github.com/ziglang/zig/issues/11712

pub fn init(_alloc: std.mem.Allocator) !void {
    alloc = _alloc;
    parsed = try std.json.parseFromSlice(std.json.Value, alloc, @embedFile("opcodes.json"), .{});// json_parser = //std.json.parseFromTokenSourceLeaky(comptime T: type, allocator: Allocator, scanner_or_reader: anytype, options: ParseOptions)
    opcode_data = parsed.value;
}

pub fn deinit() void {
    parsed.deinit();
    // opcode_data.deinit();
}

fn getOpCodeObj(op_code: u8) ?std.json.Value {
    const root = opcode_data.object;
    var instructions = if (op_code != 0xCB) root.get("unprefixed").?.object else root.get("cbprefixed").?.object;

    var as_hex: [4]u8 = undefined;
    _ = std.fmt.bufPrint(as_hex[0..], "0x{X:0>2}", .{op_code}) catch unreachable;
    return instructions.get(&as_hex);
}

var in_call: u32 = 0;
fn color(op_code: []const u8) []const u8 {
    const cmp = (struct {
        pub fn cmp(a: []const u8, b: []const u8) bool {
            return std.mem.eql(u8, a, b);
        }
    }).cmp;
    if (cmp("CALL", op_code)) {
        in_call += 1;
        return "\u{001b}[31mCALL\u{001b}[0m";
    } else if (cmp("RET", op_code)) {
        in_call -= 1;
        return "\u{001b}[31mRET\u{001b}[0m";
    } else if (cmp("JP", op_code)) {
        return "\u{001b}[33mJP\u{001b}[0m";
    } else if (cmp("JR", op_code)) {
        return "\u{001b}[33mJR\u{001b}[0m";
    } else if (cmp("LD", op_code)) {
        return "\u{001b}[34mLD\u{001b}[0m";
    } else if (cmp("CP", op_code)) {
        return "\u{001b}[33mCP\u{001b}[0m";
    } else if (cmp("LDH", op_code)) {
        return "\u{001b}[34mLDH\u{001b}[0m";
    } else if (cmp("XOR", op_code)) {
        return "\u{001b}[32mXOR\u{001b}[0m";
    } else if (cmp("AND", op_code)) {
        return "\u{001b}[32mAND\u{001b}[0m";
    } else if (cmp("DEC", op_code)) {
        return "\u{001b}[32mDEC\u{001b}[0m";
    } else if (cmp("INC", op_code)) {
        return "\u{001b}[32mINC\u{001b}[0m";
    }
    return op_code;
}

pub const Instr = struct {
    nbytes: u32,
    cycles: u32
};

pub fn debugOpCode(op_code: u8, op_args: u16) !Instr {
    // std.debug.print("{x} ", .{op_code[0]});//: anytype)
    const obj = getOpCodeObj(op_code);
    if (obj) |o| {
        const is_wide = op_code == 0xCB;
        const instr = Instr{ .nbytes = @as(u32, @intCast(o.object.get("bytes").?.integer)), .cycles = @as(u32, @intCast(o.object.get("cycles").?.array.items[0].integer)) };
        const op_name = color(o.object.get("mnemonic").?.string);
        const op_nargs: usize = instr.nbytes - 1;
        if (op_code == 0x00) {
            return instr;
        }
        for (0..in_call) |_| {
            std.debug.print("\t", .{});
        }
        if (is_wide) {
            std.debug.print("{x:0>2} \u{001b}[35mWIDE:\u{001b}[0m {s} {x:0>4}\t\t", .{ op_args & 0x00ff, op_name, 0x00 });
        } else {
            std.debug.print("{x:0>2} {s} {x:0>4}\t\t", .{ op_code, op_name, if (op_nargs > 0) op_args else 0x00 });
        }
        return instr;
    } else {
        return error.CannotFindOpCode; //std.debug.panic("CANNOT FIND OPERATION CODE: {x:0>4}", .{std.fmt.fmtSliceHexLower(op_code[0..2])});
    }
}
const self = @This();
test "debug : basic" {
    var m_alloc = std.testing.allocator;
    try self.init(m_alloc);
    // try std.testing.expectEqualSlices(u8, "NOP", try getOpCodeName(0x0000));
    defer self.deinit();
}
