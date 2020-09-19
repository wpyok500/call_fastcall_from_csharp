using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CallFastcall
{
    public static class FastCall
    {
        [Flags]
        public enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }
        [Flags]
        public enum MemoryProtection
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAlloc(IntPtr hProcess, uint size,  AllocationType flAllocationType, MemoryProtection flProtect);
        public static IntPtr InvokePtr { get; private set; }
        public static List<IntPtr> FastCallWrappers;

        private static Byte[] InvokeCode = new Byte[]
            {
            0x5A,                           // pop edx
            0x36, 0x87, 0x54, 0x24, 0x08,   // xchg ss:[esp+08],edx
            0x58,                           // pop eax
            0x59,                           // pop ecx
            0xFF, 0xE0                      // jmp eax
            };

        private static Byte[] WrapperCode = new Byte[]
            {
            0x58,                           // pop eax
            0x52,                           // push edx
            0x51,                           // push ecx
            0x50,                           // push eax
            0x68, 0x00, 0x00, 0x00, 0x00,   // push ...
            0xC3                            // retn
            };

        static FastCall()
        {
            InvokePtr = VirtualAlloc(IntPtr.Zero, (uint)InvokeCode.Length, AllocationType.Commit, MemoryProtection.ExecuteReadWrite);
            Marshal.Copy(InvokeCode, 0, InvokePtr, InvokeCode.Length);
        }
        public static IntPtr WrapStdCallInFastCall(IntPtr stdCallPtr)//用于HOOK
        {
            var result = VirtualAlloc(IntPtr.Zero, (uint)WrapperCode.Length, AllocationType.Commit, MemoryProtection.ExecuteReadWrite);
            Marshal.Copy(WrapperCode, 0, result, WrapperCode.Length);
            Marshal.WriteIntPtr(result, 5, stdCallPtr);
            return result;
        }
        public static T StdcallToFastcall<T>(IntPtr functionPtr)
        {

            var wrapper = new List<byte>();

            wrapper.Add(0x58);          // pop eax  - store the return address
            wrapper.Add(0x59);          // pop ecx  - move the 1st argument to ecx
            wrapper.Add(0x5A);          // pop edx  - move the 2nd argument to edx
            wrapper.Add(0x50);          // push eax - restore the return address

            wrapper.Add(0x68);                                                  // push ...
            wrapper.AddRange(BitConverter.GetBytes(functionPtr.ToInt32()));     // the function address to call
            wrapper.Add(0xC3);                                                  // ret - and jump to          

            var wrapperPtr = Marshal.AllocHGlobal(wrapper.Count);
            Marshal.Copy(wrapper.ToArray(), 0, wrapperPtr, wrapper.Count);

            if (FastCallWrappers == null)
                FastCallWrappers = new List<IntPtr>();
            FastCallWrappers.Add(wrapperPtr);
            return (T)(object)Marshal.GetDelegateForFunctionPointer(functionPtr, typeof(T));
        }

        public static void RemoveToFastcall()
        {
            if (FastCallWrappers == null)
                return;
            foreach (var p in FastCallWrappers)
                Marshal.FreeHGlobal(p);
        }

    }
}
