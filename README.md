# arxbuild

A command-line tool to compile ObjectARX plugins from a single .cpp file.

## Features

- Compile single .cpp file to .arx plugin without project files
- No need to open Visual Studio
- Automatic configuration of headers, libraries, and linker
- Auto-detect ARX SDK path, Visual Studio version, and library version
- No hardcoded versions — works with any CMake, VS, .NET, or ObjectARX SDK
- Cleanup temporary files after compilation

## Requirements

- Windows 7 SP1 or later
- Visual Studio 2017+ (with C++ development tools)
- CMake 2.8+ (added to PATH)
- ObjectARX SDK (any version)

## Usage

```bash
arxbuild.exe <source.cpp> [--sdk=PATH] [--platform=x64|x86] [--config=Release|Debug]
```

### Options

| Option | Description | Default |
|---|---|---|
| `--sdk=PATH` | ObjectARX SDK root directory | auto-detect |
| `--platform=x64` | Target platform | x64 |
| `--config=Release` | Build configuration | Release |

### Examples

```bash
# Basic usage (auto-detect SDK)
arxbuild.exe test.cpp

# Specify SDK path
arxbuild.exe test.cpp --sdk=D:\SDK\ObjectARX_2024

# Debug build, x86
arxbuild.exe test.cpp --platform=x86 --config=Debug
```

## ObjectARX SDK Path

The tool resolves the SDK path in the following priority:

1. **`--sdk=PATH`** command line argument (highest)
2. **`ARX_SDK_ROOT`** environment variable
3. **Auto-detect** — scan `C:\Autodesk\` for directories matching `*ObjectARX*` that contain an `inc\` subfolder

Set the environment variable for convenience:

```bash
set ARX_SDK_ROOT=C:\Autodesk\Autodesk_ObjectARX_2019_Win_64_and_32_Bit
```

Or pass it each time:

```bash
arxbuild.exe demo.cpp --sdk=C:\Autodesk\Autodesk_ObjectARX_2019_Win_64_and_32_Bit
```

## Build the Tool

### Option 1: Using Built-in C# Compiler (Recommended)

No Visual Studio or .NET SDK required — Windows includes `csc.exe`:

```bash
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe -nologo -out:arxbuild.exe Program.cs
```

### Option 2: Using Visual Studio Developer Command Prompt

```bash
csc /out:arxbuild.exe Program.cs
```

### Option 3: Using MSBuild

```bash
msbuild arxbuild.csproj /p:Configuration=Release
```

## How It Works

1. Resolves ObjectARX SDK path (`--sdk` > env var > auto-detect)
2. Auto-detects VS version via `vswhere.exe` for CMake generator
3. Auto-detects ARX library version by scanning `acdb*.lib` in the SDK
4. Creates a temporary directory
5. Generates `CMakeLists.txt` with proper ObjectARX configuration
6. Copies the source file to the temporary directory
7. Runs CMake to generate Visual Studio solution
8. Runs MSBuild to compile the project
9. Copies the resulting `.arx` file to the original directory
10. Cleans up the temporary directory

## Standalone CMake Project

The `build_arx_demo/` directory contains a standalone CMake project for manual builds:

```bash
cd build_arx_demo/build
cmake -DARX_SDK_ROOT=C:/Autodesk/your_sdk_path ..
cmake --build . --config Release
```

Or set the environment variable instead:

```bash
set ARX_SDK_ROOT=C:/Autodesk/your_sdk_path
cmake ..
cmake --build . --config Release
```

## License

MIT License
