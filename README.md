# arxbuild

A simple command-line tool to compile ObjectARX 2019 plugins from a single .cpp file.

## Features

- Compile single .cpp file to .arx plugin without project files
- No need to open Visual Studio
- Automatic configuration of headers, libraries, and linker
- Uses ObjectARX 2019 SDK automatically
- Cleanup temporary files after compilation
- Output compilation logs
- Open source, can be uploaded to GitHub

## Requirements

- Windows 10/11
- Visual Studio 2022 (with C++ development tools)
- CMake (added to PATH)
- ObjectARX 2019 SDK installed at: `C:\Autodesk\Autodesk_ObjectARX_2019_Win_64_and_32_Bit`

## Usage

```bash
arxbuild.exe test.cpp
```

This will compile `test.cpp` and output `test.arx` in the same directory.

## Build the Tool

### Option 1: Using Built-in C# Compiler (Recommended)

Open Command Prompt and run:

```bash
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe /out:arxbuild.exe Program.cs
```

### Option 2: Using Visual Studio Developer Command Prompt

1. Open **Developer Command Prompt for VS 2022**
2. Navigate to the project directory
3. Run:

```bash
csc Program.cs /out:arxbuild.exe /target:exe
```

### Option 3: Using Visual Studio IDE

1. Open Visual Studio 2022
2. File → Open → File → Select `Program.cs`
3. Use Visual Studio's build functionality

## How It Works

1. Creates a temporary directory
2. Generates CMakeLists.txt with proper ObjectARX configuration
3. Copies the source file to the temporary directory
4. Runs CMake to generate Visual Studio solution
5. Runs MSBuild to compile the project
6. Copies the resulting .arx file to the original directory
7. Cleans up the temporary directory

## ObjectARX SDK Path

The tool assumes ObjectARX 2019 SDK is installed at:
`C:\Autodesk\Autodesk_ObjectARX_2019_Win_64_and_32_Bit`

If your SDK is installed at a different location, modify the `sdkPath` variable in `Program.cs`.

## License

MIT License

## Contributing

Feel free to submit issues and pull requests.