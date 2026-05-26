using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ArxBuild
{
    class Program
    {
        static string sdkPath = @"C:\Autodesk\Autodesk_ObjectARX_2019_Win_64_and_32_Bit";
        static string platform = "x64";
        static string buildType = "Release";

        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: arxbuild.exe <source.cpp>");
                return 1;
            }

            string cppFile = args[0];
            
            if (!File.Exists(cppFile))
            {
                Console.WriteLine(string.Format("Error: File not found - {0}", cppFile));
                return 1;
            }

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

        static string GenerateCMakeLists(string sourceFile, string targetName)
        {
            string incDir = Path.Combine(sdkPath, "inc").Replace("\\", "/");
            string incDirX64 = Path.Combine(sdkPath, "inc-x64").Replace("\\", "/");
            string libDir = Path.Combine(sdkPath, "lib-x64").Replace("\\", "/");

            return string.Format(@"
cmake_minimum_required(VERSION 3.15)
project({0})

set(CMAKE_CXX_STANDARD 14)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

if(MSVC)
    add_compile_options(/W3 /D""_WIN64"" /D""WINVER=0x0601"" /D""_WIN32_WINNT=0x0601"")
    add_compile_options(/D""ACRX_DECLARE_EXPORTS"" /D""_UNICODE"" /D""UNICODE"")
    add_compile_options(/EHsc)
endif()

include_directories(""{1}"" ""{4}"")
link_directories(""{2}"")

add_library({0} SHARED ""{3}"")

target_link_libraries({0}
    acdb23.lib
    acge23.lib
    acgiapi.lib
    acad.lib
    rxapi.lib
    acui23.lib
    acdbmgd.lib
    ac1st23.lib
    accore.lib
)

set_target_properties({0} PROPERTIES SUFFIX .arx)
set_target_properties({0} PROPERTIES OUTPUT_NAME ""{0}"")
", targetName, incDir, libDir, Path.GetFileName(sourceFile), incDirX64);
        }

        static void CopySourceFile(string sourceFile, string destDir)
        {
            string destFile = Path.Combine(destDir, Path.GetFileName(sourceFile));
            File.Copy(sourceFile, destFile, true);
        }

        static bool RunCmake(string workingDir)
        {
            Console.WriteLine("Running CMake...");
            
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmake",
                Arguments = string.Format("-G \"Visual Studio 17 2022\" -A {0} .", platform),
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

        static string FindMsBuild()
        {
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
                    return path;
            }

            return null;
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