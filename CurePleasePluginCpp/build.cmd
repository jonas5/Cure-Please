REM cl.exe /LD /std:c++20 /EHsc /D UNICODE /D _UNICODE main.cpp /link /LIBPATH:"../Ashita-v4beta/plugins/sdk/libs/x86" /DEF:CurePleasePluginCpp.def

cl /std:c++20 /EHsc /c main.cpp BitReader.cpp Pipe.cpp debuffhandler.cpp
cl.exe /LD /std:c++20 /EHsc /D UNICODE /D _UNICODE main.obj BitReader.obj Pipe.obj debuffhandler.obj ^
    /link /LIBPATH:"../Ashita-v4beta/plugins/sdk/libs/x86" ^
    /DEF:CurePleasePluginCpp.def /OUT:main.dll /IMPLIB:main.lib

REM move main.dll ..\bin\Debug\m.dll
move main.dll C:\games\HorizonXI\Game\plugins\m.dll
