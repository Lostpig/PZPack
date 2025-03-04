using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PZPack.View.Service
{
    internal static class SingleInstance
    {
        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr FindWindow(IntPtr ZeroOnly, string lpWindowName);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);

        const int _port = 57894;
        private static Mutex _mutex;

        public static void EnsureSingleAppInstance(string uniqueId, string? startupArg)
        {
            if (null != _mutex) { throw new InvalidOperationException("_mutex is already create"); }
            _mutex = new Mutex(initiallyOwned: true, name: uniqueId);

            if (!_mutex.WaitOne(timeout: TimeSpan.Zero, exitContext: true))
            {
                Alert.ShowMessage(uniqueId + " is already running");
                if (startupArg != null) SendStartupArgs(startupArg);
                AvtiveRunningInstance();
                Environment.Exit(0);
            }
            else
            {
                Thread listenerThread = new Thread(ListenForNewStartup);
                listenerThread.SetApartmentState(ApartmentState.STA);
                listenerThread.IsBackground = true;
                listenerThread.Start();
            }
        }

        public static void ReleaseSingleInstanceMutex()
        {
            if (_mutex == null) { throw new InvalidOperationException("_mutex is not created"); }
            _mutex.Dispose();
        }

        private static void AvtiveRunningInstance()
        {
            Process current = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(current.ProcessName);

            foreach (Process process in processes)
            {
                if (process.Id != current.Id)
                {
                    SetForegroundWindow(process.MainWindowHandle);
                    break;
                }
            }
        }

        private static void SendStartupArgs(string args)
        {
            try
            {
                using TcpClient sender = new TcpClient("localhost", _port);
                using NetworkStream stream = sender.GetStream();

                Byte[] data = System.Text.Encoding.UTF8.GetBytes(args);
                stream.Write(data, 0, data.Length);

                Debug.WriteLine("Sent startup args: {0}", args);
            }
            catch (ArgumentNullException e)
            {
                Debug.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Debug.WriteLine("SocketException: {0}", e);
            }
        }

        private static void ListenForNewStartup()
        {
            var ipAddress = Dns.GetHostEntry("localhost").AddressList[0];
            TcpListener listener = new TcpListener(ipAddress, _port);
            listener.Start();

            while (true)
            {
                if (listener.Pending())
                {
                    var client = listener.AcceptTcpClient();
                    using NetworkStream ns = client.GetStream();
                    using StreamReader sr = new StreamReader(ns);
                    string msg = sr.ReadToEnd();

                    Debug.WriteLine($"Received new message ({msg.Length} bytes):\n{msg}");

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StartupWith.ApplyStartArg(msg);
                    });
                }
                else {
                    Thread.Sleep(100);
                }
            }
        }
    }
}
