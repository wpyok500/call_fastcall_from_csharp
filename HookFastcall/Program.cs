
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CallFastcall
{
    class Program
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);
        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);


        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate int func_fastcall(int x, int y, int z, int n);
         
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int WrapperFastcall(IntPtr functionPtr, int x, int y, int z, int n);
        private static WrapperFastcall wrapper_fastcall;
        static IntPtr FuncPtr;
        static void Main(string[] args)
        {
            IntPtr hDll = LoadLibrary("FastcallDll.dll");
            FuncPtr = GetProcAddress(hDll, "@func_fastcall@16");
            wrapper_fastcall = FastCall.StdcallToFastcall<WrapperFastcall>(FastCall.InvokePtr);
            //如果要hook该函数
            IntPtr HookPtr = FastCall.WrapStdCallInFastCall(Marshal.GetFunctionPointerForDelegate(new func_fastcall(HookCallback)));
            Hook HookFunc = new Hook(FuncPtr, HookPtr);
            Hook.Install();
            //直接调用
            int res = wrapper_fastcall(FuncPtr, 1, 2, 3, 4);
            Console.WriteLine(res);            
            FastCall.RemoveToFastcall();
            Console.ReadLine();
        }

        //Hook回调
        private static int HookCallback(int x, int y, int z, int n)
        {
            try
            {
                Hook.Unistall();
                var result = wrapper_fastcall(FuncPtr, 1, 2, 3, 4);
                Console.WriteLine(result);
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return 0;
        }

    }
}
