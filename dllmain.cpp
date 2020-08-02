// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"

__declspec(dllexport) int __cdecl cppcdecl(int a, int b, int c, int d);
__declspec(dllexport) int __fastcall cppfastcall(int a, int b, int c, int d);
__declspec(dllexport) int __stdcall cppstdcall(int a, int b, int c, int d);
__declspec(dllexport) void __stdcall callfunc(int a, int b, int c, int d);



int __cdecl cppcdecl(int a, int b, int c, int d)
{
    return cppfastcall(1, 2, 3, 4);
}

int __fastcall cppfastcall(int a, int b, int c, int d)
{
    return (a + b) * c + d;
}

int __stdcall cppstdcall(int a, int b, int c, int d)
{
    return (a + b) * c + d;
}

void __stdcall callfunc(int a, int b, int c, int d)
{
    int a1 = cppstdcall(1, 2, 3, 4);
    int a2 = cppfastcall(1, 2, 3, 4);
    int a3 = cppstdcall(1, 2, 3, 4);

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

