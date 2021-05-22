# 09_InstallerWithUserInterface

In this example, we will reuse the [06_RememberPropertyPatternComplete example](../06_RememberPropertyPatternComplete/) and add a user interface to the MSI.\
In the user interface, the user will be able to set the value for the install location and the value of one property.

## Predefined user interface

WiX comes with several predefined user interfaces described [here](https://wixtoolset.org/documentation/manual/v3/wixui/wixui_dialog_library.html).\
The simplest one (`WixUI_Minimal`) just shows one dialog where the user needs to accept the shown license agreement.

Two steps are needed to include that dialog in the MSI:
- installer project must have a reference to `WixUIExtension.dll`
- in the installer wxs file, the following line must be added:
  ```xml
  <UIRef Id="WixUI_Minimal" />
  ```
Other types of predefined user interfaces come with dialogs are more customization options, such as:
- ability to choose install location
- ability to choose which features will be installed

## Creating and integrating custom dialogs

As a base installer, we will use [WixUI_InstallDir](https://wixtoolset.org//documentation/manual/v3/wixui/dialog_reference/wixui_installdir.html).\
This predefined user interface gives the ability to choose an install location.\
The ability to set custom property values will be added using a custom dialog.
