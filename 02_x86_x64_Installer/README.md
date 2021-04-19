# 02_x86_x64_Installer

## Making an x64 Installer

- x64 platform should be added to the project using Visual Studio's Configuration Manager
- when building the project using the new x64 configuration, Visual Studio will send the following arguments to *candle.exe*
```
-dPlatform=x64
-arch x64
```

## Hypothetical installer requirements

- we want to have both an x86 and an x64 installer for our product
- some files are deployed by both installers (e.g. *FileToInstall.txt*)
- installers should have different UpgradeCodes (should be possible to upgrade them independently)
- MSI file name should end with either "(x64)" or "(x86)" based on the platform
- MSI entry in Windows "Programs and Features" should end with either "(x64)" or "(x86)" based the on platform
- x86 installer
  - deploys *my_x86.dll*
  - by default is installed in the 32-bit program files folder
  - writes registry keys in `HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node`
- x64 installer
  - deploys *my_x64.dll*
  - by default is installed in the 64-bit program files folder
  - writes registry keys in `HKEY_LOCAL_MACHINE\SOFTWARE`
- it should be possible to install both installers on the same machine at the same time

## Implementation of requirements

- we get two installers by building one for the x86 platform and another by building for the x64 platform
- platform info (e.g. "(x64)") is appended to the name of the built installer by manually adding *OutputName* in the wixproj file
```xml
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|x64' ">
    ...
    <OutputName>02_x86_x64_Installer (x64)</OutputName>
    ...
  </PropertyGroup>
```
- different UpgradeCodes, different entries in "Programs and Features" and installation to 32-bit/64-bit program files folder 
  are achieved by using the [include file](PlatformDependentVariables.wxi) where different values are defined based on the platform
- these values are then used as variables in the main wxs file
- [preprocessor](https://wixtoolset.org/documentation/manual/v3/overview/preprocessor.html) directives are used to determine which files will be deployed in which MSI (x86/x64)
```xml
  <?if $(var.Platform) = x64?>
    <File KeyPath="yes" Source="my_x64.dll" />
  <?else?>
    <File KeyPath="yes" Source="my_x86.dll" />
  <?endif?>
```
- registry entries will be automatically saved either to `HKEY_LOCAL_MACHINE\SOFTWARE (x64)` or `HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node` (x86) based on platform
