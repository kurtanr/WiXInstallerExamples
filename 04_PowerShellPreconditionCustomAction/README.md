# 04_PowerShellPreconditionCustomAction

Continuing from the [previous example](../03_PassingInstallLocationParameterToInstaller/), in this example, as a precondition of installation, we will check if PowerShell is installed.\
Later, a PowerShell script will be executed as part of a [custom action](https://wixtoolset.org/documentation/manual/v3/xsd/wix/customaction.html) during the installation.

## Checking if PowerShell is installed

Following block uses [RegistrySearch](https://wixtoolset.org/documentation/manual/v3/xsd/wix/registrysearch.html) element to read from Windows Registry which version of PowerShell is installed.\
Retrieved value is assigned to a property:
```xml
    <Property Id="POWERSHELLVERSION">
      <RegistrySearch Id="POWERSHELLVERSION"
                      Type="raw"
                      Root="HKLM"
                      Key="SOFTWARE\Microsoft\PowerShell\3\PowerShellEngine"
                      Name="PowerShellVersion" />
    </Property>
```
Aditionally, location of PowerShell.exe is also assigned to a property:
```xml
    <Property Id="POWERSHELLEXE">
      <RegistrySearch Id="POWERSHELLEXE"
                      Type="raw"
                      Root="HKLM"
                      Key="SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell"
                      Name="Path" />
    </Property>
```
Similar code blocks can be found in e.g. [MongoDB](https://github.com/mongodb/mongo/blob/f064d4aee86645a36fe38930fb1f5a75815adacf/src/mongo/installer/msi/wxs/Installer_64.wxs#L178-L197) and [Git Extensions](https://github.com/gitextensions/gitextensions/blob/fde9a6ef8a314a122b8453509a530a16f3955317/Setup/UI/TelemetryDlg.wxs#L6-L12) installers.

## Aborting installation if PowerShell is not installed

When the [Condition](https://wixtoolset.org/documentation/manual/v3/xsd/wix/condition.html) element is placed under Fragment or Product element, it becomes a [LaunchCondition](https://wixtoolset.org/documentation/manual/v3/xsd/wix/launchconditions.html) entry.\
When such conditions are not satisfied, installation immediately aborts and shows a warning dialog to the user.\
LaunchCondition entries are visible in the Orca MSI editor.
```xml
    <Condition Message="You must have PowerShell 3.0 or higher.">
      <![CDATA[Installed OR (POWERSHELLEXE AND POWERSHELLVERSION >= "3.0")]]>
    </Condition>
```
By adding the above-listed condition, if PowerShell is not installed or the installed version is less than [Windows PowerShell 3.0](https://en.wikipedia.org/wiki/PowerShell#Windows_PowerShell_3.0), the installation will immediately abort and show the following warning dialog:

<p align="center">
    <img src="https://raw.githubusercontent.com/kurtanr/WiXInstallerExamples/master/images/PowerShellError.png" alt="PowerShell error" style="max-width:100%;">
</p>

The installation will **abort if the inner text** of the element is **evaluated as false**.\
"[Installed](https://docs.microsoft.com/en-us/windows/win32/msi/installed) OR" part is present in the condition, because we only want to do the check on initial install, and not on upgrade/repair/uninstall (assumption here is that we will be executing PowerShell scripts only during installation, and not during upgrade/repair/uninstall).

If we are doing an initial installation, *Installed* is false, so to proceed with installation PowerShell condition must be true.\
If we are doing an upgrade/repair/uninstall, *Installed* is true, so it does not matter if the PowerShell condition is true or false.\
If we are also executing PowerShell scripts on upgrade/repair/uninstall, the condition can omit the "Installed OR" part.

## Overview of install stages

Before defining a custom action that will use PowerShell.exe to execute a PowerShell script, first a quick explanation of installation stages.

When looking at the installer using Orca MSI editor, tables **InstallUISequence** and **InstallExecuteSequence** can be seen.\
Those two tables represent two stages of the installation. Each stage consists of a set of actions that are executed in the order shown below.

<p align="center">
    <img src="https://raw.githubusercontent.com/kurtanr/WiXInstallerExamples/master/images/InstallUISequence.png" alt="InstallUISequence" style="max-width:100%;">
</p>

<p align="center">
    <img src="https://raw.githubusercontent.com/kurtanr/WiXInstallerExamples/master/images/InstallExecuteSequence.png" alt="InstallExecuteSequence" style="max-width:100%;">
</p>

### InstallUISequence
- it is executed first
- called "client-side" of the installation
- runs under the user account that launched the MSI
- shows UI to the user and does tasks that do not alter file system/registry - things like
  - checking if the previous version of the product is installed (FindRelatedProducts)
  - doing a lookup of entries in e.g. Windows Registry or in Ini files (AppSearch)
  - checking Launch conditions (LaunchConditions)
  - checking how much space is needed for the installation (Cost actions)
- can be entirely skipped by calling MsiExec with parameter [`/quiet`](https://docs.microsoft.com/en-us/windows/win32/msi/standard-installer-command-line-options)

### InstallExecuteSequence
- it is executed after InstallUISequence
- called "server-side" of the installation
- runs under [LocalSystem](https://docs.microsoft.com/en-us/windows/win32/services/localsystem-account) user account
- does tasks that alter file system/registry - things like
  - removing previously installed version of the product
  - modifying file system and Windows Registry
  - adding a new entry in "Program and Features"
- also contains all the actions done as part of InstallUISequence (such as AppSearch, LaunchConditions, CostFinalize...)\
  (this is needed in case InstallUISequence was entirely skipped using /quiet parameter)
- is split into two phases
  - immediate phase
    - includes everything done before **InstallInitialize** action
    - this phase does not have rollback protection
  - deferred phase
    - starts with **InstallInitialize** and ends with **InstallFinalize** action
    - if an error occurs during this phase, installation is rolled back
    - custom actions that modify system state [should be executed as deferred](https://www.advancedinstaller.com/immediate-vs-deferred-custom-action.html)
- a good explanation of differences between immediate and deferred execution of custom actions can be [found here](http://www.msigeek.com/345/windows-installer-faq-part-3)

<p align="center">
    <img src="https://www.msigeek.com/wp-content/uploads/Difference-Between-Execute-Immediate-and-Deferred.png" alt="Immediate-and-Deferred" style="max-width:100%;">
</p>

## Executing PowerShell script as custom action during install

Multiple types of custom actions exist and there are multiple ways of executing them.\
Here we will define a deferred custom action that executes after files have been deployed by the installer.\
This action will call a PowerShell script which will append one line to a deployed text file.

[Here](https://wixtoolset.org/documentation/manual/v3/customactions/qtexec.html) we can find how to define such a custom action (section "Deferred execution").\
First we use the [SetProperty](https://wixtoolset.org/documentation/manual/v3/xsd/wix/setproperty.html) element to define an immediately executed custom action of type 51 which prepares the command line (custom action type can be seen in Orca):
```xml
    <SetProperty Id="CA_AppendTextUsingPowerShell"
                 Before ="CA_AppendTextUsingPowerShell"
                 Sequence="execute"
                 Value='&quot;[POWERSHELLEXE]&quot; -NoLogo -NonInteractive -NoProfile -ExecutionPolicy Bypass -File "[MY_INSTALL_LOCATION]MyAppendScript.ps1" -propertyValue "[MY_PROPERTY]"' />
```
Then we define the deferred custom action (note that Id used in the SetProperty custom action must match the Id value used in the deferred custom action):
```xml
    <CustomAction Id="CA_AppendTextUsingPowerShell"
                  BinaryKey="WixCA"
                  DllEntry="WixQuietExec"
                  Execute="deferred"
                  Return="check"
                  Impersonate="no" />
```
<p align="center">
    <img src="https://raw.githubusercontent.com/kurtanr/WiXInstallerExamples/master/images/CustomActionType51.png" alt="CustomActionType51" style="max-width:100%;">
</p>

The final step is to schedule execution of the deferred custom action after files have been deployed by the installer:
```xml
    <InstallExecuteSequence>
      <Custom Action='CA_AppendTextUsingPowerShell' After='InstallFiles'>NOT Installed AND NOT WIX_UPGRADE_DETECTED</Custom>
    </InstallExecuteSequence> 
```
<p align="center">
    <img src="https://raw.githubusercontent.com/kurtanr/WiXInstallerExamples/master/images/SchedulingCustomAction.png" alt="SchedulingCustomAction" style="max-width:100%;">
</p>

If the installation is executed with logging enabled, we can see from the log file that:
- custom actions are executed during the "server-side" of the installation
- custom actions are executed in the deferred part, between *InstallFiles* and *WriteRegistryValues*
- SetCA_AppendTextUsingPowerShell custom action successfully sets the command line
- CA_AppendTextUsingPowerShell custom action successfully executed.</br>This is represented as [return value 1](https://docs.microsoft.com/en-us/windows/win32/msi/logging-of-action-return-values) in the log file.</br>If custom action fails to execute, usually [value 3](https://robmensching.com/blog/posts/2010/8/2/the-first-thing-i-do-with-an-msi-log/) is returned (and MsiExec error code 1603).
```
MSI (s) (F8:00) [22:09:41:191]: BeginTransaction: Locking Server
...
MSI (s) (F8:00) [22:09:41:203]: Doing action: InstallFiles
...
Action ended 22:09:41: InstallFiles. Return value 1.

MSI (s) (F8:00) [22:09:41:205]: Doing action: SetCA_AppendTextUsingPowerShell
Action start 22:09:41: SetCA_AppendTextUsingPowerShell.
MSI (s) (F8:00) [22:09:41:205]: PROPERTY CHANGE: Adding CA_AppendTextUsingPowerShell property. Its value is '"C:\Windows\SysWOW64\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NonInteractive -NoProfile -ExecutionPolicy Bypass -File "C:\Program Files (x86)\04_PowerShellPreconditionCustomAction\MyAppendScript.ps1" -propertyValue "my value"'.
Action ended 22:09:41: SetCA_AppendTextUsingPowerShell. Return value 1.

MSI (s) (F8:00) [22:09:41:205]: Doing action: CA_AppendTextUsingPowerShell
Action start 22:09:41: CA_AppendTextUsingPowerShell.
Action ended 22:09:41: CA_AppendTextUsingPowerShell. Return value 1.
...
MSI (s) (F8:00) [22:09:41:207]: Doing action: WriteRegistryValues
```