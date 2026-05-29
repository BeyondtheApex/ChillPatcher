cmake_minimum_required(VERSION 3.20)
project(ForzaHorizon6OmniBridge LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

if(NOT CMAKE_BUILD_TYPE)
    set(CMAKE_BUILD_TYPE Release)
endif()

if(NOT WIN32)
    message(FATAL_ERROR "ForzaHorizon6OmniBridge targets Windows only")
endif()

set(FH6_UPSTREAM_DIR "${CMAKE_CURRENT_SOURCE_DIR}/../fh6-universal-radio")
set(OMNI_PCM_SHARED_DIR "${CMAKE_CURRENT_SOURCE_DIR}/../OmniPcmShared")

add_compile_definitions(_WIN32_WINNT=0x0A00 WIN32_LEAN_AND_MEAN NOMINMAX UNICODE _UNICODE)

if(MSVC)
    add_compile_options(/W4 /permissive- /Zc:__cplusplus /utf-8 /MP /EHsc /wd4324)
    add_compile_options("$<$<CONFIG:Release>:/O2;/GL;/Gy;/Gw>")
    add_link_options("$<$<CONFIG:Release>:/LTCG;/OPT:REF;/OPT:ICF>")
    add_compile_definitions(_CRT_SECURE_NO_WARNINGS)
endif()

add_library(version SHARED
    "${FH6_UPSTREAM_DIR}/src/proxy/dll_main.cpp"
    "${FH6_UPSTREAM_DIR}/src/log.cpp"
    "${FH6_UPSTREAM_DIR}/src/audio_source_manager.cpp"
    "${FH6_UPSTREAM_DIR}/src/fmod/pe_image.cpp"
    "${FH6_UPSTREAM_DIR}/src/fmod/sig_scanner.cpp"
    "${FH6_UPSTREAM_DIR}/src/fmod/radio_discovery.cpp"
    "${FH6_UPSTREAM_DIR}/src/fmod/game_state_probe.cpp"
    "${FH6_UPSTREAM_DIR}/src/fmod/metadata_injector.cpp"
    "${FH6_UPSTREAM_DIR}/src/fmod/dsp_control_loop.cpp"
    src/bridge.cpp
    src/ring_buffer.cpp
    src/fmod/dsp_bridge.cpp
    src/sources/omni_pcm_source.cpp
)

if(NOT MSVC)
    target_sources(version PRIVATE "${FH6_UPSTREAM_DIR}/src/proxy/version.def")
endif()

target_include_directories(version PRIVATE
    include
    "${FH6_UPSTREAM_DIR}/include"
    "${OMNI_PCM_SHARED_DIR}/include"
)

target_link_libraries(version PRIVATE ws2_32 bcrypt psapi winhttp)

set_target_properties(version PROPERTIES
    OUTPUT_NAME version
    PREFIX ""
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/bin"
    RUNTIME_OUTPUT_DIRECTORY_RELEASE "${CMAKE_CURRENT_SOURCE_DIR}/bin"
    RUNTIME_OUTPUT_DIRECTORY_DEBUG "${CMAKE_CURRENT_SOURCE_DIR}/bin"
)

if(MSVC)
    set_property(TARGET version PROPERTY
        MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
endif()
