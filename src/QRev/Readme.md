# QRev Field Data Plugin

The QRev plugin will work with stock AQTS 2020.2 systems with no special configuration required.

But if you have changed the configuration of some of your AQTS system's configurable "Drop-down Lists", then you may need to tell the QRev plugin how to correctly interpret a few key fields.

## The `Config.json` file stores the plugin's configuration

The plugin can be configured via a [`Config.json`](./Config.json) JSON document, to control the date and time formats used by your organization.

Use the Settings page of the System Config app to change the configuration setting.
- **Group**: `FieldDataPluginConfig-QRev`
- **Key**: `Config`<br/>
- **Value**: The entire contents of the Config.json file. If blank or omitted, the plugin's default [`Config.json`](./Config.json) is used.

This JSON document is reloaded each time a QRev file is uploaded to AQTS for parsing. Updates to the setting will take effect on the next QRev file parsed.

The JSON configuration information stores four settings:

| Property Name | Description |
| --- | --- |
| **TopEstimateMethods** | How to map the QRev `Constant`, `Power`, and `3-Point` top extrapolation methods to the AQTS `Top Estimate Method` list items. |
| **BottomEstimateMethods** | How to map the QRev `Power` and `No Slip` bottom extrapolation methods to the AQTS `Bottom Estimate Method` list items. |
| **DepthReferences** | How to map the QRev `BT`, `VB`, and `DS` source depths to the AQTS `Depth Reference` list items. |
| **NavigationMethods** | How to map the QRev `BT`, `GGA`, and `VTG` navigation references to the AQTS `Navigation Method` list items. |


```json
{
  "TopEstimateMethods": {
    "Constant": "CNST",
    "Power": "POWR",
    "3-Point": "3PNT"
  },
  "BottomEstimateMethods": {
    "Power": "POWR",
    "No Slip": "NSLP"
  },
  "DepthReferences": {
    "BT": "BottomTrack",
    "VB": "VerticalBeam",
    "DS": "DepthSounder",
    "Composite": "Composite"
  },
  "NavigationMethods": {
    "BT": "BT",
    "GGA": "GGA",
    "VTG": "VTG"
  }
}
```

Notes:
- Editing JSON files [can be tricky](#json-editing-tips). Don't include a trailing comma after the last item in any list.

## JSON editing tips

Editing [JSON](https://json.org) can be a tricky thing.

Sometimes the plugin code can detect a poorly formatted JSON document and report a decent error, but sometimes a poorly formatted JSON document will appear to the plugin as just an empty document.

Here are some tips to help eliminate common JSON config errors:
- Edit JSON in a real text editor. Notepad is fine, [Notepad++](https://notepad-plus-plus.org/) or [Visual Studio Code](https://code.visualstudio.com/) are even better choices.
- Don't try editing JSON in Microsoft Word. Word will mess up your quotes and you'll just have a bad time.
- Try validating your JSON using the online [JSONLint validator](https://jsonlint.com/).
- Whitespace between items is ignored. Your JSON document can be single (but very long!) line, but the convention is separate items on different lines, to make the text file more readable.
- All property names must be enclosed in double-quotes (`"`). Don't use single quotes (`'`) or smart quotes (`“` or `”`), which are actually not that smart for JSON!
- Avoid a trailing comma in lists. JSON is very fussy about using commas **between** list items, but rejects lists when a trailing comma is included. Only use a comma to separate items in the middle of a list.

### Adding comments to JSON

The JSON spec doesn't support comments, which is unfortunate.

However, the code will simply skip over properties it doesn't care about, so a common trick is to add a dummy property name/value string. The code won't care or complain, and you get to keep some notes close to other special values in your custom JSON document.

Instead of this:

```json
{
  "ExpectedPropertyName": "a value",
  "AnotherExpectedProperty": 12.5 
}
```

Try this:

```json
{
  "_comment_": "Don't enter a value below 12, otherwise things break",
  "ExpectedPropertyName": "a value",
  "AnotherExpectedProperty": 12.5 
}
```

Now your JSON has a comment to help you remember why you chose the `12.5` value.
