using PZPack.Test;

var task1 = Tests.TestBytesEncodeAndDecode();
task1.Wait();

var task2 = Tests.EncodeAndDocodeTest();
task2.Wait();

Console.WriteLine("Test complete");