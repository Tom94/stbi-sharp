cmake_minimum_required(VERSION 3.0.0)

project(stbi)

# Set a default configuration if none was specified
if (NOT CMAKE_BUILD_TYPE AND NOT CMAKE_CONFIGURATION_TYPES)
    message(STATUS "No release type specified. Setting to 'Release'.")
    set(CMAKE_BUILD_TYPE Release CACHE STRING "Choose the type of build." FORCE)
    set_property(CACHE CMAKE_BUILD_TYPE PROPERTY STRINGS "Debug" "Release" "RelWithDebInfo")
endif()

if (MSVC)
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /D_CRT_SECURE_NO_WARNINGS")
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /MP")
elseif (CMAKE_CXX_COMPILER_ID MATCHES "Clang" OR CMAKE_CXX_COMPILER_ID MATCHES "GNU")
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} --std=c++11 -O3")
endif()

add_library(stbi SHARED
    src/stbi.cpp
)

if (MSVC)
    set(RUNTIME_DIR "win")
elseif (APPLE)
    set(RUNTIME_DIR "osx")
else()
    set(RUNTIME_DIR "linux")
endif()

if (CMAKE_SIZEOF_VOID_P EQUAL 8)
    set(RUNTIME_DIR "${RUNTIME_DIR}-x64")
elseif (CMAKE_SIZEOF_VOID_P EQUAL 4)
    set(RUNTIME_DIR "${RUNTIME_DIR}-x86")
endif()

install(TARGETS stbi
    LIBRARY DESTINATION ${CMAKE_CURRENT_SOURCE_DIR}/../bin/${RUNTIME_DIR}
)
