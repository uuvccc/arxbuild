using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ArxBuild
{
    class Program
    {
        static string sdkPath;
        static string platform = "x64";
        static string buildType = "Release";

        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: arxbuild.exe <source.cpp> [--sdk=PATH] [--platform=x64|x86] [--config=Release|Debug]");
                Console.WriteLine("  --sdk=PATH      ObjectARX SDK root directory");
                Console.WriteLine("  --platform=x64  Target platform (default: x64)");
                Console.WriteLine("  --config=Release Build configuration (default: Release)");
                Console.WriteLine("\nSDK path priority: --sdk > ARX_SDK_ROOT env var > auto-detect");
                return 1;
            }

            string cppFile = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("--sdk="))
                    sdkPath = args[i].Substring(6);
                else if (args[i].StartsWith("--platform="))
                    platform = args[i].Substring(11);
                else if (args[i].StartsWith("--config="))
                    buildType = args[i].Substring(9);
                else if (!args[i].StartsWith("-"))
                    cppFile = args[i];
            }

            if (string.IsNullOrEmpty(cppFile))
            {
                Console.WriteLine("Error: No source file specified.");
                return 1;
            }

            if (!File.Exists(cppFile))
            {
                Console.WriteLine(string.Format("Error: File not found - {0}", cppFile));
                return 1;
            }

            // Resolve SDK path: --sdk > env var > auto-detect
            if (string.IsNullOrEmpty(sdkPath))
            {
                sdkPath = Environment.GetEnvironmentVariable("ARX_SDK_ROOT");
            }
            if (string.IsNullOrEmpty(sdkPath))
            {
                sdkPath = AutoDetectSdkPath();
            }
            if (string.IsNullOrEmpty(sdkPath) || !Directory.Exists(sdkPath))
            {
                Console.WriteLine("Error: ARX SDK not found. Use --sdk=PATH or set ARX_SDK_ROOT environment variable.");
                return 1;
            }
            Console.WriteLine(string.Format("Using ARX SDK: {0}", sdkPath));

            string fullPath = Path.GetFullPath(cppFile);
            string dir = Path.GetDirectoryName(fullPath);
            string fileName = Path.GetFileNameWithoutExtension(fullPath);
            string arxFile = Path.Combine(dir, string.Format("{0}.arx", fileName));

            Console.WriteLine(string.Format("Compiling {0} to {1}...", cppFile, arxFile));

            string tempDir = Path.Combine(Path.GetTempPath(), string.Format("arxbuild_{0}", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(tempDir);

            try
            {
                string cmakeLists = GenerateCMakeLists(fullPath, fileName);
                File.WriteAllText(Path.Combine(tempDir, "CMakeLists.txt"), cmakeLists);

                CopySourceFile(fullPath, tempDir);

                bool cmakeSuccess = RunCmake(tempDir);
                if (!cmakeSuccess)
                {
                    Console.WriteLine("CMake failed");
                    return 1;
                }

                bool buildSuccess = RunMsBuild(tempDir, fileName);
                if (!buildSuccess)
                {
                    Console.WriteLine("MSBuild failed");
                    return 1;
                }

                string outputArx = Path.Combine(tempDir, buildType, string.Format("{0}.arx", fileName));
                if (File.Exists(outputArx))
                {
                    File.Copy(outputArx, arxFile, true);
                    Console.WriteLine(string.Format("Successfully created {0}", arxFile));
                    return 0;
                }
                else
                {
                    Console.WriteLine(string.Format("Error: Output file not found - {0}", outputArx));
                    return 1;
                }
            }
            finally
            {
                Cleanup(tempDir);
            }
        }

        static string AutoDetectSdkPath()
        {
            // Scan C:\Autodesk for ObjectARX SDK directories
            string autodeskDir = @"C:\Autodesk";
            if (Directory.Exists(autodeskDir))
            {
                try
                {
                    var dirs = Directory.GetDirectories(autodeskDir, "*ObjectARX*")
                        .Concat(Directory.GetDirectories(autodeskDir, "*objectarx*"))
                        .Where(d => Directory.Exists(Path.Combine(d, "inc")))
                        .OrderByDescending(d => d)
                        .ToList();
                    if (dirs.Count > 0)
                    {
                        Console.WriteLine(string.Format("Auto-detected ARX SDK: {0}", dirs[0]));
                        return dirs[0];
                    }
                }
                catch { }
            }
            return null;
        }

        static string DetectLibVersion()
        {
            string libDir = Path.Combine(sdkPath, "lib-x64");
            if (!Directory.Exists(libDir))
            {
                libDir = Path.Combine(sdkPath, "lib");
            }
            if (Directory.Exists(libDir))
            {
                try
                {
                    var libFiles = Directory.GetFiles(libDir, "acdb*.lib");
                    foreach (var lib in libFiles)
                    {
                        string name = Path.GetFileNameWithoutExtension(lib);
                        Match m = Regex.Match(name, @"\d+$");
                        if (m.Success)
                        {
                            string version = m.Value;
                            Console.WriteLine(string.Format("Detected ARX lib version: {0}", version));
                            return version;
                        }
                    }
                }
                catch { }
            }
            Console.WriteLine("Warning: Could not detect ARX lib version, using empty suffix.");
            return "";
        }

        static string GenerateCMakeLists(string sourceFile, string targetName)
        {
            string incDir = Path.Combine(sdkPath, "inc").Replace("\\", "/");
            string incDirX64 = Path.Combine(sdkPath, "inc-x64").Replace("\\", "/");
            string libDir = Path.Combine(sdkPath, "lib-x64").Replace("\\", "/");
            string libVersion = DetectLibVersion();

            return string.Format(@"cmake_minimum_required(VERSION 2.8)
project({0} CXX)

if(MSVC)
    add_compile_options(/W3 /EHsc /std:c++14)
    add_definitions(-D_WIN64 -DWINVER=0x0601 -D_WIN32_WINNT=0x0601)
    add_definitions(-DACRX_DECLARE_EXPORTS -D_UNICODE -DUNICODE)
endif()

include_directories(""{1}"" ""{4}"")
link_directories(""{2}"")

add_library({0} SHARED ""{3}"")

target_link_libraries({0}
    ac1st{5}.lib
    acdb{5}.lib
    acge{5}.lib
    acgiapi.lib
    acad.lib
    rxapi.lib
    accore.lib
)

set_target_properties({0} PROPERTIES SUFFIX .arx)
set_target_properties({0} PROPERTIES OUTPUT_NAME ""{0}"")
", targetName, incDir, libDir, Path.GetFileName(sourceFile), incDirX64, libVersion);
        }

        static void CopySourceFile(string sourceFile, string destDir)
        {
            string destFile = Path.Combine(destDir, Path.GetFileName(sourceFile));
            File.Copy(sourceFile, destFile, true);
        }

        static string FindVswhere()
        {
            string candidate = @"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe";
            if (File.Exists(candidate)) return candidate;
            return null;
        }

        static string DetectCMakeGenerator()
        {
            string vswhere = FindVswhere();
            if (!string.IsNullOrEmpty(vswhere))
            {
                try
                {
                    // Find latest VS installation
                    var psi = new ProcessStartInfo
                    {
                        FileName = vswhere,
                        Arguments = "-latest -property installationVersion",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using (var proc = Process.Start(psi))
                    {
                        string output = proc.StandardOutput.ReadToEnd().Trim();
                        proc.WaitForExit();
                        Version v = null;
                        if (!string.IsNullOrEmpty(output) && Version.TryParse(output, out v))
                        {
                            int major = v.Major;
                            // Map VS major version to CMake generator name
                            // 17 = VS 2022, 16 = VS 2019, 15 = VS 2017
                            string genVersion = major.ToString();
                            string genYear;
                            switch (major)
                            {
                                case 17: genYear = "2022"; break;
                                case 16: genYear = "2019"; break;
                                case 15: genYear = "2017"; break;
                                default: genYear = null; break;
                            }
                            if (genYear != null)
                            {
                                string generator = string.Format("Visual Studio {0} {1}", genVersion, genYear);
                                Console.WriteLine(string.Format("Detected VS {0}, using CMake generator: {1}", genYear, generator));
                                return generator;
                            }
                        }
                    }
                }
                catch { }
            }

            // Fallback: let CMake auto-detect by not specifying -G
            Console.WriteLine("Warning: Could not detect VS version, letting CMake auto-detect generator.");
            return null;
        }

        static bool RunCmake(string workingDir)
        {
            Console.WriteLine("Running CMake...");

            string generator = DetectCMakeGenerator();
            string arguments;
            if (!string.IsNullOrEmpty(generator))
            {
                arguments = string.Format("-G \"{0}\" -A {1} .", generator, platform);
            }
            else
            {
                arguments = string.Format("-A {0} .", platform);
            }

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmake",
                Arguments = arguments,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(psi))
            {
                process.OutputDataReceived += (sender, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
                process.ErrorDataReceived += (sender, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
        }

        static string FindMsBuild()
        {
            // Try vswhere first (supports any VS version)
            string vswhere = FindVswhere();
            if (!string.IsNullOrEmpty(vswhere))
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = vswhere,
                        Arguments = "-latest -requires Microsoft.Component.MSBuild -find MSBuild\\**\\Bin\\MSBuild.exe",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using (var proc = Process.Start(psi))
                    {
                        string output = proc.StandardOutput.ReadToEnd().Trim();
                        proc.WaitForExit();
                        string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length > 0 && File.Exists(lines[0]))
                        {
                            Console.WriteLine(string.Format("Found MSBuild: {0}", lines[0]));
                            return lines[0];
                        }
                    }
                }
                catch { }
            }

            // Fallback: try common paths
            string[] possiblePaths = {
                @"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
                @"C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
                @"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
                @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
                @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
                @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    Console.WriteLine(string.Format("Found MSBuild: {0}", path));
                    return path;
                }
            }

            return null;
        }

        static bool RunMsBuild(string workingDir, string targetName)
        {
            Console.WriteLine("Running MSBuild...");

            string solutionFile = string.Format("{0}.sln", targetName);
            string msbuildPath = FindMsBuild();

            if (string.IsNullOrEmpty(msbuildPath))
            {
                Console.WriteLine("Error: MSBuild not found");
                return false;
            }

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = msbuildPath,
                Arguments = string.Format("{0} /p:Configuration={1} /p:Platform={2} /m", solutionFile, buildType, platform),
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(psi))
            {
                process.OutputDataReceived += (sender, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
                process.ErrorDataReceived += (sender, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
        }

        static void Cleanup(string tempDir)
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Warning: Failed to cleanup temp directory: {0}", ex.Message));
            }
        }
    }
}
