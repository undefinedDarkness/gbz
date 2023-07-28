static class PP {

public static void Main(string[] args) {

var GB = new GBEmulator("gb-test-roms/cpu_instrs/individual/01-special.gb");
GB.run();
Console.WriteLine("dafaq");
}
}