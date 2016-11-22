using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace csharpTriggerbotCsgo
{
    class Program
    {
        static private int triggerKey;
        
        static bool canShoot(windowsAPIs winAPI)
        {
            int playerBase = mem.readInt((int)(client) + 0x00A9053C); //0x00A9053C is the localplayer address in memory
            int inCross = mem.readInt(playerBase + 0x0000AA64); //reads our local playerbase + the crosshair offset. 0x0000AA64 is the crosshairID address in memory
            
            return inCross > 0 && inCross < 64 ? true : false;     
        }
        static void triggerLoop(memoryFunctions mem, IntPtr handle, IntPtr client)
        {
            Console.Beep(100, 400); //started successfully
            
            windowsAPIs winAPI = new windowsAPIs();
             
            while (true)
            {
               if (winAPI.IsKeyPushedDown(triggerKey))
               {
                    if (canShoot) //sees if there is a valid target
                        DoMouseClick();
               }
                Thread.Sleep(1); //brings down CPU usage
                   
            }
        }
        static void Main(string[] args)
        {
            Console.Title = "github.com/rem2beast";
            Console.WriteLine("key list at http://cherrytree.at/misc/vk.htm");
            Console.Write("Please enter a decimal trigger key value: ");

            string input = Console.ReadLine(); //get decimal key value as a string 

            triggerKey = Convert.ToInt16(input); //needs error handling

            Console.WriteLine("Confirm that the game is open. Press enter to start");
            Console.ReadKey(); //waits for enter press

            Console.Clear();
            memoryFunctions mem = new memoryFunctions();
            mem.initialize(); //starts intialize method 
            triggerLoop(mem, mem.getHandle(), mem.getModuleAddress());
        }
    }
    class memoryFunctions
    {
        private IntPtr processHandle; //for the handle to the game so we can read data from csgo, would be HANDLE processHandle in c++
        private IntPtr client; //for the client module inside of the game, would be DWORD in C++

        //have to import these api calls because C#. kernel32.dll and user32.dll refer to their locations
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId); //so we can get access to the game

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] buffer, int size, int lpNumberOfBytesRead); //so we can read data from the game using our handle

        public void initialize()
        {
            Process[] processes = Process.GetProcessesByName("csgo");

            foreach (Process p in processes) //get the csgo process
            {
                processHandle = OpenProcess(0x0010, false, p.Id); //open a handle to the game using the process id with read only privileges(sp)

                foreach (ProcessModule module in p.Modules)
                {
                    if ((module.FileName).IndexOf("client.dll") != -1 && (module.FileName).IndexOf("steamclient.dll") == -1) //checks to make sure that we dont accidentally get the steamclient module address
                    {
                        client = module.BaseAddress; //get the address of the client.dll
                        module.Dispose();
                    }
                }
            }
        }
        public IntPtr getHandle()
        {
            return processHandle;
        }
        public IntPtr getModuleAddress()
        {
            return client;
        }
        private byte[] ReadMem(int pOffset, int pSize) //offset is the adddress to whatever we're trying to read
        {
            byte[] buffer = new byte[pSize];
            ReadProcessMemory((int)processHandle, pOffset, buffer, pSize, 0); //reads from the game our offset into the buffer, a byte array, and returns it
            return buffer;
        }
        public int readInt(int pOffset)
        {
            return BitConverter.ToInt32(ReadMem(pOffset, 4), 0); //converts it to int, 4 is the size that you will need 99% of the time
        }

    }
    class windowsAPIs
    {
        [DllImport("user32.dll")]
        public static extern ushort GetAsyncKeyState(int vKey); //so we can see if our triggerkey is pressed

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo); //so we can simulate a mouse click

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;

        public void DoMouseClick()
        { 
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }
        public bool IsKeyPushedDown(int vKey)
        {
            return 0 != (GetAsyncKeyState(vKey) & 0x8000); //bitwise checks to see if key is pressed down
        }
    }

}
