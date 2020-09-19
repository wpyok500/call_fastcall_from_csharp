#include "pch.h"
#include "Windows.h"


extern "C" {
		__declspec(dllexport) int __fastcall func_fastcall(int x, int y, int z, int n);
};

int __fastcall func_fastcall(int x, int y, int z, int n) {
	return x*2+y +z*2+ 1+n*10;
}

