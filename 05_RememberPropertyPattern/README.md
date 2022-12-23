# 05_RememberPropertyPattern

If the user enters values in the install UI or passes them on the command-line, those values will not be present during repair, upgrade nor uninstall. This issue can be fixed by saving initial property values in Windows Registry - steps to do that are described in [this article](https://robmensching.com/blog/posts/2010/5/2/the-wix-toolsets-remember-property-pattern/).\
This example contains an implementation of the described "Remember Property" pattern.

## The Simple Solution

Continuing from the [previous example](../04_PowerShellPreconditionCustomAction/), we already have a property called MY_PROPERTY and we are already saving its value in the registry.\
Next, reading of that value using [RegistrySearch](https://wixtoolset.org/documentation/manual/v3/xsd/wix/registrysearch.html) will be added:
```xml
    <Property Id="MY_PROPERTY" Value="default value">
      <RegistrySearch Id='RegSearch_MY_PROPERTY'
                      Root='HKLM'
                      Key='Software\[Manufacturer]\[ProductName]'
                      Name='My property'
                      Type='raw' />
    </Property>
```
This fixes the problem with the value not being available at upgrade/repair/uninstall.

E.g. if we install the MSI using:
```
MsiExec /L*V `"installation.log`" /i `"05_RememberPropertyPattern.msi`" MY_PROPERTY=`"my value`" MY_INSTALL_LOCATION=`"c:\Example 05`"
```
And then repair it using:
```
MsiExec /L*V `"repair.log`" /fu `"05_RememberPropertyPattern.msi`"
```
We would see the following lines in the *repair.log*:
```
MSI (s) (F0:34) [19:00:21:599]: PROPERTY CHANGE: Modifying MY_PROPERTY property. Its current value is 'default value'. Its new value: 'my value'.
Property(S): MY_PROPERTY = my value
```
Without the RegistrySearch part, *repair.log* would just contain a line saying: `Property(S): MY_PROPERTY = default value`

## Defining custom actions that execute only during install/upgrade/repair/uninstall

Initiating install/upgrade/repair/uninstall using MsiExec and then going through the log file to see if a property was set or not is kind of tedious. One way to make checking if remembering property values works correctly during install/upgrade/repair/uninstall is to create a set of custom actions that execute only during one specific part of the installation process (e.g. only during upgrade or only during uninstall). Such an action can then write values of all properties that it receives into a file.

The first problem that has to be solved when defining such a set of actions is how to define conditions that are true only during install/upgrade/repair/uninstall. Such a set of conditions is defined in [InstallationStages.wxi](InstallationStages.wxi) (originally taken from [here](https://gist.github.com/dander/8408382)).\
In the file, we define a set of custom properties that use a combination of predefined WiX/Windows Installer properties.

| Custom property name | True only during                      | WiX/Windows Installer condition |
|----------------------|---------------------------------------|---------------------------------|
| FirstInstall         | Initial installation                  | NOT Installed AND NOT WIX_UPGRADE_DETECTED AND NOT WIX_DOWNGRADE_DETECTED |
| Upgrading            | Upgrade                               | WIX_UPGRADE_DETECTED AND NOT (REMOVE="ALL") |
| RemovingForUpgrade   | Removal of old version during upgrade | (REMOVE="ALL") AND UPGRADINGPRODUCTCODE |
| Uninstalling         | Uninstallation                        | Installed AND (REMOVE="ALL") AND NOT (WIX_UPGRADE_DETECTED OR UPGRADINGPRODUCTCODE) |
| Maintenance          | Repair and e.g. MSI re-caching        | Installed AND NOT Upgrading AND NOT Uninstalling AND NOT UPGRADINGPRODUCTCODE |

Setting of those properties is scheduled after the *FindRelatedProducts* stage, which is at the beginning of the installation (see [here](../04_PowerShellPreconditionCustomAction#overview-of-install-stages) for a list of stages).

Next, we need to define and schedule custom actions.\
Every custom action will do the same thing - call [MyAppendScript.ps1](MyAppendScript.ps1) which will log property values in the *InstallationStages.txt* file.\
This file is created after installation, so uninstallation will not remove it.
Scheduling of actions is done during the InstallExecuteSequence:
```xml
    <InstallExecuteSequence>
      <Custom Action='CA_AppendTextUsingPowerShell_FirstInstall' Before='InstallFinalize'>FirstInstall</Custom>
      <Custom Action='CA_AppendTextUsingPowerShell_Upgrading' Before='InstallFinalize'>Upgrading</Custom>
      <Custom Action='CA_AppendTextUsingPowerShell_Uninstalling' Before='RemoveFiles'>Uninstalling</Custom>
      <Custom Action='CA_AppendTextUsingPowerShell_Maintenance' Before='InstallFinalize'>Maintenance</Custom>
    </InstallExecuteSequence>
```
NOTE: There is no action scheduled related to the *RemovingForUpgrade* stage because the way that scheduling should be done depends on the value of [MajorUpgrade](https://wixtoolset.org/documentation/manual/v3/xsd/wix/majorupgrade.html).Schedule attribute.

All the actions except *Uninstalling* are scheduled to execute before the *InstallFinalize* stage (end of the installation, after *MyAppendScript.ps1* is deployed). If they would be scheduled before the *InstallFiles* stage, FirstInstall would fail in custom action because there would be no script to execute.

*Uninstalling* is scheduled to execute before *RemoveFiles* stage because at that time the *MyAppendScript.ps1* is still present.\
If e.g. *CA_AppendTextUsingPowerShell_Uninstalling* was scheduled before *InstallFinalize* the following error would happen during uninstall:
```
WixQuietExec:  The argument 'C:\Program Files (x86)\05_RememberPropertyPattern\MyAppendScript.ps1' to the -File parameter does not exist. Provide the path to an existing '.ps1' file as an argument to the -File parameter.
...
CustomAction CA_AppendTextUsingPowerShell_Uninstalling returned actual error code 1603 (note this may not be 100% accurate if translation happened inside sandbox)
MSI (s) (B8:68) [09:16:35:432]: Note: 1: 2265 2:  3: -2147287035 
MSI (s) (B8:68) [09:16:35:432]: User policy value 'DisableRollback' is 0
MSI (s) (B8:68) [09:16:35:432]: Machine policy value 'DisableRollback' is 0
Action ended 9:16:35: InstallFinalize. Return value 3.
```
This would leave us with an MSI that cannot be uninstalled. This situation can be fixed by:
- manually fixing the issue in the MSI using Orca
- re-caching the MSI: `MsiExec /L*V recache.log /fv original_msi_with_fixes.msi`
- doing the uninstall with the fixed MSI

## Executing install/upgrade/repair/uninstall with previously defined custom actions

If we build version 1.0.0.0 and version 2.0.0.0 of our MSI (and name them v1.msi and v2.msi), we can do the following set of actions:
- install v1.msi with custom arguments:\
  `MsiExec /L*V FirstInstall.log /i v1.msi MY_PROPERTY=my_value MY_INSTALL_LOCATION=c:\Example_05`
- right-click on v1.msi and choose 'Repair'
- right-click on v2.msi and choose 'Install' (this is actually doing upgrade 1.0.0.0 -> 2.0.0.0)
- right-click on v2.msi and choose 'Repair'
- right-click on v2.msi and choose 'Uninstall'

We would end up with the following text in *c:\Example_05\InstallationStages.txt*:
```
10:50:30.453 - C:\Example_05\ - 1.0.0.0 - my_value - FirstInstall
10:50:41.267 - C:\Example_05\ - 1.0.0.0 - my_value - Maintenance
10:50:48.627 - C:\Example_05\ - 2.0.0.0 - my_value - Upgrading
10:50:54.014 - C:\Example_05\ - 2.0.0.0 - my_value - Maintenance
10:50:59.175 - C:\Example_05\ - 2.0.0.0 - my_value - Uninstalling
```
This proves that property values entered during initial installation are correctly remembered and used during upgrade/repair/uninstall.

## Remaining problem - what if arguments are modified during the upgrade?

If in step 3, instead of right-clicking on v2.msi and choosing 'Install', we call v2.msi using:\
`MsiExec /L*V Upgrade.log /i v2.msi MY_PROPERTY=my_value_2`

We would end up with exactly the same content in *c:\Example_05\InstallationStages.txt* - meaning `MY_PROPERTY=my_value_2` is ignored.\
This is visible in the *Upgrade.log*:
```
MSI (c) (C8:EC) [11:00:10:192]: PROPERTY CHANGE: Modifying MY_PROPERTY property. Its current value is 'default value'. Its new value: 'my_value_2'.
...
MSI (c) (C8:EC) [11:00:10:208]: PROPERTY CHANGE: Modifying MY_PROPERTY property. Its current value is 'my_value_2'. Its new value: 'my_value'.
...
Property(N): MY_PROPERTY = my_value
```
The current simple solution for remembering property values (which has this problem) might be enough for most installers.\
If not, the [next example](../06_RememberPropertyPatternComplete/) fixes this issue with a full implementation of the "Remember Property" pattern.