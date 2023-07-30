class GBEmulator {
    STATE state;
    CPU cpu;
    Debugger dbg;
    public GBEmulator(string rom) {
        state = new STATE(rom);
        cpu = new CPU(state);
        // state.setDebugger(dbg);
        dbg = new Debugger(state, cpu, rom);
    }

    public void run() {
        while (true) {
            dbg.DebugTick(); // here opcode is set
            cpu.Tick();
            dbg.IncrementPC();
        }
    }
}