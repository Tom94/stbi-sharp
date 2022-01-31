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

    # Static build
    set(CompilerFlags
        CMAKE_CXX_FLAGS CMAKE_CXX_FLAGS_DEBUG CMAKE_CXX_FLAGS_RELEASE
        CMAKE_CXX_FLAGS_MINSIZEREL CMAKE_CXX_FLAGS_RELWITHDEBINFO
        CMAKE_C_FLAGS CMAKE_C_FLAGS_DEBUG CMAKE_C_FLAGS_RELEASE
        CMAKE_C_FLAGS_MINSIZEREL CMAKE_C_FLAGS_RELWITHDEBINFO
    )

    foreach (CompilerFlag ${CompilerFlags})
        string(REPLACE "/MD" "/MT" ${CompilerFlag} "${${CompilerFlag}}")
    endforeach()
elseif (CMAKE_CXX_COMPILER_ID MATCHES "Clang" OR CMAKE_CXX_COMPILER_ID MATCHES "GNU")
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} --std=c++11 -O3")
endif()

include_directories(
    ${CMAKE_CURRENT_SOURCE_DIR}/../dependencies/qoi
    ${CMAKE_CURRENT_SOURCE_DIR}/../dependencies/stb
)

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
    DESTINATION ${CMAKE_CURRENT_SOURCE_DIR}/../runtimes/${RUNTIME_DIR}/native
)
