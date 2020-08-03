#include "pch.h"

__declspec(dllexport) int __fastcall cppfastcall(int a, int b, int c, int d, int e);
__declspec(dllexport) int __stdcall cppstdcall(int a, int b, int c, int d, int e);


int __fastcall cppfastcall(int a, int b, int c, int d, int e)
{
    return (a + b) * c + d+e;
}

int __stdcall cppstdcall(int a, int b, int c, int d, int e)
{
    return cppfastcall(a, b, c, d, e);
}


BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}
