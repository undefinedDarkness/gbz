static class PP
{

    public static void Main(string[] args)
    {

        var GB = new GBEmulator("gb-test-roms/cpu_instrs/individual/07-jr,jp,call,ret,rst.gb");
        GB.run();
        Console.WriteLine("dafaq");
    }
}