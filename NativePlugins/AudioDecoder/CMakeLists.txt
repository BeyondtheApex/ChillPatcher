cmake_minimum_required(VERSION 3.15)
project(ChillPatcher_AudioDecoder VERSION 1.0.0 LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_C_STANDARD 11)

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)

# dr_libs headers (dr_mp3.h, dr_flac.h, dr_wav.h)
include_directories(${CMAKE_SOURCE_DIR}/../dr_libs)
include_directories(${CMAKE_SOURCE_DIR}/include)

set(SOURCES src/audio_decoder.cpp)

add_library(ChillAudioDecoder SHARED ${SOURCES})

if(WIN32)
    set_target_properties(ChillAudioDecoder PROPERTIES
        PREFIX ""
        SUFFIX ".dll"
        WINDOWS_EXPORT_ALL_SYMBOLS ON
    )
    if(MSVC)
        set_property(TARGET ChillAudioDecoder PROPERTY
            MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
    endif()
endif()

set(NATIVE_OUTPUT_DIR "${CMAKE_SOURCE_DIR}/../../bin/native")
install(TARGETS ChillAudioDecoder
    RUNTIME DESTINATION "${NATIVE_OUTPUT_DIR}/$<IF:$<EQUAL:${CMAKE_SIZEOF_VOID_P},8>,x64,x86>"
    LIBRARY DESTINATION "${NATIVE_OUTPUT_DIR}/$<IF:$<EQUAL:${CMAKE_SIZEOF_VOID_P},8>,x64,x86>"
)

message(STATUS "========================================")
message(STATUS "ChillPatcher Audio Decoder Native Plugin")
message(STATUS "  Formats: MP3, FLAC, WAV")
message(STATUS "  Build Type: ${CMAKE_BUILD_TYPE}")
message(STATUS "========================================")
