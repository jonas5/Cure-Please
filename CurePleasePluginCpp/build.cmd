REM cl.exe /LD /std:c++20 /EHsc /D UNICODE /D _UNICODE main.cpp /link /LIBPATH:"../Ashita-v4beta/plugins/sdk/libs/x86" /DEF:CurePleasePluginCpp.def

cl /std:c++20 /EHsc /c main.cpp BitReader.cpp
cl.exe /LD /std:c++20 /EHsc /D UNICODE /D _UNICODE main.obj BitReader.obj /link /LIBPATH:"../Ashita-v4beta/plugins/sdk/libs/x86" /DEF:CurePleasePluginCpp.def /OUT:main.dll /IMPLIB:main.lib


move main.dll ..\bin\Debug\CurePleasePluginCpp.dll