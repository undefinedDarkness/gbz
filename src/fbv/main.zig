const std = @import("std");
const T = @cImport({
    @cInclude("tigr.h");
});
const CPU = @import("../cpu.zig");

pub fn main() !void {
    std.debug.print("HI\n", .{});
    const screen = T.tigrWindow(160, 144, "FBV: GB", T.TIGR_2X);
    var gpa = std.heap.GeneralPurposeAllocator(.{}){};
    var alloc = gpa.allocator();
    _ = alloc;
    defer _ = gpa.deinit();
    const EMU = std.Thread.spawn(.{}, (struct { fn inThread() !void {
        
    } }).inThread, .{});
    _ = EMU;
    defer T.tigrFree(screen);
    while (T.tigrClosed(screen) == 0) {
        T.tigrClear(screen, T.tigrRGB(0x1, 0x1, 0x1));
        defer T.tigrUpdate(screen);
        T.tigrPrint(screen, T.tfont, 0, 0, T.tigrRGB(0xff, 0xff, 0xff), "Hello, world.");
    }  
    std.debug.print("BYE\n", .{});
}
