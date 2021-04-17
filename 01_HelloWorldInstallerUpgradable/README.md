# 01_HelloWorldInstallerUpgradable

## Making an upgradable installer

Relevant code changes:
- the Project **Id** attribute is auto-generated
- **UpgradeCode** attribute is set (and is constant between versions)
```xml
  <Product Id="*"
           ...
           UpgradeCode="B28B3FA8-9310-4AA7-B0CA-CBCCA5FD00B2"
           ...>
```
- **MajorUpgrade** element is added
  - by default, it forbids the installation of an older version
  - the default value of the **Schedule** attribute is *afterInstallValidate*
    - removes the installed product entirely before installing the upgraded product
    - if the installation of the upgraded product fails, the machine will have neither version installed)
```xml
    <MajorUpgrade DowngradeErrorMessage="A later version of [ProductName] is already installed. Setup will now exit." />
```

## How upgrade works
- once installed, this installer is upgradable
- after installing version "1.0.0.0" of the installer, *FileToInstall.txt* is deployed to the installation folder and an entry is visible in Windows "Programs and Features"
- after modifying the version in the wxs file to e.g. "2.0.0.0" and modifying the content of *FileToInstall.txt*, a new installer can be built
- trying to install that installer will
  - first, uninstall the old version and then install the new version (order defined by *MajorUpgrade.Schedule*)
  - *FileToInstall.txt* in the installation folder will be updated with the new one
  - a "2.0.0.0" version entry will replace the "1.0.0.0" one in Windows "Programs and Features"
- trying to install an older version than the one installed will result in the error message (the one specified in *MajorUpgrade.DowngradeErrorMessage*)


## Additional reading

- [How To: Implement a Major Upgrade In Your Installer](https://wixtoolset.org/documentation/manual/v3/howtos/updates/major_upgrade.html)
- [MajorUpgrade Element](https://wixtoolset.org/documentation/manual/v3/xsd/wix/majorupgrade.html)