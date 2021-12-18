using PZPack.Cli;

// Test.TestFolderTree();
// Test.TestCrypto();
// Test.TestStreamCrypto();
// Test.TestFileEncrypt();
// Test.TestFileDecrypt();
// Test.TestMultiFileEncrypt();
// Test.TestMultiFileDecrypt();

// Test.FullEncodeTest();
// Test.UnpackTest();

Console.WriteLine("PZPack .Net console application");
Console.WriteLine($"PZPack Version = {PZPack.Core.Common.Version}");
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
    default:    break;
}