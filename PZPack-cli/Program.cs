using PZPack.Cli;

Console.WriteLine("PZPack .Net console application");
Console.WriteLine($"PZPack Core Version = {PZPack.Core.Version.Current}");
Console.WriteLine("Please select opearte:");

string[] options = new string[]
{
    "Pack a directory",
    "Unpack a pzpk file",
    "View a pzpk file info",
    "Exit"
};
int selected = ConsoleHelper.ConsoleSelect(options);

switch (selected)
{
    case 0: Operates.Pack(); break;
    case 1: Operates.Unpack(); break;
    case 2: Operates.ViewInfo(); break;
    default: break;
}

Console.WriteLine("PZPack excute complete");
Console.WriteLine("Press any key close");
var _ = Console.ReadKey(true).Key;

Environment.Exit(0);