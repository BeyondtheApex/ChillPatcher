cmake_minimum_required(VERSION 3.15)
project(ChillPatcher_AudioDecoder VERSION 1.0.0 LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_C_STANDARD 11)

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)

# dr_libs headers (dr_mp3.h, dr_flac.h, dr_wav.h)
include_directories(${CMAKE_SOURCE_DIR}/../dr_libs)
include_directories(${CMAKE_SOURCE_DIR}/include)

# minimp4 header (MP4/M4A demuxer)
include_directories(${CMAKE_SOURCE_DIR}/../minimp4)

# fdk-aac (AAC decoder) - build as static library
set(BUILD_SHARED_LIBS OFF CACHE BOOL "" FORCE)
set(FDK_AAC_DIR ${CMAKE_SOURCE_DIR}/../fdk-aac)
add_subdirectory(${FDK_AAC_DIR} ${CMAKE_BINARY_DIR}/fdk-aac)
# Force fdk-aac to use static runtime as well
if(MSVC)
    set_property(TARGET fdk-aac PROPERTY
        MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
endif()

set(SOURCES src/audio_decoder.cpp)

add_library(ChillAudioDecoder SHARED ${SOURCES})

# Link fdk-aac and include its headers
target_link_libraries(ChillAudioDecoder PRIVATE fdk-aac)
target_include_directories(ChillAudioDecoder PRIVATE
    ${FDK_AAC_DIR}/libAACdec/include
    ${FDK_AAC_DIR}/libAACenc/include
    ${FDK_AAC_DIR}/libSYS/include
    ${FDK_AAC_DIR}/libMpegTPDec/include
    ${FDK_AAC_DIR}/libFDK/include
)

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
message(STATUS "  Formats: MP3, FLAC, WAV, AAC(M4A)")
message(STATUS "  Build Type: ${CMAKE_BUILD_TYPE}")
message(STATUS "========================================")
