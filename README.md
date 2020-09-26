# QRev Field Data Plugin

[![Build status](https://ci.appveyor.com/api/projects/status/o04tlahqy188fcos/branch/master?svg=true)](https://ci.appveyor.com/project/SystemsAdministrator/qrev-field-data-plugin/branch/master)

An AQTS field data plugin for AQTS 2020.2-or-newer systems, which can import ADCP discharge summary XML measurements from the [USGS QREV software](https://hydroacoustics.usgs.gov/movingboat/QRev.shtml).

## Want to install this plugin?

- Download the [latest release here](https://github.com/AquaticInformatics/qrev-field-data-plugin/releases/latest).
- Install the plugin using the System Config page on your AQTS app server.

## Requirements for building the plugin from source

- Requires Visual Studio 2017 (Community Edition is fine)
- .NET 4.7 runtime

## Configuring the plugin

See the [Configuration page](src/QRev/Readme.md) for details.

## Building the plugin

- Load the `src\QRevPlugin.sln` file in Visual Studio and build the `Release` configuration.
- The `src\QRev\deploy\Release\QRev.plugin` file can then be installed on your AQTS app server.

## Testing the plugin within Visual Studio

Use the included `PluginTester.exe` tool from the `Aquarius.FieldDataFramework` package to test your plugin logic on the sample files.

1. Open the QRev project's **Properties** page
2. Select the **Debug** tab
3. Select **Start external program:** as the start action and browse to `"src\packages\Aquarius.FieldDataFramework.20.2.5\tools\PluginTester.exe`
4. Enter the **Command line arguments:** to launch your plugin

```
/Plugin=QRev.dll /Json=AppendedResults.json /Data=..\..\..\..\data\QRevDischargeSummary.xml
```

The `/Plugin=` argument can be the filename of your plugin assembly, without any folder. The default working directory for a start action is the bin folder containing your plugin.

5. Set a breakpoint in the plugin's `ParseFile()` methods.
6. Select your plugin project in Solution Explorer and select **"Debug | Start new instance"**
7. Now you're debugging your plugin!

See the [PluginTester](https://github.com/AquaticInformatics/aquarius-field-data-framework/tree/master/src/PluginTester) documentation for more details.
