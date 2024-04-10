[![GitHub Release](https://img.shields.io/github/v/release/crizzly57/TwinCAT-ADSLogging)](https://github.com/Crizzly57/TwinCAT-ADSLogging/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/Crizzly57/TwinCAT-ADSLogging/blob/main/LICENSE)

# TwinCAT-ADSLogging
Logging for TwinCAT2 &amp; TwinCAT3 over ADS.

This ADS-Logging provides comprehensive support for logging data from TwinCAT2 and TwinCAT3 environments. It offers a range of features to meet different logging requirements, including support for different data types, optional threshold setting, configuration of maximum decimal precision, and flexible configuration via XML files.

### How it works
To use ADSLogging, an AmsNet router such as TwinCAT2 or TwinCAT3 must be installed on the system running ADSLogging. This is necessary because a TwinCAT route to the target system must be established. You can run ADSLogging either on a PLC or on a remote PC connected to the PLC.
The .NET 4 runtime must also be installed on the system where ADSLogging is executed.

If ADSLogging is started and doesn't find a configuration, it automatically creates a default configuration, which serves as a starting point for further customisation.

To ensure compatibility with both TwinCAT2 and TwinCAT3, ADSLogging dynamically loads the correct TwinCAT.Ads.dll in the background at runtime, based on the configuration settings. Although TwinCAT3 is downward compatible with TwinCAT2, it's advisable to specify TwinCAT3 in the configuration for reasons of clarity. This is because the use of the TwinCAT.Ads.dll for TwinCAT2 has sometimes caused problems in the past when used with TwinCAT3.

As soon as a value of a variable to be logged changes in the PLC, an event is triggered and ADSLogging is informed of this.

The logged data is displayed in the console and saved to a text file in the format: *<date - time - variable - current value>*.

### Key Features
**TwinCAT2 & TwinCAT3 support:**  
Seamless integration in both TwinCAT2 and TwinCAT3 environments, ensuring cross-version compatibility.

**Various data types:**  
Supports logging for a wide range of data types, allowing flexibility in logging different types of data.
<details>
	<summary>Supported data types</summary>

- BOOL
- BYTE
- SINT
- USINT
- INT
- UINT
- DINT
- UDINT
- ULINT
- LINT
- WORD
- DWORD
- LWORD
- REAL
- LREAL
- TIME
- LTIME
- TIME AND DATE (DT)
- TIME OF DAY (TOD)
- DATE
- POINTER
- STRING (only ASCII)
</details>

**Optional Threshold:**  
Allows users to set optional thresholds, providing control over when data is logged based on pre-defined conditions.

**Maximum decimal precision:**  
Allows users to specify the maximum decimal precision for logged data, ensuring accuracy and consistency.

**Configuration via XML:**  
Enables easy configuration via XML files, allowing users to customise logging settings to suit their requirements.

### Configuration 
- **TwinCATVersion (required):** Specifies the TwinCAT version to be used. Possible values are TwinCAT2 or TwinCAT3.

- **AmsNetId (optional):** The AmsNetId of the target system. If left empty, the local AmsNetId will be used.

- **Port (required):** The port of the target system.

- **MaxLinesPerLogFile (required):** Sets the maximum number of lines per log file.

- **VariableConfig (required):** Defines the variables to be logged.
  
  - **Possible attributes:**
    - **DecimalPlaces:** Specifies the number of decimal places to display, applicable only to floating point numbers. Ignored for other types.
    - **Threshold:** Specifies the minimum value change required for data to be logged, applicable to numeric types only. Ignored for other types.

### Example
```xml
<?xml version="1.0" encoding="UTF-8"?>
<Configuration>
  <TwinCATVersion>TwinCAT3</TwinCATVersion>
  <AmsNetId>10.0.2.15.1.1</AmsNetId>
  <Port>851</Port>
  <MaxLinesPerLogFile>1000</MaxLinesPerLogFile>
  <VariableConfig>
    <var DecimalPlaces="2" Threshold="0.01">MAIN.rRealValue</var>
    <var DecimalPlaces="3">MAIN.rLREALValue</var>
    <var>MAIN.iIntValue</var>
    <var>MAIN.xBoolValue</var>
    <var Threshold="2">MAIN.bByteValue</var>
  </VariableConfig>
</Configuration>
```

<img alt="Console" src="https://github.com/Crizzly57/TwinCAT-ADSLogging/assets/81525848/8c102366-b516-40be-8998-cde373751839">

### Contributing

Contributions to TwinCAT-ADSLogging are welcome! If you encounter any bugs, have feature requests, or would like to contribute improvements, please feel free to follow these guidelines:

1. **Reporting Bugs:** If you find a bug or unexpected behavior, please open an issue on the [GitHub issues page](https://github.com/crizzly57/TwinCAT-ADSLogging/issues) with detailed information about the problem, including steps to reproduce it.

2. **Submitting Feature Requests:** If you have ideas for new features or enhancements, you can submit them by opening an issue on the [GitHub issues page](https://github.com/crizzly57/TwinCAT-ADSLogging/issues) and describing the proposed functionality.

3. **Contributing Code:** If you'd like to contribute code improvements or new features, you can do so by forking the repository, making your changes, and submitting a pull request. Please ensure that your code follows the project's coding standards and include relevant tests if applicable.

### License
This ADSLogging is licensed under the MIT License, granting users the freedom to use, modify, and distribute the software as per their needs.
