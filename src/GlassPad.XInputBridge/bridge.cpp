// SPDX-License-Identifier: GPL-3.0-or-later
// Game-local compatibility route. No hooks, patching, drivers or global remapping.
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <Xinput.h>
struct Route { DWORD magic, version, slot, enabled; ULONGLONG heartbeat; DWORD pad[2]; volatile LONG polls; };
static HMODULE original;
static HANDLE mapping;
static Route* route;
static INIT_ONCE once=INIT_ONCE_STATIC_INIT;
static BOOL CALLBACK Init(PINIT_ONCE, PVOID, PVOID*) {
    wchar_t path[MAX_PATH];
    if (!GetSystemDirectoryW(path,MAX_PATH-20)) return FALSE;
    lstrcatW(path,L"\\xinput1_3.dll"); original=LoadLibraryW(path); return original!=NULL;
}
static FARPROC Proc(const char* name) { InitOnceExecuteOnce(&once,Init,NULL,NULL); return original?GetProcAddress(original,name):NULL; }
static DWORD Index(DWORD requested) {
    if(!route) { mapping=OpenFileMappingW(FILE_MAP_READ|FILE_MAP_WRITE,FALSE,L"Local\\GlassPadQueens.XInputRoute.v1"); if(mapping)route=(Route*)MapViewOfFile(mapping,FILE_MAP_READ|FILE_MAP_WRITE,0,0,64); }
    if(route && route->magic==0x47505131 && route->version==1 && route->enabled && route->slot<4 && GetTickCount64()-route->heartbeat<250) {
        if(requested==0) { InterlockedIncrement(&route->polls); return route->slot; }
        if(requested==route->slot && route->slot!=0) return 4;
    }
    return requested;
}
extern "C" DWORD WINAPI GPGetState(DWORD u,XINPUT_STATE* s) { auto f=(DWORD(WINAPI*)(DWORD,XINPUT_STATE*))Proc("XInputGetState");return f?f(Index(u),s):ERROR_DEVICE_NOT_CONNECTED; }
extern "C" DWORD WINAPI GPGetStateEx(DWORD u,XINPUT_STATE* s) { auto f=(DWORD(WINAPI*)(DWORD,XINPUT_STATE*))Proc((const char*)100);return f?f(Index(u),s):GPGetState(u,s); }
extern "C" DWORD WINAPI GPSetState(DWORD u,XINPUT_VIBRATION* v) { auto f=(DWORD(WINAPI*)(DWORD,XINPUT_VIBRATION*))Proc("XInputSetState");return f?f(Index(u),v):ERROR_DEVICE_NOT_CONNECTED; }
extern "C" DWORD WINAPI GPGetCapabilities(DWORD u,DWORD flags,XINPUT_CAPABILITIES* c) { auto f=(DWORD(WINAPI*)(DWORD,DWORD,XINPUT_CAPABILITIES*))Proc("XInputGetCapabilities");return f?f(Index(u),flags,c):ERROR_DEVICE_NOT_CONNECTED; }
extern "C" void WINAPI GPEnable(BOOL enabled) { auto f=(void(WINAPI*)(BOOL))Proc("XInputEnable");if(f)f(enabled); }
extern "C" DWORD WINAPI GPGetBattery(DWORD u,BYTE type,XINPUT_BATTERY_INFORMATION* b) { auto f=(DWORD(WINAPI*)(DWORD,BYTE,XINPUT_BATTERY_INFORMATION*))Proc("XInputGetBatteryInformation");return f?f(Index(u),type,b):ERROR_DEVICE_NOT_CONNECTED; }
extern "C" DWORD WINAPI GPGetKeystroke(DWORD u,DWORD flags,XINPUT_KEYSTROKE* k) { auto f=(DWORD(WINAPI*)(DWORD,DWORD,XINPUT_KEYSTROKE*))Proc("XInputGetKeystroke");auto result=f?f(Index(u),flags,k):ERROR_DEVICE_NOT_CONNECTED;if(result==0&&k&&u==0)k->UserIndex=0;return result; }
extern "C" DWORD WINAPI GPGetDSound(DWORD u,GUID* render,GUID* capture) { auto f=(DWORD(WINAPI*)(DWORD,GUID*,GUID*))Proc("XInputGetDSoundAudioDeviceGuids");return f?f(Index(u),render,capture):ERROR_DEVICE_NOT_CONNECTED; }
extern "C" DWORD WINAPI GPWaitGuide(DWORD u,DWORD flags,void* value) { auto f=(DWORD(WINAPI*)(DWORD,DWORD,void*))Proc((const char*)101);return f?f(Index(u),flags,value):ERROR_DEVICE_NOT_CONNECTED; }
extern "C" DWORD WINAPI GPCancelGuide(DWORD u) { auto f=(DWORD(WINAPI*)(DWORD))Proc((const char*)102);return f?f(Index(u)):ERROR_DEVICE_NOT_CONNECTED; }
extern "C" DWORD WINAPI GPPowerOff(DWORD u) { auto f=(DWORD(WINAPI*)(DWORD))Proc((const char*)103);return f?f(Index(u)):ERROR_DEVICE_NOT_CONNECTED; }
BOOL WINAPI DllMain(HINSTANCE, DWORD reason, LPVOID) { if(reason==DLL_PROCESS_DETACH) { if(route)UnmapViewOfFile(route);if(mapping)CloseHandle(mapping); }return TRUE; }
