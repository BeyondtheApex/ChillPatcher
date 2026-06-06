cmake_minimum_required(VERSION 3.15)
project(OmniPcmShared VERSION 1.0.0 LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_C_STANDARD 11)

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)

include(FetchContent)

set(protobuf_BUILD_TESTS OFF CACHE BOOL "" FORCE)
set(protobuf_BUILD_PROTOC_BINARIES OFF CACHE BOOL "" FORCE)
set(protobuf_INSTALL OFF CACHE BOOL "" FORCE)
FetchContent_Declare(
    protobuf
    GIT_REPOSITORY https://github.com/protocolbuffers/protobuf.git
    GIT_TAG v29.3
)
FetchContent_MakeAvailable(protobuf)

include_directories(${CMAKE_SOURCE_DIR}/include)

file(GLOB_RECURSE GENERATED_PROTO_SOURCES
    "${CMAKE_SOURCE_DIR}/generated/*.pb.cc"
)

set(SOURCES
    src/omni_pcm_shared.cpp
    src/omni_pcm_control.cpp
    ${GENERATED_PROTO_SOURCES}
)

add_library(OmniPcmShared SHARED ${SOURCES})

target_include_directories(OmniPcmShared PRIVATE
    ${CMAKE_SOURCE_DIR}/generated
    ${CMAKE_SOURCE_DIR}/../../ForzaRadioprobe
)

target_link_libraries(OmniPcmShared PRIVATE protobuf::libprotobuf-lite)

if(WIN32)
    target_compile_definitions(OmniPcmShared PRIVATE
        UNICODE
        _UNICODE
        WIN32_LEAN_AND_MEAN
        BUILDING_OMNI_PCM_SHARED
    )
    target_link_libraries(OmniPcmShared PRIVATE winhttp)

    set_target_properties(OmniPcmShared PROPERTIES
        PREFIX ""
        SUFFIX ".dll"
        WINDOWS_EXPORT_ALL_SYMBOLS ON
    )

    if(MSVC)
        set_property(TARGET OmniPcmShared PROPERTY
            MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
        target_compile_options(OmniPcmShared PRIVATE /wd4100)
    endif()
endif()

set(NATIVE_OUTPUT_DIR "${CMAKE_SOURCE_DIR}/../../bin/native")
install(TARGETS OmniPcmShared
    RUNTIME DESTINATION "${NATIVE_OUTPUT_DIR}/$<IF:$<EQUAL:${CMAKE_SIZEOF_VOID_P},8>,x64,x86>"
    LIBRARY DESTINATION "${NATIVE_OUTPUT_DIR}/$<IF:$<EQUAL:${CMAKE_SIZEOF_VOID_P},8>,x64,x86>"
)

message(STATUS "========================================")
message(STATUS "OmniPcmShared Native SDK")
message(STATUS "  Build Type: ${CMAKE_BUILD_TYPE}")
message(STATUS "  Output: ${CMAKE_RUNTIME_OUTPUT_DIRECTORY}")
message(STATUS "========================================")
