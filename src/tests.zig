const std = @import("std");
// const rom = @import("rom.zig");

test {
    const rom = @import("rom.zig");
    const dbg = @import("debug.zig");
    const cpu = @import("cpu.zig");
    _ = cpu;
    _ = dbg;
    _ = rom;
}
