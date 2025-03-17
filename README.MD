# ArmaExtension

## Prerequisites

Before you begin, ensure you have the following installed:

1. **Visual Studio Build Tools**:
    - Download the Visual Studio Installer from the following link:
      [Visual Studio Build Tools](https://aka.ms/vs/17/release/vs_BuildTools.exe)
    - During installation, select the following components:
        - **MSVC v143 - VS 2022 C++ x64/x86 build tools** (or newer)
        - **Windows 11 or 10 SDK** (latest version)
        - **C++ CMake Tools for Windows**

## Setup

1. **Clone the Repository**:
    - Clone this repository to your local machine using:
      ```sh
      git clone https://github.com/VainoKarppi/ArmaExtension
      ```

2. **Configure Project**:
    - Open the `ArmaExtension.csproj` file.
    - Modify the `AssemblyName` and `Version` fields as per your requirements.

3. **Build the Project**:
    - Open a terminal and navigate to the project directory.
    - Run the following command to build the project:
      ```sh
      msbuild ArmaExtension.sln
      ```

## Running Tests

1. **Navigate to Tools Folder**:
    - Open a terminal and navigate to the `Tools` folder within the project directory.

2. **Run Test Script**:
    - Execute the `TestExtension.ps1` script to run the tests:
      ```sh
      ./TestExtension.ps1
      ```
    - If the script gets stuck at "Building the project...", press `Ctrl+C` and try running the script again.

3. **Edit `base_test.sqf`**:
    - You can edit the `base_test.sqf` file as you wish, but keep the `XXXX` placeholder since this is automatically updated, when a copy of this file is moved to the `callExtension` folder.

## License

This project is licensed under the MIT License. You are allowed to do whatever you want with it. See the [LICENSE](./LICENSE.MD) file for more details.
