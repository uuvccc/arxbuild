cmake_minimum_required(VERSION 3.15)
project(demo)

set(CMAKE_CXX_STANDARD 14)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# ObjectARX SDK path: -DARX_SDK_ROOT > environment variable ARX_SDK_ROOT
if(NOT ARX_SDK_ROOT)
    if(DEFINED ENV{ARX_SDK_ROOT})
        set(ARX_SDK_ROOT "$ENV{ARX_SDK_ROOT}" CACHE PATH "ObjectARX SDK root directory")
    else()
        message(FATAL_ERROR "ARX_SDK_ROOT not set. Use -DARX_SDK_ROOT=<path> or set ARX_SDK_ROOT environment variable.")
    endif()
endif()
message(STATUS "ARX_SDK_ROOT = ${ARX_SDK_ROOT}")

# Auto-detect ARX library version suffix (e.g. "23" from acdb23.lib)
file(GLOB _ARX_ACDB_LIB "${ARX_SDK_ROOT}/lib-x64/acdb*.lib")
if(_ARX_ACDB_LIB)
    get_filename_component(_ARX_ACDB_LIB_NAME "${_ARX_ACDB_LIB}" NAME_WE)
    string(REGEX MATCH "[0-9]+$" ARX_LIB_VERSION "${_ARX_ACDB_LIB_NAME}")
    message(STATUS "Detected ARX lib version: ${ARX_LIB_VERSION}")
else()
    message(FATAL_ERROR "Cannot find acdb*.lib in ${ARX_SDK_ROOT}/lib-x64")
endif()

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

# Link libraries (auto-detected version suffix for versioned libs)
target_link_libraries(${PROJECT_NAME}
    ac1st${ARX_LIB_VERSION}
    acdb${ARX_LIB_VERSION}
    acge${ARX_LIB_VERSION}
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
