class GBEmulator {
    STATE state;
    CPU cpu;
    Debugger dbg;
    public GBEmulator(string rom) {
        state = new STATE(rom);
        cpu = new CPU(state);
        dbg = new Debugger(state, cpu);
    }

    public void run() {
        while (true) {
            dbg.DebugTick();
            cpu.Tick();
            dbg.IncrementPC();
        }
    }
}