# 06_RememberPropertyPatternComplete

To fix the case not covered in the [previous example](../05_RememberPropertyPattern/), we need to add two custom actions (per property) to save the property value if it is set from the command line. Both actions should be immediate actions:
- one should be scheduled before the *AppSearch* stage and save property passed through the command line into a temporary property
- the other should be scheduled after *AppSearch* and restore property value from a temporary property (only if the property was passed on the command line)
```xml
    <CustomAction Id='SaveCmdLineValue_MY_PROPERTY' Property='CMDLINE_MY_PROPERTY' Value='[MY_PROPERTY]' Execute='firstSequence' />
    <CustomAction Id='SetFromCmdLineValue_MY_PROPERTY' Property='MY_PROPERTY' Value='[CMDLINE_MY_PROPERTY]' Execute='firstSequence' />
    <InstallUISequence>
      <Custom Action='SaveCmdLineValue_MY_PROPERTY' Before='AppSearch' />
      <Custom Action='SetFromCmdLineValue_MY_PROPERTY' After='AppSearch'>
        <![CDATA[CMDLINE_MY_PROPERTY AND (CMDLINE_MY_PROPERTY <> "default value")]]>
      </Custom>
    </InstallUISequence>
    <InstallExecuteSequence>
      <Custom Action='SaveCmdLineValue_MY_PROPERTY' Before='AppSearch' />
      <Custom Action='SetFromCmdLineValue_MY_PROPERTY' After='AppSearch'>
        <![CDATA[CMDLINE_MY_PROPERTY AND (CMDLINE_MY_PROPERTY <> "default value")]]>
      </Custom>
    </InstallExecuteSequence>
```
NOTE: If the property does not have a default value, the `AND (CMDLINE_MY_PROPERTY <> "default value")` part is not needed.

If we now repeat [Executing install/upgrade/repair/uninstall with previously defined custom actions](../05_RememberPropertyPattern#executing-installupgraderepairuninstall-with-previously-defined-custom-actions) and during step 3 (upgrade) pass `MY_PROPERTY=my_value_2` to the installer, we should get the following result:
```
18:55:01.312 - C:\Example_06\ - 1.0.0.0 - my_value - FirstInstall
18:55:06.796 - C:\Example_06\ - 1.0.0.0 - my_value - Maintenance
18:55:27.954 - C:\Example_06\ - 2.0.0.0 - my_value_2 - Upgrading
18:55:32.305 - C:\Example_06\ - 2.0.0.0 - my_value_2 - Maintenance
18:55:44.354 - C:\Example_06\ - 2.0.0.0 - my_value_2 - Uninstalling
```
As seen from the result, property value is successfully changed during the upgrade.\
NOTE: Properties cannot be changed during repair - according to [this](https://docs.microsoft.com/en-us/windows/win32/msi/command-line-options), the /f option "ignores any property values entered on the command line".