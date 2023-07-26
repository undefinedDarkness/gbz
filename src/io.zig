const std = @import("std");

// alloc: std.mem.Allocator,
buffer: [0xff7f - 0xff00]u8 = undefined,
pub fn init() @This() {
    return .{ };
}

pub fn gamelinkDbg(d: *@This()) void {
    if (d.buffer[0xff02 - 0xff00] != 0) {
        std.log.info("{c}", .{ d.buffer[0xff01 - 0xff00] });
        d.buffer[0xff02 - 0xff00] = 0;
        // std.log.info("%", args: anytype)
    }
}
