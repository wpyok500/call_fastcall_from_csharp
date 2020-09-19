# c# 中调用fastcall

众所周知，X86下,.net是无法直接调用C++的fastcall的，但是可以通过变通的方法实现，一种是在目标进程空间申请一块内存，构建一个与fastcall参数一样的stdcall，复制代码到该空间，然后通过调用该标准函数间接调用fastcall，stdcall主要的处理过程是将参数1和参数2赋值到ECX和EDX，然后再跳到fascall。另一种方法是暴力内存注入，调用fastcall时注入赋值ECX和EDX的值的代码。
还有一种情况是当fastcall的第二个参数（就是压入EDX的参数）可以是空值时，就可以用thiscall代替fastcall，thiscall的第一个参数也是压入ECX，第二个参数反正为0无所谓，当然，调用时参数数要比fastcall少一个（x86的情况下）。
还有最简单的方法是用c++/clr写个调用fastcall的dll供.net导入调用。    

实例是通过伪造一个标准委托函数，将fastcall指针作为参数加载实现，具体看代码。

