cmake_minimum_required(VERSION 3.15)
project(demo)

set(CMAKE_CXX_STANDARD 14)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# ObjectARX SDK
set(ARX_SDK_ROOT "C:/Autodesk/Autodesk_ObjectARX_2019_Win_64_and_32_Bit")

# Include directories
include_directories(
    ${ARX_SDK_ROOT}/inc
    ${ARX_SDK_ROOT}/inc-x64
)

# Library directories
link_directories(
    ${ARX_SDK_ROOT}/lib-x64
)

# Source files
add_library(${PROJECT_NAME} SHARED
    "demo.cpp"
)

# Preprocessor definitions
target_compile_definitions(${PROJECT_NAME} PRIVATE
    _WINDOWS
    _ACRXAPP
    _RXSDK
    _CRT_SECURE_NO_WARNINGS
    UNICODE
    _UNICODE
)

# Link libraries (order matters for MSVC linker)
target_link_libraries(${PROJECT_NAME}
    ac1st23
    acdb23
    acge23
    accore
    acad
    rxapi
)

# Output as .arx (DLL with .arx extension)
set_target_properties(${PROJECT_NAME} PROPERTIES
    SUFFIX ".arx"
    PREFIX ""
    LINK_FLAGS "/EXPORT:acrxEntryPoint /EXPORT:acrxGetApiVersion"
)
