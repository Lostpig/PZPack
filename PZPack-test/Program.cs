using PZPack.Test;

Tests.TestBytesEncodeAndDecode();

var task = Tests.EncodeAndDocodeTest();
task.Wait();

Console.WriteLine("Test complete");