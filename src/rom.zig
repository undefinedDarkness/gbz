const std = @import("std");
const dbg = @import("debug.zig");

pub const ROM = struct {
    alloc: std.mem.Allocator,
    data: []const u8,

    const self = @This();
    pub fn name(d: *const self) []const u8 {
        return d.data[0x134..0x143];
    }

    pub fn verify(d: *const self) bool {
        _ = d;
        // TODO: Check Nintendo Logo
        // TODO: Check checksums
        @compileError("Not implemented");
    }

    pub fn romSize(d: *const self) usize {
        return 32 * 1000 * (@as(u32, 1) << @truncate(d.data[0x148]));
    }

    pub fn deinit(d: *const self) void {
        d.alloc.free(d.data);
    }
};

pub fn loadFile(alloc: std.mem.Allocator, filename: []const u8) !ROM {
    // alloc_ = alloc;
    const file = try std.fs.cwd().openFile(filename, .{ .mode = .read_only });
    defer file.close();
    // var ret: self = .{};
    var fs_contents = try file.readToEndAlloc(alloc, 1000000);
    return ROM{ .data = fs_contents, .alloc = alloc };
}

test "loadFile : basic" {
    var alloc = std.testing.allocator;
    const inst = try loadFile(alloc, "tetris.gb");
    try std.testing.expectEqualSlices(u8, "TETRIS" ++ [1]u8{0} ** 9, inst.name());
    try std.testing.expectEqual(@as(u8, 0x00), inst.data[0x100]); // Tetris's 1st instr is a NO OP
    defer inst.deinit();

}
