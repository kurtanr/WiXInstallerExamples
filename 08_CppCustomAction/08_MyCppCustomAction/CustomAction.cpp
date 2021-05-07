#include "stdafx.h"

/// <summary>
/// Immediate custom action accesses properties directly using WcaGetProperty.
/// </summary>
UINT __stdcall MyImmediateCustomAction(MSIHANDLE hInstall)
{
  HRESULT hr = S_OK;
  UINT er = ERROR_SUCCESS;

  hr = WcaInitialize(hInstall, "MyImmediateCustomAction");
  ExitOnFailure(hr, "Failed to initialize");

  WcaLog(LOGMSG_STANDARD, "Initialized.");

  LPWSTR myProperty = NULL;
  hr = WcaGetProperty(L"MY_PROPERTY", &myProperty);
  ExitOnFailure(hr, "Failure reading MY_PROPERTY");

  WcaLog(LOGMSG_STANDARD, "MY_PROPERTY value in MyImmediateCustomAction = '%ls'", (LPCWSTR)myProperty);

LExit:
  er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
  return WcaFinalize(er);
}

/// <summary>
/// Deferred custom action accesses properties using CustomActionData.
/// </summary>
UINT __stdcall MyDeferredCustomAction(MSIHANDLE hInstall)
{
  HRESULT hr = S_OK;
  UINT er = ERROR_SUCCESS;

  hr = WcaInitialize(hInstall, "MyDeferredCustomAction");
  ExitOnFailure(hr, "Failed to initialize");

  WcaLog(LOGMSG_STANDARD, "Initialized.");

  LPWSTR customActionData = NULL;
  hr = WcaGetProperty(L"CustomActionData", &customActionData);
  ExitOnFailure(hr, "Failure reading CustomActionData");

  WcaLog(LOGMSG_STANDARD, "CustomActionData value in MyDeferredCustomAction = '%ls'", (LPCWSTR)customActionData);

LExit:
  er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
  return WcaFinalize(er);
}


// DllMain - Initialize and cleanup WiX custom action utils.
extern "C" BOOL WINAPI DllMain(
  __in HINSTANCE hInst,
  __in ULONG ulReason,
  __in LPVOID
)
{
  switch (ulReason)
  {
  case DLL_PROCESS_ATTACH:
    WcaGlobalInitialize(hInst);
    break;

  case DLL_PROCESS_DETACH:
    WcaGlobalFinalize();
    break;
  }

  return TRUE;
}
