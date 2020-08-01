# laomms-call_fastcall_from_csharp

```c#
       static class FastCall
    {
        public static T CreateToFastcall<T>(IntPtr functionPtr, string patchName) where T : class
        {
            var method = typeof(T).GetMethod("Invoke");
            if (method.GetParameters().Any(param => Marshal.SizeOf(param.ParameterType) != 4))
                throw new ArgumentException("Only supports functions with 32 bit parameters");

            var parameterCount = method.GetParameters().Length;

            var payload = new List<byte>();

            payload.Add(0x55);                                  // push ebp
            payload.AddRange(new byte[] { 0x89, 0xE5 });        // mov ebp, esp
            payload.AddRange(new byte[] { 0x8B, 0x4D, 0x08 });  // mov ecx, [ebp+0x08]
            payload.AddRange(new byte[] { 0x8B, 0x55, 0x0C });  // mov edx, [ebp+0x0C]

            if (parameterCount > 2)
                for (var i = 0; i < parameterCount - 2; i++)
                {
                    payload.AddRange(new byte[] { 0x8B, 0x5D, (byte)(0x10 + 4*i) });    // mov ebx, [ebp+0x10+4*i]
                    payload.Add(0x53);                                                  // push ebx
                }

            var callOpcodeLocation = payload.Count + 1;

            payload.AddRange(new byte[] { 0xE8, 0x00, 0x00, 0x00, 0x00 });  // call function

            if (parameterCount > 2)
            {
                payload.AddRange(new byte[] {0x89, 0xEC }); // mov esp, ebp
                payload.Add(0x5D);                          // pop ebp
            }

            payload.Add(0xC2);
            payload.AddRange(BitConverter.GetBytes((ushort)(parameterCount * 4)));     // retn 4 * paramCount

            var payloadPtr = Locator.PayloadSpace(payload.Count);

            var functionCall = functionPtr.ToInt32() - payloadPtr.ToInt32() - callOpcodeLocation - 5;

            // update payload
            payload[callOpcodeLocation + 0] = (byte)functionCall;
            payload[callOpcodeLocation + 1] = (byte)(functionCall >> 8);
            payload[callOpcodeLocation + 2] = (byte)(functionCall >> 16);
            payload[callOpcodeLocation + 3] = (byte)(functionCall >> 24);

            // deposit payload
            Patcher.CreatePatch(new Patcher.Patch(payloadPtr, payload.ToArray(), patchName));

            return Utilities.RegisterDelegate<T>(payloadPtr);
        }

        public static void RemoveToFastcall(string patchName)
        {
            Patcher.RemovePatch(patchName);
        }
    }     

```
