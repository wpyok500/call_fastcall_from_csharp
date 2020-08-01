# laomms-call_fastcall_from_csharp

```c#
    static class FastCall
    {
        public static T StdcallToFastcall<T>(IntPtr functionPtr) where T : class
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
            return Marshal.GetDelegateForFunctionPointer<T>(wrapperPtr);
        }

        public static void RemoveToFastcall(string patchName)
        {
            Patcher.RemovePatch(patchName);
        }
    }     

```
