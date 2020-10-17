using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.VisualBasic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MemRead {
    class Program {
        const int PROCESS_ALL_ACCESS = 0x1F0FFF;

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, long lpBaseAddress, byte[] lpBuffer, long dwSize, out long lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern Int32 CloseHandle(IntPtr hProcess);

        static string duck = "duckstation-sdl-x64-ReleaseLTCG";

        static void Main() {
            Process process = Process.GetProcessesByName(duck)[0];
            IntPtr startOffset = process.MainModule.BaseAddress;
            long start = startOffset.ToInt64();
            IntPtr endOffset = IntPtr.Add(startOffset, process.MainModule.ModuleMemorySize);
            long end = endOffset.ToInt64();
            Console.WriteLine(start.ToString("X2"));

            IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);

            //Console.WriteLine(ReadMem(processHandle, 0xB125F4));

            //WriteMem(processHandle, 0xB125F0, 0);

            var watch = System.Diagnostics.Stopwatch.StartNew();
            var results = AoBScan(processHandle, start, end, "00 01 02 03 04");
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            foreach(long result in results) {
                Console.WriteLine(result.ToString("X2"));
            }
            Console.WriteLine($"Executed in {elapsedMs} ms.");

            CloseHandle(processHandle);
        }

        public static Int16 ReadMem(IntPtr processHandle, long address) {
            byte[] buffer = new byte[2];

            ReadProcessMemory(processHandle, address, buffer, buffer.Length, out long bytesRead);

            return BitConverter.ToInt16(buffer);
        }

        public static byte ReadByte(IntPtr processHandle, long address) {
            byte[] buffer = new byte[1];
            ReadProcessMemory(processHandle, address, buffer, 1, out long bytesRead);
            return buffer[0];
        }

        public static void WriteMem(IntPtr processHandle, int address, short v) {
            var val = BitConverter.GetBytes(v);
            WriteProcessMemory(processHandle, new IntPtr(address), val, val.Length, out int error);
        }

        public static byte[] ReadAoB(IntPtr processHandle, long startAddr, long endAddr) {
            long len = (long) (endAddr - startAddr);
            byte[] buffer = new byte[len];

            ReadProcessMemory(processHandle, startAddr, buffer, len, out long bytesRead);

            return buffer;
        }

        public static List<long> AoBScan(IntPtr processHandle, long startAddr, long endAddr, string mask) {
            List<long> results = new List<long>();
            string[] maskStr = mask.Split(' ');
            byte[] pattern = new byte[maskStr.Length];
            byte[] maskArr = new byte[maskStr.Length];
            int i = 0;
            foreach (string element in maskStr) {
                if (element.Contains('?')) {
                    if (element == "??") {
                        maskArr[i] = 0xFF;
                    } else {
                        char[] toPattern = element.ToCharArray();
                        if (element[0] == '?') {
                            toPattern[0] = '0';
                            maskArr[i] = 0x0F;
                        } else {
                            toPattern[1] = '0';
                            maskArr[i] = 0xF0;
                        }
                        string str = new string(toPattern);
                        pattern[i] = Convert.ToByte(str, 16);

                    }
                } else {
                    pattern[i] = Convert.ToByte(element, 16);
                    maskArr[i] = 0x00;
                }
                i++;
            }
            int iter = 3000000;

            var tasks = new List<Task>();

            for (int chunk = 0; chunk < 1 + (endAddr - startAddr) / iter; chunk++) {
                /*
                byte[] data = ReadAoB(processHandle, Math.Max(startAddr, startAddr + chunk * iter - maskStr.Length), Math.Min(endAddr, startAddr + iter + chunk * iter));
                foreach (var position in data.Locate(pattern, maskArr)) {
                    results.Add(position + chunk * iter);
                }
                */
                
                long start = Math.Max(startAddr, startAddr + chunk * iter - maskStr.Length);
                long end = Math.Min(endAddr, startAddr + iter + chunk * iter);
                int chunk2 = chunk;
                
                
                
                Task t = new Task(() => {
                    byte[] data = ReadAoB(processHandle, start, end);
                    foreach (var position in data.Locate(pattern, maskArr)) {
                        results.Add(position + start);
                    }
                });
                tasks.Add(t);
                
                
                
            }
            
            foreach (Task t in tasks) {
                t.Start();
            }
            Task.WaitAll(tasks.ToArray());
            
            return results;
            
        }

        public static void MatchMask(int index, byte[] array, List<int> resultList, string[] maskStr) {
            bool check = true;

            for (int i = 0; i < maskStr.Length; i++) {
                if (array[i] != (byte)Convert.ToInt16(maskStr[i], 16)) {
                    check = false;
                    break;
                }
            }
            if (check) {
                resultList.Add(index);
            }

        }

        public static void Test(IntPtr processHandle, long startAddr, long endAddr, byte[] pattern, byte[] maskArr, int chunk, int iter, List<int> results) {
            byte[] data = ReadAoB(processHandle, startAddr, endAddr);
            foreach (var position in data.Locate(pattern, maskArr)) {
                results.Add(position + chunk * iter);
            }
        }
    }
}
