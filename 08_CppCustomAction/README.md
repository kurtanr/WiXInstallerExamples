# 08_CppCustomAction

## Creating custom action project

Extensions that [integrate WiX Toolset into Visual Studio](https://marketplace.visualstudio.com/publishers/WixToolset) come with a project template for a C++ Custom Action. This is what is used here to create the C++ Custom Action project. In the [previous example](../07_CSharpCustomAction/), we already wrote some C# custom actions that are executed as immediate and as deferred. We will write equivalent actions in C++.

## Writing immediate and deferred custom action

Immediate custom action:
```C++
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
```

Deferred custom action:
```C++
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
```

## Executing installer which contains custom actions

If we would initiate the installation with the following command line:
```
MsiExec /L*V `"installation.log`" /i `"08_InstallerWithMyCppCustomAction.msi`" MY_PROPERTY=`"my value`" MY_INSTALL_LOCATION=`"c:\Example 08`"
```
We would see the following text in *installation.log*:
```
MyImmediateCustomAction:  Initialized.
MyImmediateCustomAction:  MY_PROPERTY value in MyImmediateCustomAction = 'my value'
...
MyDeferredCustomAction:  Initialized.
MyDeferredCustomAction:  CustomActionData value in MyDeferredCustomAction = 'MY_PROPERTY=my value;MY_INSTALL_LOCATION=C:\Example 08\'
```

## Debugging custom actions

See [here](https://stackoverflow.com/a/52880033/15770755) and [here](https://docs.microsoft.com/en-us/windows/win32/msi/debugging-custom-actions) for details on how to debug C++ custom actions.

## How are custom actions scheduled?

Everything except the binary which is references can stay the same like in the [previous example](../07_CSharpCustomAction/):
```xml
    <!-- Reference to 08_MyCppCustomAction.dll -->
    <Binary Id="CustomActions" SourceFile="$(var.08_MyCppCustomAction.TargetDir)$(var.08_MyCppCustomAction.TargetName).dll" />
```
