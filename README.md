# laomms-call_fastcall_from_csharp

总所周知，.net是无法直接调用C++的fastcall的，但是可以通过变通的方法实现，一种是在目标进程空间申请一块内存，构建一个与fastcall参数一样的stdcall，复制代码到该空间，然后通过调用该标准函数间接调用fastcall，stdcall主要的处理过程是将参数1和参数2赋值到ECX和EDX，然后再跳到fascall。另一种方法是暴力内存注入，调用fastcall时注入赋值ECX和EDX的值的代码。
还有一种情况是当fastcall的第二个参数（就是压入EDX的参数）可以是空值时，就可以用thiscall代替fastcall，thiscall的第一个参数也是压入ECX，第二个参数反正为0无所谓，当然，调用时参数数要比fastcall少一个（x86的情况下）。
还有最简单的方法是用c++/clr写个调用fastcall的dll供.net导入调用。


https://docs.microsoft.com/zh-cn/archive/blogs/winsdk/c-and-fastcall-how-to-make-them-work-together-without-ccli-shellcode

与标准函数的区别
```c
.text:10011618                 push    5               ; int
.text:1001161A                 push    4               ; int
.text:1001161C                 push    3               ; int
.text:1001161E                 push    2               ; int
.text:10011620                 push    1               ; int
.text:10011622                 call    cppstdcall(int,int,int,int,int)
.text:10011627                 mov     [ebp+var_8], eax

.text:1001162A                 push    5               ; int
.text:1001162C                 push    4               ; int
.text:1001162E                 push    3               ; int
.text:10011630                 mov     edx, 2          ; int
.text:10011635                 mov     ecx, 1          ; int
.text:1001163A                 call    cppfastcall(int,int,int,int,int)
.text:1001163F                 mov     [ebp+var_14], eax
```

通过构建一个标准函数去调用fastcall
```c
.text:100116F0                                                 int __stdcall cppstdcall(int, int, int, int, int) proc near
.text:100116F0                                                                                         ; CODE XREF: cppstdcall(int,int,int,int,int)↑j
.text:100116F0
.text:100116F0                                                 var_C0          = byte ptr -0C0h
.text:100116F0                                                 arg_0           = dword ptr  8
.text:100116F0                                                 arg_4           = dword ptr  0Ch
.text:100116F0                                                 arg_8           = dword ptr  10h
.text:100116F0                                                 arg_C           = dword ptr  14h
.text:100116F0                                                 arg_10          = dword ptr  18h
.text:100116F0
.text:100116F0 55                                                              push    ebp
.text:100116F1 8B EC                                                           mov     ebp, esp
.text:100116F3 81 EC C0 00 00 00                                               sub     esp, 0C0h
.text:100116F9 53                                                              push    ebx
.text:100116FA 56                                                              push    esi
.text:100116FB 57                                                              push    edi

.text:10011718 8B 45 18                                                        mov     eax, [ebp+arg_10]
.text:1001171B 50                                                              push    eax             ; int
.text:1001171C 8B 4D 14                                                        mov     ecx, [ebp+arg_C]
.text:1001171F 51                                                              push    ecx             ; int
.text:10011720 8B 55 10                                                        mov     edx, [ebp+arg_8]
.text:10011723 52                                                              push    edx             ; int
.text:10011724 8B 55 0C                                                        mov     edx, [ebp+arg_4] ; int
.text:10011727 8B 4D 08                                                        mov     ecx, [ebp+arg_0] ; int
.text:1001172A E8 C0 FA FF FF                                                  call    cppfastcall(int,int,int,int,int)

.text:1001172F 5F                                                              pop     edi
.text:10011730 5E                                                              pop     esi
.text:10011731 5B                                                              pop     ebx
.text:10011732 81 C4 C0 00 00 00                                               add     esp, 0C0h
.text:10011738 3B EC                                                           cmp     ebp, esp
.text:1001173F 8B E5                                                           mov     esp, ebp
.text:10011741 5D                                                              pop     ebp
.text:10011742 C2 14 00                                                        retn    14h
.text:10011742                                                 int __stdcall cppstdcall(int, int, int, int, int) endp
```

```c#
            byte[] pFuncAddr = BitConverter.GetBytes(cppfastcallPtr.ToInt32());
            byte[] patchcode1 = new byte[]
            {
                     0x55,//                                            push    ebp
                     0x8B, 0xEC,//                                      mov     ebp, esp
                     0x81 , 0xEC , 0xC0 , 0x00 , 0x00 , 0x00,//         sub     esp, 0C0h
                     0x53,//                                            push    ebx
                     0x56,//                                            push    esi
                     0x57,//                                            push    edi
                     0x8D , 0xBD , 0x40 , 0xFF , 0xFF , 0xFF,//         lea     edi, [ebp + var_C0]
                     0x8B , 0x45 , 0x18,//                              mov eax, [ebp + arg_10]
                     0x50,//                                            push eax; int
                     0x8B , 0x4D , 0x14,//                              mov ecx, [ebp + arg_C]
                     0x51,//                                            push ecx; int
                     0x8B , 0x55 , 0x10,//                              mov edx, [ebp + arg_8]
                     0x52,//                                            push edx; int
                     0x8B , 0x55 , 0x0C,//                              mov     edx, [ebp+arg_4] ; int
                     0x8B , 0x4D , 0x08,//                              mov ecx, [ebp + arg_0]; int
                     0xE8 //                                            call    cppfastcall//10011710
            };
            byte[] patchcode2 = new byte[]
            {
                     0x5F,//                                            pop edi
                     0x5E,//                                            pop esi
                     0x5B,//                                            pop ebx
                     0x81 , 0xC4 , 0xC0 , 0x00 , 0x00 , 0x00,//         add esp, 0C0h
                     0x3B ,0xEC,//                                      cmp ebp, esp
                     0x8B ,0xE5,//                                      mov esp, ebp
                     0x5D,//                                            pop ebp
                     0xC3//                                             retn
             };

            byte[] patch_bytes = patchcode1.Concat(pFuncAddr).Concat(patchcode2).ToArray();
            
            //修改内存属性
		VirtualProtect(cppfastcallPtr, 1, PAGE_EXECUTE_READWRITE, ref lpflOldProtect))

            //读相同长度的原始字节
                        IntPtr OldEntry = Marshal.AllocHGlobal(patch_bytes.Length); 
		for (int i = 0; i <= patch_bytes.Length; i++)
		{

			Marshal.WriteByte(OldEntry, i, Marshal.ReadByte(cppfastcallPtr, i));

		}

            //写入自定义代码区
            WriteProcessMemory(Hwnd, cppfastcallPtr, patch_bytes, (uint)patch_bytes.Length, IntPtr.Zero);
            
```


网上的一些例子

```c#

   [UnmanagedFunctionPointer(CallingConvention.StdCall)]
   public delegate bool FastCallDelegate(int a1,int a2,int a3,int a4,int a5);
   
   
   FastCallDelegate pFastCall = Fastcall.StdcallToFastcall<FastCallDelegate>((IntPtr)0x006E3D60);
   
   
    static class FastCall
    {
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
