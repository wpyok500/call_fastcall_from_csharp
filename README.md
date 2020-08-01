# laomms-call_fastcall_from_csharp

https://docs.microsoft.com/zh-cn/archive/blogs/winsdk/c-and-fastcall-how-to-make-them-work-together-without-ccli-shellcode

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

```c#
public static class FastCall
{
    public static IntPtr InvokePtr { get; private set; }

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
        FastCall.InvokePtr = Kernel32.VirtualAlloc(IntPtr.Zero, FastCall.InvokeCode.Length, AllocationType.Commit, MemoryProtection.ExecuteReadWrite);

        Marshal.Copy(FastCall.InvokeCode, 0, FastCall.InvokePtr, FastCall.InvokeCode.Length);
    }

    public static IntPtr WrapStdCallInFastCall(IntPtr stdCallPtr)
    {
        var result = Kernel32.VirtualAlloc(IntPtr.Zero, FastCall.WrapperCode.Length, AllocationType.Commit, MemoryProtection.ExecuteReadWrite);

        Marshal.Copy(FastCall.WrapperCode, 0, result, FastCall.WrapperCode.Length);
        Marshal.WriteIntPtr(result, 5, stdCallPtr);

        return result;
    }
}


// int __fastcall RegisterType(ObjectIdL type, ObjectIdL base, ClassPrototype *prototype)
    private const Int32 REGISTER_TYPE_OFFSET = 0x4716D0;
    private IntPtr RegisterTypePtr;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr RegisterTypeWrapperHookPrototype(ObjectIdL a1, ObjectIdL a2, ClassPrototypePtr a3);
    private IntPtr RegisterTypeHookPtr;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr RegisterTypeWrapperPrototype(IntPtr functionPtr, ObjectIdL a1, ObjectIdL a2, ClassPrototypePtr a3);
    private RegisterTypeWrapperPrototype RegisterTypeWrapper;

    private LocalHook hook;

    public override void OnGameReady()
    {
        this.RegisterTypePtr = Kernel32.GetModuleHandle("game.dll") + REGISTER_TYPE_OFFSET;

        this.RegisterTypeWrapper = Utility.PtrAsFunction<RegisterTypeWrapperPrototype>(FastCall.InvokePtr);
        this.RegisterTypeHookPtr = FastCall.WrapStdCallInFastCall(Utility.FunctionAsPtr(new RegisterTypeWrapperHookPrototype(this.RegisterTypeHook)));

        this.hook = LocalHookEx.CreateUnmanaged(this.RegisterTypePtr, this.RegisterTypeHookPtr, IntPtr.Zero);
        this.hook.ThreadACL.SetInclusiveACL(new[] { 0 });

        base.OnGameReady();
    }

    private IntPtr RegisterTypeHook(ObjectIdL type, ObjectIdL parent, ClassPrototypePtr prototype)
    {
        try
        {
            var result = this.RegisterTypeWrapper(this.RegisterTypePtr, type, parent, prototype);

            //if (type.ToString() == "wscd")
                //Trace.WriteLine("RegisterTypeWrapperHook(" + type + ", " + parent + ", " + prototype.AsIntPtr().ToString("X8") + ")");

            return result;
        }
        catch (Exception e)
        {
            Trace.WriteLine("RegisterTypeWrapperHook: " + e.ToString());
        }
        return IntPtr.Zero;
    }
```
