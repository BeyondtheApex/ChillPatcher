#define WIN32_LEAN_AND_MEAN
#define VER_H
#include <windows.h>
#include <psapi.h>

#include <algorithm>
#include <atomic>
#include <cstdint>
#include <cstdarg>
#include <cstdio>
#include <cstring>
#include <mutex>
#include <sstream>
#include <string>
#include <vector>

#include "httplib.h"

namespace {

HMODULE g_self = nullptr;
HMODULE g_realVersion = nullptr;
std::atomic<bool> g_shutdown{false};

struct SectionInfo {
    uintptr_t start = 0;
    size_t size = 0;
};

struct ModuleInfo {
    uintptr_t base = 0;
    size_t size = 0;
    SectionInfo text;
    SectionInfo rdata;
    SectionInfo data;
};

struct RttiResult {
    uintptr_t typeName = 0;
    uintptr_t typeDesc = 0;
    uintptr_t col = 0;
    uintptr_t vtable = 0;
    uint32_t typeDescRva = 0;
    uint32_t colRva = 0;
    uint32_t vtableRva = 0;
    int colCandidates = 0;
    int vtableCandidates = 0;
    bool ok = false;
};

struct MsvcString {
    bool ok = false;
    uintptr_t field = 0;
    uintptr_t data = 0;
    uint64_t len = 0;
    uint64_t cap = 0;
    bool sso = false;
    std::string value;
};

struct Candidate {
    uintptr_t refcount = 0;
    uintptr_t streamObject = 0;
    int32_t uses = 0;
    int32_t weaks = 0;
    uintptr_t objectVtable = 0;
    uintptr_t fmodSound = 0;
    uintptr_t propsOwner = 0;
    uintptr_t sampleProperties = 0;
    uint32_t handle32 = 0;
    MsvcString soundName;
    MsvcString displayName;
    MsvcString artist;
    bool r10FallbackSound = false;
    int score = 0;
};

struct RadioStateResult {
    bool sigFound = false;
    bool chainOk = false;
    uintptr_t globalPtrSlot = 0;
    uint32_t globalPtrRva = 0;
    uintptr_t p1 = 0;
    uintptr_t p2 = 0;
    uintptr_t p3 = 0;
    MsvcString stationName;
    bool activeStreamerMode = false;
};

struct ControlState;
std::string BuildStatusJson(const ControlState& s);

struct LockState {
    bool locked = false;
    uintptr_t refcount = 0;
    uintptr_t fmodSound = 0;
    uintptr_t propsOwner = 0;
    uintptr_t sampleProperties = 0;
    uint32_t handle32 = 0;
    uint64_t sinceTick = 0;
    std::string liveStatus = "none";
};

struct HandlePathElement {
    uintptr_t elemAddr = 0;
    uintptr_t field_00 = 0, field_08 = 0, field_10 = 0;
    uintptr_t ptrAt18 = 0;
    uintptr_t field_20 = 0, field_28 = 0;
    uint32_t handle = 0;
    uint32_t field_34 = 0;
    uintptr_t field_38 = 0, field_40 = 0;
    bool soundInModule = false;
};

struct HandlePathDump {
    bool ok = false;
    uintptr_t sampleProperties = 0;
    uintptr_t handleBase = 0;
    uintptr_t vecBegin = 0, vecEnd = 0;
    uint64_t vecSize = 0;
    std::string error;
    std::vector<HandlePathElement> items;
};

struct FmodApiEntry {
    std::string name;
    uintptr_t stringRva = 0;
    uintptr_t leaRva = 0;
    uintptr_t funcRva = 0;
    uintptr_t funcAddr = 0;
    bool found = false;
    std::string note;
};

struct FmodApiResult {
    bool ok = false;
    std::vector<FmodApiEntry> entries;
    uintptr_t systemI = 0;
    std::string error;
};

struct TopologyDump {
    bool ok = false;
    uintptr_t targetSound = 0;
    uintptr_t systemI = 0;
    uintptr_t masterCg = 0;
    int groupsSeen = 0;
    int channelsSeen = 0;
    bool matched = false;
    uintptr_t matchedChannel = 0;
    uintptr_t matchedGroup = 0;
    bool handleFallback = false;
    std::string error;
    std::vector<std::string> log;
};

struct NativePatternHit {
    uintptr_t addr = 0;
    uintptr_t rva = 0;
    uintptr_t funcAddr = 0;
    uintptr_t funcRva = 0;
    int alt = 0;
};

struct NativePatternResult {
    std::string name;
    std::string pattern;
    std::vector<NativePatternHit> hits;
};

struct SystemIContextHit {
    uintptr_t ctx = 0;
    uintptr_t rangeBase = 0;
    uintptr_t rangeSize = 0;
    uintptr_t vecBegin = 0;
    uintptr_t vecEnd = 0;
    uint64_t vecCount = 0;
    uintptr_t slot = 0;
    uintptr_t elem = 0;
    uintptr_t p1 = 0;
    uintptr_t systemI = 0;
    uintptr_t vtable = 0;
    int index = -1;
    int textEntries = 0;
    int rdataEntries = 0;
    int dataEntries = 0;
    int moduleEntries = 0;
    int nonModuleEntries = 0;
    int asciiLikeEntries = 0;
    int prefixTextEntries = 0;
    int firstBadIndex = -1;
    int score = 0;
};

struct ControlState {
    ModuleInfo mod;
    RttiResult rtti;
    RadioStateResult radio;
    std::vector<Candidate> candidates;
    int scanCount = 0;
    uint64_t lastScanTick = 0;
    std::string phase = "loaded";
    std::string lastCommand;
    std::string lastError;
    bool haveOriginalMeta = false;
    uintptr_t originalSampleProperties = 0;
    std::string originalSoundName;
    std::string originalDisplayName;
    std::string originalArtist;
    LockState lock;
    HandlePathDump handlePath;
    FmodApiResult apiResult;
    TopologyDump topo;
    std::vector<NativePatternResult> nativePatterns;
    std::vector<SystemIContextHit> systemIHits;
};

MsvcString ReadMsvcString(uintptr_t field);
std::string BuildStatusJson(const ControlState& s);

HMODULE RealVersion() {
    if (g_realVersion) return g_realVersion;
    wchar_t buf[MAX_PATH]{};
    GetSystemDirectoryW(buf, MAX_PATH);
    wcscat_s(buf, L"\\version.dll");
    g_realVersion = LoadLibraryW(buf);
    return g_realVersion;
}

template <typename Fn>
Fn RealProc(const char* name) {
    HMODULE m = RealVersion();
    return m ? reinterpret_cast<Fn>(GetProcAddress(m, name)) : nullptr;
}

void GetOutputPath(char* path, const char* fname) {
    GetModuleFileNameA(g_self, path, MAX_PATH);
    char* s = strrchr(path, '\\');
    if (s) s[1] = '\0'; else path[0] = '\0';
    strcat_s(path, MAX_PATH, fname);
}

void AppendLog(const char* fmt, ...) {
    if (!g_self) return;
    char path[MAX_PATH];
    GetOutputPath(path, "forza_radio_probe.log");

    FILE* fp = nullptr;
    fopen_s(&fp, path, "a");
    if (!fp) return;

    SYSTEMTIME st{};
    GetLocalTime(&st);
    fprintf(fp, "[%04u-%02u-%02u %02u:%02u:%02u.%03u] ",
        st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds);

    va_list args;
    va_start(args, fmt);
    vfprintf(fp, fmt, args);
    va_end(args);

    fprintf(fp, "\n");
    fclose(fp);
}

template <typename T>
bool SafeRead(uintptr_t addr, T* out) {
    SIZE_T read = 0;
    return ReadProcessMemory(GetCurrentProcess(), reinterpret_cast<LPCVOID>(addr), out, sizeof(T), &read) &&
           read == sizeof(T);
}

bool SafeReadBytes(uintptr_t addr, void* out, size_t len) {
    SIZE_T read = 0;
    return ReadProcessMemory(GetCurrentProcess(), reinterpret_cast<LPCVOID>(addr), out, len, &read) &&
           read == len;
}

bool SafeWriteBytes(uintptr_t addr, const void* data, size_t len) {
    SIZE_T written = 0;
    return WriteProcessMemory(GetCurrentProcess(), reinterpret_cast<LPVOID>(addr), data, len, &written) &&
           written == len;
}

bool InRange(uintptr_t p, uintptr_t start, size_t size) {
    return p >= start && p < start + size;
}

bool InModule(const ModuleInfo& mod, uintptr_t p) {
    return InRange(p, mod.base, mod.size);
}

bool InText(const ModuleInfo& mod, uintptr_t p) {
    return InRange(p, mod.text.start, mod.text.size);
}

bool InRdata(const ModuleInfo& mod, uintptr_t p) {
    return InRange(p, mod.rdata.start, mod.rdata.size);
}

bool InData(const ModuleInfo& mod, uintptr_t p) {
    return InRange(p, mod.data.start, mod.data.size);
}

bool InTextOrImage(const ModuleInfo& mod, uintptr_t p) {
    return InRange(p, mod.text.start, mod.text.size) || InModule(mod, p);
}

const char* SectionName(const ModuleInfo& mod, uintptr_t p) {
    if (InText(mod, p)) return ".text";
    if (InRdata(mod, p)) return ".rdata";
    if (InData(mod, p)) return ".data";
    if (InModule(mod, p)) return "image";
    return "external";
}

bool LooksAsciiQword(uintptr_t v) {
    int printable = 0;
    int nonZero = 0;
    for (int i = 0; i < 8; ++i) {
        unsigned char c = static_cast<unsigned char>((v >> (i * 8)) & 0xff);
        if (c == 0) continue;
        ++nonZero;
        if (c >= 0x20 && c < 0x7f) ++printable;
    }
    return nonZero >= 4 && printable == nonZero;
}

std::string JsonEscape(const std::string& s) {
    std::string out;
    out.reserve(s.size() + 16);
    for (unsigned char c : s) {
        switch (c) {
        case '\\': out += "\\\\"; break;
        case '"': out += "\\\""; break;
        case '\n': out += "\\n"; break;
        case '\r': out += "\\r"; break;
        case '\t': out += "\\t"; break;
        default:
            if (c < 0x20) {
                char tmp[8];
                sprintf_s(tmp, "\\u%04x", c);
                out += tmp;
            } else {
                out.push_back(static_cast<char>(c));
            }
            break;
        }
    }
    return out;
}

std::string Hex(uintptr_t v) {
    char buf[32];
    sprintf_s(buf, "0x%llX", static_cast<unsigned long long>(v));
    return buf;
}

std::string HexBytes(uintptr_t addr, size_t len) {
    if (!addr || len == 0) return "";
    if (len > 256) len = 256;
    std::vector<uint8_t> bytes(len);
    if (!SafeReadBytes(addr, bytes.data(), len)) return "";
    static const char* kHex = "0123456789ABCDEF";
    std::string out;
    out.reserve(len * 3);
    for (size_t i = 0; i < len; ++i) {
        if (i) out.push_back(' ');
        out.push_back(kHex[bytes[i] >> 4]);
        out.push_back(kHex[bytes[i] & 0xf]);
    }
    return out;
}

std::mutex g_stateMutex;
ControlState g_state;

bool GetMainModuleInfo(ModuleInfo* out) {
    HMODULE exe = GetModuleHandleW(nullptr);
    MODULEINFO mi{};
    if (!GetModuleInformation(GetCurrentProcess(), exe, &mi, sizeof(mi))) return false;

    ModuleInfo mod{};
    mod.base = reinterpret_cast<uintptr_t>(mi.lpBaseOfDll);
    mod.size = mi.SizeOfImage;

    auto* dos = reinterpret_cast<IMAGE_DOS_HEADER*>(mod.base);
    if (dos->e_magic != IMAGE_DOS_SIGNATURE) return false;
    auto* nt = reinterpret_cast<IMAGE_NT_HEADERS*>(mod.base + dos->e_lfanew);
    if (nt->Signature != IMAGE_NT_SIGNATURE) return false;

    auto* sec = IMAGE_FIRST_SECTION(nt);
    for (WORD i = 0; i < nt->FileHeader.NumberOfSections; ++i) {
        char name[9]{};
        memcpy(name, sec[i].Name, 8);
        SectionInfo si{mod.base + sec[i].VirtualAddress, sec[i].Misc.VirtualSize};
        if (strcmp(name, ".text") == 0) mod.text = si;
        else if (strcmp(name, ".rdata") == 0) mod.rdata = si;
        else if (strcmp(name, ".data") == 0) mod.data = si;
    }

    *out = mod;
    return true;
}

std::vector<uintptr_t> FindBytes(uintptr_t start, size_t size, const char* needle) {
    std::vector<uintptr_t> hits;
    const size_t n = strlen(needle);
    if (!start || size < n) return hits;

    const char* p = reinterpret_cast<const char*>(start);
    for (size_t i = 0; i + n <= size; ++i) {
        if (memcmp(p + i, needle, n) == 0) {
            hits.push_back(start + i);
        }
    }
    return hits;
}

std::vector<int> ParsePattern(const char* pattern) {
    std::vector<int> out;
    const char* p = pattern;
    while (*p) {
        while (*p == ' ') ++p;
        if (!*p) break;
        if (p[0] == '?' && p[1] == '?') {
            out.push_back(-1);
            p += 2;
            continue;
        }
        unsigned int byte = 0;
        if (sscanf_s(p, "%2x", &byte) == 1) {
            out.push_back(static_cast<int>(byte & 0xff));
            p += 2;
        } else {
            break;
        }
    }
    return out;
}

uintptr_t FindPattern(uintptr_t start, size_t size, const char* pattern) {
    auto pat = ParsePattern(pattern);
    if (!start || pat.empty() || size < pat.size()) return 0;

    std::vector<uint8_t> bytes(size);
    if (!SafeReadBytes(start, bytes.data(), size)) return 0;

    for (size_t i = 0; i + pat.size() <= size; ++i) {
        bool ok = true;
        for (size_t j = 0; j < pat.size(); ++j) {
            if (pat[j] >= 0 && bytes[i + j] != static_cast<uint8_t>(pat[j])) {
                ok = false;
                break;
            }
        }
        if (ok) return start + i;
    }
    return 0;
}

std::vector<std::string> SplitPatternAlternates(const char* pattern) {
    std::vector<std::string> out;
    std::string cur;
    for (const char* p = pattern; *p; ++p) {
        if (*p == '|') {
            if (!cur.empty()) out.push_back(cur);
            cur.clear();
        } else {
            cur.push_back(*p);
        }
    }
    if (!cur.empty()) out.push_back(cur);
    return out;
}

std::vector<uintptr_t> FindPatternAllInBytes(uintptr_t start, const std::vector<uint8_t>& bytes,
                                             const char* pattern, size_t maxHits) {
    std::vector<uintptr_t> hits;
    auto pat = ParsePattern(pattern);
    if (pat.empty() || bytes.size() < pat.size()) return hits;
    for (size_t i = 0; i + pat.size() <= bytes.size(); ++i) {
        bool ok = true;
        for (size_t j = 0; j < pat.size(); ++j) {
            if (pat[j] >= 0 && bytes[i + j] != static_cast<uint8_t>(pat[j])) {
                ok = false;
                break;
            }
        }
        if (ok) {
            hits.push_back(start + i);
            if (hits.size() >= maxHits) break;
        }
    }
    return hits;
}

uintptr_t ResolveRipRelativeTarget(uintptr_t instruction) {
    int32_t disp = 0;
    if (!SafeRead(instruction + 3, &disp)) return 0;
    return instruction + 7 + disp;
}

uintptr_t ResolveRipRelativeTargetFromDisp(uintptr_t dispAddr) {
    int32_t disp = 0;
    if (!SafeRead(dispAddr, &disp)) return 0;
    return dispAddr + 4 + disp;
}

RadioStateResult ResolveRadioState(const ModuleInfo& mod) {
    RadioStateResult out{};
    const char* sig =
        "48 89 5C 24 08 48 89 54 24 10 57 48 83 EC 40 "
        "48 8B FA 48 8B 1D ?? ?? ?? ?? 48 85 DB 74 16 "
        "48 8D 4C 24 20 E8 ?? ?? ?? ?? 48 8B D0 48 8B CB";

    uintptr_t hit = FindPattern(mod.text.start, mod.text.size, sig);
    if (!hit) return out;

    out.sigFound = true;
    // The closed mod's resolver uses offset 0x15 for this signature. That offset
    // points at the disp32 of `48 8B 1D disp32`, not the start of the instruction.
    uintptr_t dispAddr = hit + 0x15;
    out.globalPtrSlot = ResolveRipRelativeTargetFromDisp(dispAddr);
    if (!InModule(mod, out.globalPtrSlot)) return out;
    out.globalPtrRva = static_cast<uint32_t>(out.globalPtrSlot - mod.base);

    if (!SafeRead(out.globalPtrSlot, &out.p1) || !out.p1) return out;
    if (!SafeRead(out.p1 + 0x40, &out.p2) || !out.p2) return out;
    if (!SafeRead(out.p2 + 0x50, &out.p3) || !out.p3) return out;
    out.stationName = ReadMsvcString(out.p3 + 0x200);
    out.chainOk = out.stationName.ok;
    out.activeStreamerMode =
        out.stationName.value == "Streamer Mode" ||
        out.stationName.value == "Spotify Radio";
    return out;
}

RttiResult ResolveRefcountRtti(const ModuleInfo& mod) {
    RttiResult out{};
    const char* decorated = ".?AV?$_Ref_count_obj2@VRadioStreamFmod@@@std@@";

    auto nameHits = FindBytes(mod.base, mod.size, decorated);
    if (nameHits.empty() && mod.rdata.start) {
        nameHits = FindBytes(mod.rdata.start, mod.rdata.size, "RadioStreamFmod");
    }
    if (nameHits.empty()) return out;

    uintptr_t name = 0;
    uintptr_t typeDesc = 0;
    for (uintptr_t hit : nameHits) {
        uintptr_t candidateName = hit;
        if (memcmp(reinterpret_cast<void*>(hit), "RadioStreamFmod", 15) == 0) {
            uintptr_t lo = hit > 0x80 ? hit - 0x80 : hit;
            for (uintptr_t p = hit; p > lo + 2; --p) {
                const char* s = reinterpret_cast<const char*>(p - 2);
                if (s[0] == '.' && s[1] == '?' && s[2] == 'A' && s[3] == 'V') {
                    candidateName = p - 2;
                    break;
                }
            }
        }
        if (candidateName >= mod.base + 0x10 && InModule(mod, candidateName)) {
            name = candidateName;
            typeDesc = candidateName - 0x10;
            break;
        }
    }
    if (!name || !typeDesc) return out;

    out.typeName = name;
    out.typeDesc = typeDesc;
    out.typeDescRva = static_cast<uint32_t>(typeDesc - mod.base);

    auto scanCols = [&](uintptr_t start, size_t size) {
        // MSVC x64 CompleteObjectLocator:
        // int signature, int offset, int cdOffset, int typeDescriptorRva, int classDescriptorRva, int selfRva.
        std::vector<uint8_t> bytes(size);
        if (!SafeReadBytes(start, bytes.data(), size)) return;
        for (size_t off = 0; off + 0x18 <= size; off += 4) {
            uintptr_t p = start + off;
            const uint8_t* b = bytes.data() + off;
            int32_t sig = *reinterpret_cast<const int32_t*>(b + 0x00);
            uint32_t td = *reinterpret_cast<const uint32_t*>(b + 0x0c);
            uint32_t self = *reinterpret_cast<const uint32_t*>(b + 0x14);
            if (sig == 1 && td == out.typeDescRva && self == static_cast<uint32_t>(p - mod.base)) {
                ++out.colCandidates;
                if (!out.col) {
                    out.col = p;
                    out.colRva = static_cast<uint32_t>(p - mod.base);
                }
            }
        }
    };

    if (mod.rdata.start) scanCols(mod.rdata.start, mod.rdata.size);
    if (!out.col) scanCols(mod.base, mod.size);

    if (!out.col) return out;

    auto scanVtables = [&](uintptr_t start, size_t size) {
        std::vector<uint8_t> bytes(size);
        if (!SafeReadBytes(start, bytes.data(), size)) return;
        for (size_t off = 0; off + 16 <= size; off += 8) {
            uintptr_t p = start + off;
            uintptr_t q = *reinterpret_cast<const uintptr_t*>(bytes.data() + off);
            if (q != out.col) continue;
            uintptr_t vt = p + 8;
            uintptr_t first = *reinterpret_cast<const uintptr_t*>(bytes.data() + off + 8);
            if (!InTextOrImage(mod, first)) continue;
            ++out.vtableCandidates;
            if (!out.vtable) {
                out.vtable = vt;
                out.vtableRva = static_cast<uint32_t>(vt - mod.base);
            }
        }
    };

    if (mod.rdata.start) scanVtables(mod.rdata.start, mod.rdata.size);
    if (!out.vtable) scanVtables(mod.base, mod.size);

    out.ok = out.vtable != 0;
    return out;

#if 0
    for (uintptr_t p = mod.base; p + 0x18 <= mod.base + mod.size; p += 4) {
        int32_t sig = 0, offset = 0, cdOffset = 0;
        uint32_t td = 0, classDesc = 0, self = 0;
        if (!SafeRead(p + 0x00, &sig) || !SafeRead(p + 0x04, &offset) ||
            !SafeRead(p + 0x08, &cdOffset) || !SafeRead(p + 0x0c, &td) ||
            !SafeRead(p + 0x10, &classDesc) || !SafeRead(p + 0x14, &self)) {
            continue;
        }
        if (sig == 1 && td == out.typeDescRva && self == static_cast<uint32_t>(p - mod.base)) {
            ++out.colCandidates;
            if (!out.col) {
                out.col = p;
                out.colRva = static_cast<uint32_t>(p - mod.base);
            }
        }
    }

    if (!out.col) return out;

    auto scanVtables = [&](uintptr_t start, size_t size) {
        for (uintptr_t p = start; p + 8 <= start + size; p += 8) {
            uintptr_t q = 0;
            if (!SafeRead(p, &q)) continue;
            if (q != out.col) continue;
            uintptr_t vt = p + 8;
            uintptr_t first = 0;
            if (!SafeRead(vt, &first)) continue;
            if (!InTextOrImage(mod, first)) continue;
            ++out.vtableCandidates;
            if (!out.vtable) {
                out.vtable = vt;
                out.vtableRva = static_cast<uint32_t>(vt - mod.base);
            }
        }
    };

    if (mod.rdata.start) scanVtables(mod.rdata.start, mod.rdata.size);
    if (!out.vtable) scanVtables(mod.base, mod.size);

    out.ok = out.vtable != 0;
    return out;
#endif
}

MsvcString ReadMsvcString(uintptr_t field) {
    MsvcString s{};
    s.field = field;

    char inlineBuf[16]{};
    uintptr_t heapPtr = 0;
    uint64_t len = 0;
    uint64_t cap = 0;
    if (!SafeReadBytes(field, inlineBuf, sizeof(inlineBuf))) return s;
    if (!SafeRead(field + 0x00, &heapPtr)) return s;
    if (!SafeRead(field + 0x10, &len)) return s;
    if (!SafeRead(field + 0x18, &cap)) return s;
    if (len > 0x1000 || cap > 0x100000) return s;

    s.len = len;
    s.cap = cap;
    s.sso = cap < 0x10;
    s.data = s.sso ? field : heapPtr;

    if (len == 0) {
        s.ok = true;
        return s;
    }

    std::vector<char> buf(static_cast<size_t>(len) + 1, 0);
    if (s.sso) {
        if (len > 15) return s;
        memcpy(buf.data(), inlineBuf, static_cast<size_t>(len));
    } else {
        if (heapPtr < 0x10000) return s;
        if (!SafeReadBytes(heapPtr, buf.data(), static_cast<size_t>(len))) return s;
    }

    int printable = 0;
    for (size_t i = 0; i < static_cast<size_t>(len); ++i) {
        unsigned char c = static_cast<unsigned char>(buf[i]);
        if (c == 0) break;
        if (c >= 0x20 && c < 0x7f) ++printable;
        else if (c >= 0x80) ++printable; // Keep possible UTF-8 metadata.
    }

    if (len && printable == 0) return s;
    s.value.assign(buf.data(), static_cast<size_t>(len));
    s.ok = true;
    return s;
}

bool WriteMsvcString(uintptr_t field, const std::string& value, std::string* error) {
    MsvcString cur = ReadMsvcString(field);
    if (!cur.ok) {
        if (error) *error = "target string is not readable";
        return false;
    }
    if (value.size() > cur.cap) {
        if (error) {
            char msg[160];
            sprintf_s(msg, "value length %llu exceeds current capacity %llu",
                static_cast<unsigned long long>(value.size()),
                static_cast<unsigned long long>(cur.cap));
            *error = msg;
        }
        return false;
    }

    uint64_t len = static_cast<uint64_t>(value.size());
    if (cur.sso) {
        char buf[16]{};
        memcpy(buf, value.data(), value.size());
        if (!SafeWriteBytes(field, buf, sizeof(buf))) {
            if (error) *error = "failed to write SSO string buffer";
            return false;
        }
    } else {
        std::vector<char> buf(value.size() + 1, 0);
        memcpy(buf.data(), value.data(), value.size());
        if (!SafeWriteBytes(cur.data, buf.data(), buf.size())) {
            if (error) *error = "failed to write heap string buffer";
            return false;
        }
    }

    if (!SafeWriteBytes(field + 0x10, &len, sizeof(len))) {
        if (error) *error = "failed to write string length";
        return false;
    }
    return true;
}

int ScoreCandidate(const ModuleInfo& mod, const Candidate& c) {
    int score = 0;
    if (c.refcount && c.streamObject == c.refcount + 0x10) score += 5;
    if (c.uses > 0 && c.uses <= 0x80 && c.weaks > 0 && c.weaks <= 0x80) score += 10;
    if (InModule(mod, c.objectVtable)) score += 10;
    if (c.fmodSound) score += 5;
    if (c.handle32) score += 15;  // R10/StreamerMode instances have a valid FMOD handle
    if (c.propsOwner) score += 5;
    if (c.sampleProperties) score += 10;
    if (c.soundName.ok) score += 20;
    if (c.displayName.ok) score += 15;
    if (c.artist.ok) score += 15;
    if (c.soundName.value.find("HZ6_") != std::string::npos) score += 20;
    if (c.soundName.value.find("PeterBroderick") != std::string::npos) score += 20;
    if (c.displayName.value.find("Spotify") != std::string::npos) score += 20;
    if (c.displayName.value.find("Streamer") != std::string::npos) score += 10;
    if (c.artist.value.find("Spotify") != std::string::npos) score += 15;
    return score;
}

std::vector<Candidate> ScanHeapCandidates(const ModuleInfo& mod, uintptr_t refcountVtable) {
    std::vector<Candidate> out;
    if (!refcountVtable) return out;

    MEMORY_BASIC_INFORMATION mbi{};
    uintptr_t addr = 0;
    uint64_t regions = 0;
    uint64_t scannedMb = 0;
    uint64_t firstHitTick = 0;
    uint64_t firstHitScannedMb = 0;
    while (VirtualQuery(reinterpret_cast<LPCVOID>(addr), &mbi, sizeof(mbi)) == sizeof(mbi)) {
        uintptr_t base = reinterpret_cast<uintptr_t>(mbi.BaseAddress);
        uintptr_t end = base + mbi.RegionSize;
        DWORD prot = mbi.Protect & 0xff;
        bool readable = prot == PAGE_READONLY || prot == PAGE_READWRITE || prot == PAGE_WRITECOPY ||
                        prot == PAGE_EXECUTE_READ || prot == PAGE_EXECUTE_READWRITE || prot == PAGE_EXECUTE_WRITECOPY;
        bool skip = mbi.State != MEM_COMMIT || (mbi.Protect & PAGE_GUARD) || (mbi.Protect & PAGE_NOACCESS) ||
                    !readable || mbi.RegionSize > 0x4000000 ||
                    InRange(base, mod.base, mod.size);

        if (!skip) {
            ++regions;
            scannedMb += static_cast<uint64_t>(mbi.RegionSize >> 20);
            if ((regions % 128) == 0) {
                AppendLog("heap scan progress: regions=%llu mb=%llu hits=%llu",
                    static_cast<unsigned long long>(regions),
                    static_cast<unsigned long long>(scannedMb),
                    static_cast<unsigned long long>(out.size()));
            }

            std::vector<uint8_t> bytes(mbi.RegionSize);
            if (!SafeReadBytes(base, bytes.data(), mbi.RegionSize)) {
                if (end <= addr) break;
                addr = end;
                continue;
            }

            uintptr_t p = (base + 0x0f) & ~uintptr_t(0x0f);
            uintptr_t scanEnd = end & ~uintptr_t(0x0f);
            for (; p + 0x60 <= scanEnd; p += 0x10) {
                size_t off = static_cast<size_t>(p - base);
                uintptr_t vt = *reinterpret_cast<const uintptr_t*>(bytes.data() + off);
                if (vt != refcountVtable) continue;

                Candidate c{};
                c.refcount = p;
                c.streamObject = p + 0x10;
                c.uses = *reinterpret_cast<const int32_t*>(bytes.data() + off + 0x08);
                c.weaks = *reinterpret_cast<const int32_t*>(bytes.data() + off + 0x0c);
                c.objectVtable = *reinterpret_cast<const uintptr_t*>(bytes.data() + off + 0x10);
                c.fmodSound = *reinterpret_cast<const uintptr_t*>(bytes.data() + off + 0x18);
                c.handle32 = *reinterpret_cast<const uint32_t*>(bytes.data() + off + 0x30);
                c.propsOwner = *reinterpret_cast<const uintptr_t*>(bytes.data() + off + 0x58);

                if (c.uses <= 0 || c.uses > 0x80 || c.weaks <= 0 || c.weaks > 0x80) continue;
                if (!InModule(mod, c.objectVtable)) continue;

                if (c.propsOwner) {
                    SafeRead(c.propsOwner + 0x18, &c.sampleProperties);
                }
                if (c.sampleProperties) {
                    c.soundName = ReadMsvcString(c.sampleProperties + 0x10);
                    c.displayName = ReadMsvcString(c.sampleProperties + 0x30);
                    c.artist = ReadMsvcString(c.sampleProperties + 0x50);
                }
                c.r10FallbackSound =
                    c.soundName.value == "HZ6_R9_PeterBroderick_EyesClosedandTraveling";
                c.score = ScoreCandidate(mod, c);
                out.push_back(c);

                if (firstHitTick == 0) {
                    firstHitTick = GetTickCount64();
                    firstHitScannedMb = scannedMb;
                    AppendLog("heap first hit: refcount=%s score=%d scanned_mb=%llu",
                        Hex(c.refcount).c_str(), c.score,
                        static_cast<unsigned long long>(scannedMb));
                }
            }
        }

        if (firstHitTick != 0) {
            uint64_t elapsedAfterHit = GetTickCount64() - firstHitTick;
            uint64_t mbAfterHit = scannedMb - firstHitScannedMb;
            if (out.size() >= 5 && (elapsedAfterHit > 10000 || mbAfterHit > 512)) {
                AppendLog("heap scan early stop: hits=%llu elapsed_after_hit_ms=%llu mb_after_hit=%llu",
                    static_cast<unsigned long long>(out.size()),
                    static_cast<unsigned long long>(elapsedAfterHit),
                    static_cast<unsigned long long>(mbAfterHit));
                break;
            }
        }

        if (end <= addr) break;
        addr = end;
    }

    AppendLog("heap scan done: regions=%llu mb=%llu raw_hits=%llu",
        static_cast<unsigned long long>(regions),
        static_cast<unsigned long long>(scannedMb),
        static_cast<unsigned long long>(out.size()));

    std::sort(out.begin(), out.end(), [](const Candidate& a, const Candidate& b) {
        return a.score > b.score;
    });
    if (out.size() > 64) out.resize(64);
    return out;
}

void WriteStringJson(FILE* fp, const char* name, const MsvcString& s, bool comma) {
    fprintf(fp, "        \"%s\": {\"ok\": %s, \"field\": \"%s\", \"data\": \"%s\", \"len\": %llu, \"cap\": %llu, \"sso\": %s, \"value\": \"%s\"}%s\n",
        name,
        s.ok ? "true" : "false",
        Hex(s.field).c_str(),
        Hex(s.data).c_str(),
        static_cast<unsigned long long>(s.len),
        static_cast<unsigned long long>(s.cap),
        s.sso ? "true" : "false",
        JsonEscape(s.value).c_str(),
        comma ? "," : "");
}

void WriteRadioStateJson(FILE* fp, const RadioStateResult& radio) {
    fprintf(fp, "  \"radio_state\": {\n");
    fprintf(fp, "    \"sig_found\": %s,\n", radio.sigFound ? "true" : "false");
    fprintf(fp, "    \"chain_ok\": %s,\n", radio.chainOk ? "true" : "false");
    fprintf(fp, "    \"active_streamer_mode\": %s,\n", radio.activeStreamerMode ? "true" : "false");
    fprintf(fp, "    \"global_ptr_slot\": \"%s\", \"global_ptr_rva\": \"0x%X\",\n",
        Hex(radio.globalPtrSlot).c_str(), radio.globalPtrRva);
    fprintf(fp, "    \"p1\": \"%s\", \"p2\": \"%s\", \"p3\": \"%s\",\n",
        Hex(radio.p1).c_str(), Hex(radio.p2).c_str(), Hex(radio.p3).c_str());
    fprintf(fp, "    \"station_name\": {\"ok\": %s, \"field\": \"%s\", \"data\": \"%s\", \"len\": %llu, \"cap\": %llu, \"sso\": %s, \"value\": \"%s\"}\n",
        radio.stationName.ok ? "true" : "false",
        Hex(radio.stationName.field).c_str(),
        Hex(radio.stationName.data).c_str(),
        static_cast<unsigned long long>(radio.stationName.len),
        static_cast<unsigned long long>(radio.stationName.cap),
        radio.stationName.sso ? "true" : "false",
        JsonEscape(radio.stationName.value).c_str());
    fprintf(fp, "  },\n");
}

void WriteProbeJson(const ModuleInfo& mod, const RttiResult& rtti, const RadioStateResult& radio,
                    const std::vector<Candidate>& candidates, int snapshot) {
    char path[MAX_PATH];
    GetOutputPath(path, "forza_radio_probe.json");
    FILE* fp = nullptr;
    fopen_s(&fp, path, "w");
    if (!fp) return;

    fprintf(fp, "{\n");
    fprintf(fp, "  \"snapshot\": %d,\n", snapshot);
    fprintf(fp, "  \"pid\": %lu,\n", GetCurrentProcessId());
    fprintf(fp, "  \"module\": {\n");
    fprintf(fp, "    \"base\": \"%s\", \"size\": %llu,\n", Hex(mod.base).c_str(), static_cast<unsigned long long>(mod.size));
    fprintf(fp, "    \"text\": {\"start\": \"%s\", \"size\": %llu},\n", Hex(mod.text.start).c_str(), static_cast<unsigned long long>(mod.text.size));
    fprintf(fp, "    \"rdata\": {\"start\": \"%s\", \"size\": %llu},\n", Hex(mod.rdata.start).c_str(), static_cast<unsigned long long>(mod.rdata.size));
    fprintf(fp, "    \"data\": {\"start\": \"%s\", \"size\": %llu}\n", Hex(mod.data.start).c_str(), static_cast<unsigned long long>(mod.data.size));
    fprintf(fp, "  },\n");
    fprintf(fp, "  \"rtti\": {\n");
    fprintf(fp, "    \"ok\": %s,\n", rtti.ok ? "true" : "false");
    fprintf(fp, "    \"type_name\": \"%s\", \"type_desc\": \"%s\", \"type_desc_rva\": \"0x%X\",\n",
        Hex(rtti.typeName).c_str(), Hex(rtti.typeDesc).c_str(), rtti.typeDescRva);
    fprintf(fp, "    \"col\": \"%s\", \"col_rva\": \"0x%X\", \"col_candidates\": %d,\n",
        Hex(rtti.col).c_str(), rtti.colRva, rtti.colCandidates);
    fprintf(fp, "    \"vtable\": \"%s\", \"vtable_rva\": \"0x%X\", \"vtable_candidates\": %d\n",
        Hex(rtti.vtable).c_str(), rtti.vtableRva, rtti.vtableCandidates);
    fprintf(fp, "  },\n");
    WriteRadioStateJson(fp, radio);
    fprintf(fp, "  \"candidate_count\": %llu,\n", static_cast<unsigned long long>(candidates.size()));
    fprintf(fp, "  \"candidates\": [\n");

    for (size_t i = 0; i < candidates.size(); ++i) {
        const Candidate& c = candidates[i];
        fprintf(fp, "    {\n");
        fprintf(fp, "      \"score\": %d,\n", c.score);
        fprintf(fp, "      \"refcount\": \"%s\", \"stream_object\": \"%s\",\n", Hex(c.refcount).c_str(), Hex(c.streamObject).c_str());
        fprintf(fp, "      \"uses\": %d, \"weaks\": %d,\n", c.uses, c.weaks);
        fprintf(fp, "      \"object_vtable\": \"%s\", \"fmod_sound\": \"%s\", \"handle32\": %u,\n",
            Hex(c.objectVtable).c_str(), Hex(c.fmodSound).c_str(), c.handle32);
        fprintf(fp, "      \"props_owner\": \"%s\", \"sample_properties\": \"%s\",\n",
            Hex(c.propsOwner).c_str(), Hex(c.sampleProperties).c_str());
        fprintf(fp, "      \"r10_fallback_sound\": %s,\n", c.r10FallbackSound ? "true" : "false");
        fprintf(fp, "      \"strings\": {\n");
        WriteStringJson(fp, "sound_name", c.soundName, true);
        WriteStringJson(fp, "display_name", c.displayName, true);
        WriteStringJson(fp, "artist", c.artist, false);
        fprintf(fp, "      }\n");
        fprintf(fp, "    }%s\n", i + 1 == candidates.size() ? "" : ",");
    }

    fprintf(fp, "  ]\n");
    fprintf(fp, "}\n");
    fclose(fp);
}

std::string MsvcStringJson(const MsvcString& s) {
    std::ostringstream os;
    os << "{\"ok\":" << (s.ok ? "true" : "false")
       << ",\"field\":\"" << Hex(s.field) << "\""
       << ",\"data\":\"" << Hex(s.data) << "\""
       << ",\"len\":" << static_cast<unsigned long long>(s.len)
       << ",\"cap\":" << static_cast<unsigned long long>(s.cap)
       << ",\"sso\":" << (s.sso ? "true" : "false")
       << ",\"value\":\"" << JsonEscape(s.value) << "\"}";
    return os.str();
}

std::string CandidateJson(const Candidate& c) {
    std::ostringstream os;
    os << "{"
       << "\"score\":" << c.score
       << ",\"refcount\":\"" << Hex(c.refcount) << "\""
       << ",\"stream_object\":\"" << Hex(c.streamObject) << "\""
       << ",\"uses\":" << c.uses
       << ",\"weaks\":" << c.weaks
       << ",\"object_vtable\":\"" << Hex(c.objectVtable) << "\""
        << ",\"fmod_sound\":\"" << Hex(c.fmodSound) << "\""
        << ",\"handle32\":" << c.handle32
        << ",\"props_owner\":\"" << Hex(c.propsOwner) << "\""
       << ",\"sample_properties\":\"" << Hex(c.sampleProperties) << "\""
       << ",\"r10_fallback_sound\":" << (c.r10FallbackSound ? "true" : "false")
       << ",\"strings\":{"
       << "\"sound_name\":" << MsvcStringJson(c.soundName) << ","
       << "\"display_name\":" << MsvcStringJson(c.displayName) << ","
       << "\"artist\":" << MsvcStringJson(c.artist)
       << "}}";
    return os.str();
}

std::string BuildStatusJson(const ControlState& s) {
    std::ostringstream os;
    os << "{";
    os << "\"pid\":" << GetCurrentProcessId();
    os << ",\"phase\":\"" << JsonEscape(s.phase) << "\"";
    os << ",\"scan_count\":" << s.scanCount;
    os << ",\"last_scan_tick\":" << static_cast<unsigned long long>(s.lastScanTick);
    os << ",\"last_command\":\"" << JsonEscape(s.lastCommand) << "\"";
    os << ",\"last_error\":\"" << JsonEscape(s.lastError) << "\"";
    os << ",\"http\":{\"host\":\"0.0.0.0\",\"port\":8104}";
    os << ",\"module\":{"
       << "\"base\":\"" << Hex(s.mod.base) << "\","
       << "\"size\":" << static_cast<unsigned long long>(s.mod.size) << ","
       << "\"text\":{\"start\":\"" << Hex(s.mod.text.start) << "\",\"size\":" << static_cast<unsigned long long>(s.mod.text.size) << "},"
       << "\"rdata\":{\"start\":\"" << Hex(s.mod.rdata.start) << "\",\"size\":" << static_cast<unsigned long long>(s.mod.rdata.size) << "},"
       << "\"data\":{\"start\":\"" << Hex(s.mod.data.start) << "\",\"size\":" << static_cast<unsigned long long>(s.mod.data.size) << "}"
       << "}";
    os << ",\"rtti\":{"
       << "\"ok\":" << (s.rtti.ok ? "true" : "false") << ","
       << "\"type_desc\":\"" << Hex(s.rtti.typeDesc) << "\","
       << "\"type_desc_rva\":\"0x" << std::hex << std::uppercase << s.rtti.typeDescRva << std::dec << "\","
       << "\"col\":\"" << Hex(s.rtti.col) << "\","
       << "\"col_rva\":\"0x" << std::hex << std::uppercase << s.rtti.colRva << std::dec << "\","
       << "\"col_candidates\":" << s.rtti.colCandidates << ","
       << "\"vtable\":\"" << Hex(s.rtti.vtable) << "\","
       << "\"vtable_rva\":\"0x" << std::hex << std::uppercase << s.rtti.vtableRva << std::dec << "\","
       << "\"vtable_candidates\":" << s.rtti.vtableCandidates
       << "}";
    os << ",\"radio_state\":{"
       << "\"sig_found\":" << (s.radio.sigFound ? "true" : "false") << ","
       << "\"chain_ok\":" << (s.radio.chainOk ? "true" : "false") << ","
       << "\"active_streamer_mode\":" << (s.radio.activeStreamerMode ? "true" : "false") << ","
       << "\"global_ptr_slot\":\"" << Hex(s.radio.globalPtrSlot) << "\","
       << "\"global_ptr_rva\":\"0x" << std::hex << std::uppercase << s.radio.globalPtrRva << std::dec << "\","
       << "\"p1\":\"" << Hex(s.radio.p1) << "\","
       << "\"p2\":\"" << Hex(s.radio.p2) << "\","
       << "\"p3\":\"" << Hex(s.radio.p3) << "\","
       << "\"station_name\":" << MsvcStringJson(s.radio.stationName)
       << "}";
    os << ",\"candidate_count\":" << s.candidates.size();
    os << ",\"best_candidate\":";
    if (s.candidates.empty()) os << "null";
    else os << CandidateJson(s.candidates.front());
    os << ",\"candidates\":[";
    for (size_t i = 0; i < s.candidates.size() && i < 8; ++i) {
        if (i) os << ",";
        os << CandidateJson(s.candidates[i]);
    }
    os << "]";
    os << ",\"original_meta\":{"
       << "\"available\":" << (s.haveOriginalMeta ? "true" : "false") << ","
       << "\"sample_properties\":\"" << Hex(s.originalSampleProperties) << "\","
       << "\"sound_name\":\"" << JsonEscape(s.originalSoundName) << "\","
        << "\"display_name\":\"" << JsonEscape(s.originalDisplayName) << "\","
       << "\"artist\":\"" << JsonEscape(s.originalArtist) << "\""
       << "},";
    os << "\"lock\":{"
       << "\"locked\":" << (s.lock.locked ? "true" : "false") << ","
       << "\"refcount\":\"" << Hex(s.lock.refcount) << "\","
       << "\"fmod_sound\":\"" << Hex(s.lock.fmodSound) << "\","
       << "\"handle32\":" << s.lock.handle32 << ","
       << "\"status\":\"" << JsonEscape(s.lock.liveStatus) << "\""
       << "}";
    os << ",\"native_pattern_specs\":" << s.nativePatterns.size();
    os << ",\"systemi_context_hits\":" << s.systemIHits.size();
    os << "}\n";
    return os.str();
}

void WriteStatusJson(const char* phase, const char* detail, int snapshot) {
    char path[MAX_PATH];
    GetOutputPath(path, "forza_radio_probe.json");
    FILE* fp = nullptr;
    fopen_s(&fp, path, "w");
    if (!fp) return;
    fprintf(fp, "{\n");
    fprintf(fp, "  \"snapshot\": %d,\n", snapshot);
    fprintf(fp, "  \"pid\": %lu,\n", GetCurrentProcessId());
    fprintf(fp, "  \"phase\": \"%s\",\n", JsonEscape(phase ? phase : "").c_str());
    fprintf(fp, "  \"detail\": \"%s\"\n", JsonEscape(detail ? detail : "").c_str());
    fprintf(fp, "}\n");
    fclose(fp);
}

// ── Handle Path Dump ──────────────────────────────────────────
HandlePathDump ReadHandlePathDump(uintptr_t sampleProperties, const ModuleInfo& mod) {
    HandlePathDump out{};
    out.sampleProperties = sampleProperties;
    if (!sampleProperties) { out.error = "no sampleProperties"; return out; }

    SafeRead(sampleProperties + 0x58, &out.handleBase);
    SafeRead(sampleProperties + 0x80, &out.vecBegin);
    SafeRead(sampleProperties + 0x88, &out.vecEnd);
    if (!out.vecBegin || !out.vecEnd || out.vecEnd <= out.vecBegin) {
        out.error = "invalid handle vector range"; return out;
    }
    out.vecSize = (out.vecEnd - out.vecBegin) / sizeof(uintptr_t);
    if (out.vecSize > 64) { out.error = "vector too large"; return out; }

    for (uint64_t i = 0; i < out.vecSize; ++i) {
        uintptr_t elem = 0;
        SafeRead(out.vecBegin + i * sizeof(uintptr_t), &elem);
        if (!elem) continue;
        HandlePathElement hpe{};
        hpe.elemAddr = elem;
        SafeRead(elem + 0x00, &hpe.field_00);
        SafeRead(elem + 0x08, &hpe.field_08);
        SafeRead(elem + 0x10, &hpe.field_10);
        SafeRead(elem + 0x18, &hpe.ptrAt18);
        SafeRead(elem + 0x20, &hpe.field_20);
        SafeRead(elem + 0x28, &hpe.field_28);
        SafeRead(elem + 0x30, &hpe.handle);
        SafeRead(elem + 0x34, &hpe.field_34);
        SafeRead(elem + 0x38, &hpe.field_38);
        SafeRead(elem + 0x40, &hpe.field_40);
        hpe.soundInModule = InModule(mod, hpe.ptrAt18);
        out.items.push_back(hpe);
    }
    out.ok = true;
    return out;
}

// ── FMOD API Resolver ─────────────────────────────────────────
// Find LEA RIP-relative instructions in .text referencing a given address.
// Pattern: 48/4C 8D ModRM[mod=0,rm=5] disp32
uintptr_t FindLeaRefTo(uintptr_t textStart, const std::vector<uint8_t>& textBytes,
                       uintptr_t targetAddr, uintptr_t* outFuncRva)
{
    if (outFuncRva) *outFuncRva = 0;
    uintptr_t textEnd = textStart + textBytes.size();
    for (size_t i = 0; i + 7 <= textBytes.size(); ++i) {
        uint8_t b0 = textBytes[i];
        if ((b0 != 0x48 && b0 != 0x4C) || textBytes[i+1] != 0x8D) continue;
        uint8_t modrm = textBytes[i+2];
        if ((modrm & 0xC7) != 0x05) continue;
        int32_t disp = *reinterpret_cast<const int32_t*>(&textBytes[i+3]);
        uintptr_t target = textStart + i + 7 + disp;
        if (target == targetAddr) {
            uintptr_t insnAddr = textStart + i;
            if (outFuncRva) *outFuncRva = i;
            return insnAddr;
        }
    }
    return 0;
}

bool LooksFunctionStart(const std::vector<uint8_t>& textBytes, size_t at) {
    if (at >= textBytes.size()) return false;
    auto match = [&](const uint8_t* pat, size_t plen) {
        return at + plen <= textBytes.size() && memcmp(&textBytes[at], pat, plen) == 0;
    };

    // Common MSVC x64 prologues seen in the FH executable and the reference mod.
    if (match((const uint8_t*)"\x48\x89\x5C\x24", 4)) return true;
    if (match((const uint8_t*)"\x48\x89\x6C\x24", 4)) return true;
    if (match((const uint8_t*)"\x48\x89\x74\x24", 4)) return true;
    if (match((const uint8_t*)"\x48\x89\x7C\x24", 4)) return true;
    if (match((const uint8_t*)"\x4C\x89\x44\x24", 4)) return true;
    if (match((const uint8_t*)"\x4C\x89\x4C\x24", 4)) return true;
    if (match((const uint8_t*)"\x4C\x89\x5C\x24", 4)) return true;
    if (match((const uint8_t*)"\x55\x48\x8B\xEC", 4)) return true;
    if (match((const uint8_t*)"\x40\x53", 2)) return true;
    if (match((const uint8_t*)"\x40\x55", 2)) return true;
    if (match((const uint8_t*)"\x40\x56", 2)) return true;
    if (match((const uint8_t*)"\x40\x57", 2)) return true;
    if (match((const uint8_t*)"\x48\x83\xEC", 3)) return true;
    if (match((const uint8_t*)"\x48\x81\xEC", 3)) return true;
    if (match((const uint8_t*)"\x4C\x8B\xDC", 3)) return true;
    if (match((const uint8_t*)"\x33\xD2", 2)) return true; // leaf helper after padding
    return false;
}

uintptr_t BacktrackFunctionStart(const std::vector<uint8_t>& textBytes, size_t insnOff) {
    if (insnOff > textBytes.size() || insnOff < 4) return 0;
    size_t lo = insnOff > 0x800 ? insnOff - 0x800 : 0;

    // First, prefer starts immediately after padding. This catches the FMOD
    // wrapper strip where functions are separated by CC runs, not just RET.
    for (size_t i = insnOff; i > lo; --i) {
        size_t cand = i;
        while (cand < insnOff && (textBytes[cand] == 0xCC || textBytes[cand] == 0x90)) ++cand;
        if (cand >= insnOff || cand == i) continue;
        if (LooksFunctionStart(textBytes, cand)) return cand;
    }

    // Fallback: scan backwards for a previous return, then skip padding.
    for (size_t i = insnOff - 1; i > lo; --i) {
        if (textBytes[i] != 0xC3 && textBytes[i] != 0xC2) continue;
        size_t cand = i + (textBytes[i] == 0xC2 ? 3 : 1);
        while (cand < insnOff && (textBytes[cand] == 0xCC || textBytes[cand] == 0x90)) ++cand;
        if (cand < insnOff && LooksFunctionStart(textBytes, cand)) return cand;
    }

    // Last resort: nearest plausible prologue in the search window.
    for (size_t i = insnOff; i > lo; --i) {
        if (LooksFunctionStart(textBytes, i)) return i;
    }
    return 0;
}

FmodApiResult ResolveFmodApis(const ModuleInfo& mod, uintptr_t systemI) {
    FmodApiResult out{};
    out.systemI = systemI;
    if (!mod.text.start || !mod.text.size) { out.error = "no .text"; return out; }

    // Read .rdata for string searching
    std::vector<uint8_t> rdataBytes(mod.rdata.size);
    if (!SafeReadBytes(mod.rdata.start, rdataBytes.data(), mod.rdata.size)) {
        out.error = "cannot read .rdata"; return out;
    }
    std::vector<uint8_t> textBytes(mod.text.size);
    if (!SafeReadBytes(mod.text.start, textBytes.data(), mod.text.size)) {
        out.error = "cannot read .text"; return out;
    }

    const char* apiNames[] = {
        "System::getMasterChannelGroup",
        "ChannelGroup::getNumChannels",
        "ChannelGroup::getChannel",
        "ChannelGroup::getNumGroups",
        "ChannelGroup::getGroup",
        "Channel::getCurrentSound",
        "Channel::setChannelGroup",
        "Channel::stop",
        "Channel::setVolume",
        "Channel::getChannelGroup",
        "System::createSound",
        "System::playSound",
        "System::createDSP",
        "ChannelControl::addDSP",
    };
    auto findInRdata = [&](const char* s) -> uintptr_t {
        size_t n = strlen(s);
        for (size_t i = 0; i + n <= mod.rdata.size; ++i) {
            if (memcmp(rdataBytes.data() + i, s, n) == 0) {
                if ((i == 0 || rdataBytes[i-1] == '\0') &&
                    (i + n >= mod.rdata.size || rdataBytes[i+n] == '\0')) {
                    return mod.rdata.start + i;
                }
            }
        }
        return 0;
    };

    for (const char* name : apiNames) {
        FmodApiEntry e;
        e.name = name;
        uintptr_t strAddr = findInRdata(name);
        if (!strAddr) { e.note = "string not in .rdata"; out.entries.push_back(e); continue; }
        e.stringRva = strAddr - mod.base;

        uintptr_t funcRva = 0;
        uintptr_t leaAddr = FindLeaRefTo(mod.text.start, textBytes, strAddr, &funcRva);
        if (!leaAddr) { e.note = "no LEA RIP-relative ref in .text"; out.entries.push_back(e); continue; }
        e.leaRva = leaAddr - mod.base;

        // Backtrack to find function start
        size_t insnOff = static_cast<size_t>(leaAddr - mod.text.start);
        uintptr_t funcOff = BacktrackFunctionStart(textBytes, insnOff);
        if (funcOff) {
            e.funcAddr = mod.text.start + funcOff;
            e.funcRva = e.funcAddr - mod.base;
        } else {
            // Fallback: use LEA address as reference
            e.funcRva = e.leaRva;
            e.funcAddr = leaAddr;
            e.note = "no prologue found; using LEA address";
        }
        e.found = true;
        out.entries.push_back(e);
    }
    out.ok = true;
    return out;
}

std::vector<NativePatternResult> ResolveNativePatterns(const ModuleInfo& mod) {
    std::vector<NativePatternResult> results;
    if (!mod.text.start || !mod.text.size) return results;

    std::vector<uint8_t> textBytes(mod.text.size);
    if (!SafeReadBytes(mod.text.start, textBytes.data(), mod.text.size)) return results;

    struct PatternSpec { const char* name; const char* pattern; };
    PatternSpec specs[] = {
        {"systemCreateDSP",
         "4C 8B DC 56 48 81 EC 70 01 00 00|40 53 55 56 57 41 56 48 81 EC 50 01 00 00"},
        {"dspRelease",
         "48 89 5C 24 10 57 48 81 EC 50 01 00 00"},
        {"channelControlAddDSP",
         "4C 8B DC 56 48 81 EC 70 01 00 00|40 53 55 56 57 41 56 48 81 EC 50 01 00 00"},
        {"channelControlRemoveDSP",
         "48 89 5C 24 18 48 89 74 24 20 57 48 81 EC 50 01 00 00"},
        {"fmod_handle_resolver",
         "48 89 6C 24 18 48 89 74 24 20 57 41 56 41 57 48 83 EC 20 8B F9 8B C1 C1 EF 11 49 8B F0 D1 E8 81 E7 FF 0F 00 00 0F B7 E8 4C 8B F2 4C 8B F9"},
        {"fmod_handle_unlock",
         "48 8B 89 F0 09 01 00 48 85 C9 0F 85 ?? ?? ?? ?? 33 C0 C3"},
        {"radio_set_station_by_name",
         "48 89 5C 24 18 56 57 41 57 48 83 EC 30 4C 8B F9 48 8B DA 48 8B 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 83 7B 18 0F"},
    };

    for (const auto& spec : specs) {
        NativePatternResult r;
        r.name = spec.name;
        r.pattern = spec.pattern;
        auto alts = SplitPatternAlternates(spec.pattern);
        for (size_t alt = 0; alt < alts.size(); ++alt) {
            auto hits = FindPatternAllInBytes(mod.text.start, textBytes, alts[alt].c_str(), 16);
            for (uintptr_t hit : hits) {
                NativePatternHit h;
                h.addr = hit;
                h.rva = hit - mod.base;
                h.alt = static_cast<int>(alt);
                size_t insnOff = static_cast<size_t>(hit - mod.text.start);
                uintptr_t funcOff = BacktrackFunctionStart(textBytes, insnOff);
                h.funcAddr = funcOff ? mod.text.start + funcOff : hit;
                h.funcRva = h.funcAddr - mod.base;
                r.hits.push_back(h);
            }
        }
        results.push_back(r);
    }
    return results;
}

std::vector<SystemIContextHit> ScanSystemIContexts(const ModuleInfo& mod, size_t maxHits) {
    std::vector<SystemIContextHit> hits;
    MEMORY_BASIC_INFORMATION mbi{};
    uintptr_t addr = 0;
    uint64_t regions = 0;
    uint64_t checked = 0;

    auto plausibleRange = [&](uintptr_t base, uintptr_t size) {
        if (!base || size < 0x1000 || size > 0x20000000) return false;
        if (base + size < base) return false;
        return InModule(mod, base) || InRange(mod.base, base, size) || InRange(mod.rdata.start, base, size);
    };

    auto plausibleVector = [](uintptr_t begin, uintptr_t end, uint64_t* count) {
        if (!begin || !end || end <= begin) return false;
        if ((begin & 7) || (end & 7)) return false;
        uint64_t n = (end - begin) / sizeof(uintptr_t);
        if (count) *count = n;
        return n > 0 && n <= 4096;
    };

    while (VirtualQuery(reinterpret_cast<LPCVOID>(addr), &mbi, sizeof(mbi)) == sizeof(mbi)) {
        uintptr_t base = reinterpret_cast<uintptr_t>(mbi.BaseAddress);
        uintptr_t end = base + mbi.RegionSize;
        DWORD prot = mbi.Protect & 0xff;
        bool readable = prot == PAGE_READONLY || prot == PAGE_READWRITE || prot == PAGE_WRITECOPY ||
                        prot == PAGE_EXECUTE_READ || prot == PAGE_EXECUTE_READWRITE || prot == PAGE_EXECUTE_WRITECOPY;
        bool skip = mbi.State != MEM_COMMIT || (mbi.Protect & PAGE_GUARD) || (mbi.Protect & PAGE_NOACCESS) ||
                    !readable || mbi.RegionSize > 0x4000000 || InRange(base, mod.base, mod.size);
        if (!skip) {
            ++regions;
            std::vector<uint8_t> bytes(mbi.RegionSize);
            if (SafeReadBytes(base, bytes.data(), mbi.RegionSize)) {
                uintptr_t p = (base + 7) & ~uintptr_t(7);
                uintptr_t scanEnd = end & ~uintptr_t(7);
                for (; p + 0x90 <= scanEnd; p += 8) {
                    ++checked;
                    size_t off = static_cast<size_t>(p - base);
                    uintptr_t rangeBase = *reinterpret_cast<const uintptr_t*>(bytes.data() + off + 0x58);
                    uintptr_t rangeSize = *reinterpret_cast<const uintptr_t*>(bytes.data() + off + 0x60);
                    uintptr_t vecBegin = *reinterpret_cast<const uintptr_t*>(bytes.data() + off + 0x80);
                    uintptr_t vecEnd = *reinterpret_cast<const uintptr_t*>(bytes.data() + off + 0x88);
                    uint64_t vecCount = 0;
                    if (!plausibleRange(rangeBase, rangeSize)) continue;
                    if (!plausibleVector(vecBegin, vecEnd, &vecCount)) continue;

                    uint64_t limit = std::min<uint64_t>(vecCount, 512);
                    for (uint64_t i = 0; i < limit; ++i) {
                        uintptr_t elem = 0, p1 = 0, systemI = 0, vtable = 0;
                        uintptr_t slot = vecBegin + i * sizeof(uintptr_t);
                        if (!SafeRead(slot, &elem) || !elem) continue;
                        if (!SafeRead(elem + 0x18, &p1) || !p1) continue;
                        if (!SafeRead(p1 + 0xC0, &systemI) || !systemI) continue;
                        if (!SafeRead(systemI, &vtable) || !vtable) continue;
                        if (!InRange(vtable, rangeBase, static_cast<size_t>(rangeSize))) continue;
                        if (!InModule(mod, vtable)) continue;

                        SystemIContextHit h;
                        h.ctx = p;
                        h.rangeBase = rangeBase;
                        h.rangeSize = rangeSize;
                        h.vecBegin = vecBegin;
                        h.vecEnd = vecEnd;
                        h.vecCount = vecCount;
                        h.slot = slot;
                        h.elem = elem;
                        h.p1 = p1;
                        h.systemI = systemI;
                        h.vtable = vtable;
                        h.index = static_cast<int>(i);
                        bool prefixOpen = true;
                        for (int vi = 0; vi < 64; ++vi) {
                            uintptr_t entry = 0;
                            if (!SafeRead(vtable + vi * sizeof(uintptr_t), &entry)) break;
                            bool text = InText(mod, entry);
                            bool ascii = LooksAsciiQword(entry);
                            if (text) ++h.textEntries;
                            else if (InRdata(mod, entry)) ++h.rdataEntries;
                            else if (InData(mod, entry)) ++h.dataEntries;
                            else if (InModule(mod, entry)) ++h.moduleEntries;
                            else ++h.nonModuleEntries;
                            if (ascii) ++h.asciiLikeEntries;
                            if (prefixOpen && text) {
                                ++h.prefixTextEntries;
                            } else if (prefixOpen) {
                                h.firstBadIndex = vi;
                                prefixOpen = false;
                            }
                        }
                        h.score = h.textEntries * 4 + h.prefixTextEntries * 6 -
                                  h.rdataEntries * 2 - h.dataEntries * 2 -
                                  h.moduleEntries - h.nonModuleEntries - h.asciiLikeEntries * 4;
                        hits.push_back(h);
                        if (hits.size() >= maxHits) {
                            AppendLog("systemi context scan early stop: hits=%llu regions=%llu checked=%llu",
                                static_cast<unsigned long long>(hits.size()),
                                static_cast<unsigned long long>(regions),
                                static_cast<unsigned long long>(checked));
                            return hits;
                        }
                    }
                }
            }
        }
        if (end <= addr) break;
        addr = end;
    }
    AppendLog("systemi context scan done: hits=%llu regions=%llu checked=%llu",
        static_cast<unsigned long long>(hits.size()),
        static_cast<unsigned long long>(regions),
        static_cast<unsigned long long>(checked));
    return hits;
}

// ── SystemI Resolver ──────────────────────────────────────────
// Probe multiple offsets from fmod_sound looking for a pointer whose vtable
// falls within the exe module. Returns the first match.
// Offsets tried: 0x08, 0x10, 0x18, 0x20, 0x28, 0x40, 0x48 (FMOD SoundI fields)
// Reports which offset matched for diagnostic purposes.
struct SystemICandidate {
    uintptr_t systemI = 0;
    int offset = 0;
    bool found = false;
};
SystemICandidate FindSystemICandidates(uintptr_t fmodSound, const ModuleInfo& mod,
                                       std::string* diagOut)
{
    SystemICandidate out;
    if (!fmodSound) { if (diagOut) *diagOut = "no fmod_sound"; return out; }

    int offsets[] = {0x08, 0x10, 0x18, 0x20, 0x28, 0x40, 0x48};
    std::string diag;
    for (int off : offsets) {
        uintptr_t p = 0;
        if (!SafeRead(fmodSound + off, &p) || !p) {
            diag += "off+" + std::to_string(off) + "=null ";
            continue;
        }
        uintptr_t candidateVt = 0;
        if (!SafeRead(p, &candidateVt)) {
            diag += "off+" + std::to_string(off) + "=unreadable ";
            continue;
        }
        if (!InModule(mod, candidateVt)) {
            diag += "off+" + std::to_string(off) + "=vt_not_in_mod ";
            continue;
        }
        out.systemI = p;
        out.offset = off;
        out.found = true;
        diag += "off+" + std::to_string(off) + "=HIT ";
        break;
    }
    if (diagOut) *diagOut = diag;
    return out;
}

// ── Topology Walk ──────────────────────────────────────────────
// Step 1: Find SystemI from fmod_sound + 0x18 (pure read)
// Step 2: Read SystemI vtable
// Step 3: Range-match resolve-api LEA addresses against vtable entries
// Step 4: Report vtable offsets for getMasterChannelGroup / getChannel / getCurrentSound
TopologyDump RunTopologyWalk(const ModuleInfo& mod, uintptr_t targetSound,
                              const FmodApiResult& api)
{
    TopologyDump out{};
    out.targetSound = targetSound;

    if (!targetSound) { out.error = "no target fmod_sound"; return out; }

    // Step 1: Find SystemI via multi-offset probe
    std::string sysDiag;
    auto sysCand = FindSystemICandidates(targetSound, mod, &sysDiag);
    out.log.push_back("FindSystemI: " + sysDiag);
    out.systemI = sysCand.systemI;
    if (!sysCand.found) { out.error = "FindSystemI failed: " + sysDiag; return out; }

    // Step 2: Read SystemI vtable
    uintptr_t sysPtr = sysCand.systemI;
    uintptr_t systemVtbl = 0;
    if (!SafeRead(sysPtr, &systemVtbl)) {
        out.error = "cannot read SystemI vtable"; return out;
    }
    if (!InModule(mod, systemVtbl)) {
        out.error = "SystemI vtable not in module"; return out;
    }

    // Step 3: Resolve which vtable entries match known FMOD API addresses
    struct ApiLookup { std::string name; uintptr_t leaAddr; bool found; };
    ApiLookup apis[] = {
        {"System::getMasterChannelGroup", 0, false},
        {"ChannelGroup::getChannel", 0, false},
        {"Channel::getCurrentSound", 0, false},
        {"ChannelGroup::getNumGroups", 0, false},
        {"ChannelGroup::getGroup", 0, false},
        {"Channel::stop", 0, false},
    };
    for (auto& a : apis) {
        for (auto& e : api.entries) {
            if (e.name == a.name && e.found) {
                a.leaAddr = e.funcAddr;
                a.found = true;
                break;
            }
        }
    }

    // Dump vtable header for diagnostics
    out.log.push_back("SystemI=" + Hex(sysPtr) + " vtbl=" + Hex(systemVtbl) + " vtbl_in_mod=" + (InModule(mod, systemVtbl) ? "yes" : "no"));

    // Log resolve-api addresses being matched
    std::string apiInfo;
    for (auto& a : apis) {
        if (a.found) {
            apiInfo += a.name + "@" + Hex(a.leaAddr) + " ";
        }
    }
    out.log.push_back("resolve targets: " + apiInfo);

    // Log first 8 vtable entries for manual inspection
    std::string vtSample;
    for (int i = 0; i < 8; ++i) {
        uintptr_t entry = 0;
        if (!SafeRead(systemVtbl + i * 8, &entry)) { vtSample += "[END]"; break; }
        vtSample += "[" + std::to_string(i) + "]=" + Hex(entry);
        if (InModule(mod, entry)) vtSample += "(mod)";
        vtSample += " ";
    }
    out.log.push_back("vtbl[:8]: " + vtSample);

    // Scan vtable entries with range matching
    const uintptr_t MATCH_THRESHOLD = 0x1000;
    struct VtMatch { int idx; uintptr_t entry; std::string name; };
    std::vector<VtMatch> matches;

    for (int i = 0; i < 512; ++i) {
        uintptr_t entry = 0;
        if (!SafeRead(systemVtbl + i * 8, &entry)) {
            out.log.push_back("vtbl read stop at idx=" + std::to_string(i));
            break;
        }
        if (!InModule(mod, entry)) continue;
        for (auto& a : apis) {
            if (!a.found) continue;
            uintptr_t diff = (entry > a.leaAddr) ? (entry - a.leaAddr) : (a.leaAddr - entry);
            if (diff < MATCH_THRESHOLD) {
                matches.push_back({i, entry, a.name});
            }
        }
    }

    // Log all matches
    for (auto& m : matches) {
        std::string line = "HIT: " + m.name + " → vtbl[" + std::to_string(m.idx) +
                          "] off=" + Hex(m.idx * 8) +
                          " entry=" + Hex(m.entry);
        out.log.push_back(line);
    }
    out.groupsSeen = static_cast<int>(matches.size());

    if (matches.empty()) {
        out.error = "no resolve-api matches in SystemI vtable (vtbl entries checked, none within 0x1000 of resolved funcAddr)";
        return out;
    }

    // Step 4: Report the key offsets
    out.ok = true;
    out.masterCg = sysPtr;
    for (auto& m : matches) {
        if (m.name == "System::getMasterChannelGroup") {
            // Master CG can be read from vtable[gmcgIdx] but we don't call it yet
            // Just report the offset
        }
    }
    return out;
}

std::string RunScan(const char* commandName) {
    AppendLog("control command: %s", commandName);

    ModuleInfo mod{};
    RttiResult rtti{};
    RadioStateResult radio{};
    std::vector<Candidate> candidates;
    std::string error;

    if (GetMainModuleInfo(&mod)) {
        AppendLog("scan: module base=%s size=%llu", Hex(mod.base).c_str(),
            static_cast<unsigned long long>(mod.size));
        rtti = ResolveRefcountRtti(mod);
        AppendLog("scan: rtti ok=%d typedesc=%s col=%s vtable=%s",
            rtti.ok ? 1 : 0, Hex(rtti.typeDesc).c_str(), Hex(rtti.col).c_str(), Hex(rtti.vtable).c_str());
        radio = ResolveRadioState(mod);
        AppendLog("scan: radio_state sig=%d chain=%d active_streamer=%d station='%s'",
            radio.sigFound ? 1 : 0, radio.chainOk ? 1 : 0,
            radio.activeStreamerMode ? 1 : 0, radio.stationName.value.c_str());
        candidates = ScanHeapCandidates(mod, rtti.vtable);
        AppendLog("scan: candidates=%llu", static_cast<unsigned long long>(candidates.size()));
    } else {
        error = "GetMainModuleInfo failed";
        AppendLog("scan: %s", error.c_str());
    }

    std::lock_guard<std::mutex> lock(g_stateMutex);
    g_state.mod = mod;
    g_state.rtti = rtti;
    g_state.radio = radio;
    g_state.candidates = candidates;
    g_state.scanCount++;
    g_state.lastScanTick = GetTickCount64();
    g_state.phase = error.empty() ? "scanned" : "error";
    g_state.lastCommand = commandName;
    g_state.lastError = error;
    WriteProbeJson(g_state.mod, g_state.rtti, g_state.radio, g_state.candidates, g_state.scanCount);
    return BuildStatusJson(g_state);
}

Candidate ReadCandidateLive(const Candidate& c) {
    Candidate out = c;
    if (out.sampleProperties) {
        out.soundName = ReadMsvcString(out.sampleProperties + 0x10);
        out.displayName = ReadMsvcString(out.sampleProperties + 0x30);
        out.artist = ReadMsvcString(out.sampleProperties + 0x50);
    }
    return out;
}

std::string ApplyMetadata(const std::string& title, const std::string& artist, const std::string& soundName) {
    Candidate target{};
    {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        if (!g_state.candidates.empty()) target = g_state.candidates.front();
    }

    if (!target.sampleProperties) {
        RunScan("meta-autoscan");
        std::lock_guard<std::mutex> lock(g_stateMutex);
        if (!g_state.candidates.empty()) target = g_state.candidates.front();
    }

    std::string error;
    bool ok = target.sampleProperties != 0;
    if (!ok) error = "no live SampleProperties candidate; call /scan while in-game and on R10";

    if (ok) {
        Candidate live = ReadCandidateLive(target);
        {
            std::lock_guard<std::mutex> lock(g_stateMutex);
            if (!g_state.haveOriginalMeta || g_state.originalSampleProperties != live.sampleProperties) {
                g_state.haveOriginalMeta = true;
                g_state.originalSampleProperties = live.sampleProperties;
                g_state.originalSoundName = live.soundName.value;
                g_state.originalDisplayName = live.displayName.value;
                g_state.originalArtist = live.artist.value;
            }
        }

        if (!title.empty()) {
            ok = WriteMsvcString(live.sampleProperties + 0x30, title, &error);
        }
        if (ok && !artist.empty()) {
            ok = WriteMsvcString(live.sampleProperties + 0x50, artist, &error);
        }
        if (ok && !soundName.empty()) {
            ok = WriteMsvcString(live.sampleProperties + 0x10, soundName, &error);
        }
    }

    std::lock_guard<std::mutex> lock(g_stateMutex);
    g_state.phase = ok ? "metadata_written" : "error";
    g_state.lastCommand = "meta";
    g_state.lastError = error;
    if (!g_state.candidates.empty()) {
        g_state.candidates.front() = ReadCandidateLive(g_state.candidates.front());
    }
    AppendLog("meta: ok=%d title='%s' artist='%s' sound='%s' error='%s'",
        ok ? 1 : 0, title.c_str(), artist.c_str(), soundName.c_str(), error.c_str());
    return BuildStatusJson(g_state);
}

std::string RestoreMetadata() {
    std::lock_guard<std::mutex> lock(g_stateMutex);
    bool ok = g_state.haveOriginalMeta && g_state.originalSampleProperties;
    std::string error;
    if (!ok) {
        error = "no original metadata saved";
    } else {
        ok = WriteMsvcString(g_state.originalSampleProperties + 0x10, g_state.originalSoundName, &error);
        if (ok) ok = WriteMsvcString(g_state.originalSampleProperties + 0x30, g_state.originalDisplayName, &error);
        if (ok) ok = WriteMsvcString(g_state.originalSampleProperties + 0x50, g_state.originalArtist, &error);
    }
    g_state.phase = ok ? "metadata_restored" : "error";
    g_state.lastCommand = "meta_restore";
    g_state.lastError = error;
    if (!g_state.candidates.empty()) {
        g_state.candidates.front() = ReadCandidateLive(g_state.candidates.front());
    }
    AppendLog("meta_restore: ok=%d error='%s'", ok ? 1 : 0, error.c_str());
    return BuildStatusJson(g_state);
}

std::string MarkPendingCommand(const std::string& command, const std::string& reason) {
    std::lock_guard<std::mutex> lock(g_stateMutex);
    g_state.phase = "pending_fmod_bridge";
    g_state.lastCommand = command;
    g_state.lastError = reason;
    AppendLog("pending command: %s (%s)", command.c_str(), reason.c_str());
    return BuildStatusJson(g_state);
}

// ── Lock / Live / Unlock ───────────────────────────────────────
std::string CmdLock() {
    std::lock_guard<std::mutex> lock(g_stateMutex);
    if (g_state.candidates.empty()) {
        g_state.lastCommand = "lock";
        g_state.lastError = "no candidates; call /scan first";
        return BuildStatusJson(g_state);
    }
    const auto& best = g_state.candidates.front();
    g_state.lock.locked = true;
    g_state.lock.refcount = best.refcount;
    g_state.lock.fmodSound = best.fmodSound;
    g_state.lock.propsOwner = best.propsOwner;
    g_state.lock.sampleProperties = best.sampleProperties;
    g_state.lock.handle32 = best.handle32;
    g_state.lock.sinceTick = GetTickCount64();
    g_state.lock.liveStatus = "locked";
    g_state.lastCommand = "lock";
    g_state.lastError.clear();
    AppendLog("lock: refcount=%s fmod_sound=%s sample_properties=%s",
        Hex(best.refcount).c_str(), Hex(best.fmodSound).c_str(), Hex(best.sampleProperties).c_str());
    return BuildStatusJson(g_state);
}

std::string CmdUnlock() {
    std::lock_guard<std::mutex> lock(g_stateMutex);
    g_state.lock = LockState{};
    g_state.lastCommand = "unlock";
    g_state.lastError.clear();
    return BuildStatusJson(g_state);
}

std::string CmdLive() {
    std::lock_guard<std::mutex> lock(g_stateMutex);
    if (!g_state.lock.locked || !g_state.lock.refcount) {
        g_state.lastCommand = "live";
        g_state.lastError = "no lock; call /lock first";
        return BuildStatusJson(g_state);
    }

    // Re-read uses for alive check
    int32_t uses = 0, weaks = 0;
    SafeRead(g_state.lock.refcount + 0x08, &uses);
    SafeRead(g_state.lock.refcount + 0x0C, &weaks);
    bool alive = (uses > 0 && uses <= 0x80 && weaks > 0 && weaks <= 0x80);
    g_state.lock.liveStatus = alive ? "alive" : "stale";

    // Read current pointers
    uintptr_t curSound = 0, curOwner = 0, curProps = 0;
    uint32_t curHandle = 0;
    SafeRead(g_state.lock.refcount + 0x18, &curSound);
    SafeRead(g_state.lock.refcount + 0x30, &curHandle);
    SafeRead(g_state.lock.refcount + 0x58, &curOwner);
    if (curOwner) SafeRead(curOwner + 0x18, &curProps);

    // Save old values BEFORE updating state
    uintptr_t oldSound = g_state.lock.fmodSound;
    uintptr_t oldOwner = g_state.lock.propsOwner;
    uintptr_t oldProps = g_state.lock.sampleProperties;
    uint32_t oldHandle = g_state.lock.handle32;

    // Read metadata (always from current props)
    Candidate live{};
    live.refcount = g_state.lock.refcount;
    live.fmodSound = curSound;
    live.propsOwner = curOwner;
    live.sampleProperties = curProps;
    if (curProps) {
        live.soundName = ReadMsvcString(curProps + 0x10);
        live.displayName = ReadMsvcString(curProps + 0x30);
        live.artist = ReadMsvcString(curProps + 0x50);
    }

    // Update state with live values (AFTER old values saved)
    g_state.lock.fmodSound = curSound;
    g_state.lock.propsOwner = curOwner;
    g_state.lock.sampleProperties = curProps;
    g_state.lock.handle32 = curHandle;
    g_state.lastCommand = "live";
    g_state.lastError.clear();

    bool songChanged = (curSound != oldSound) || (curOwner != oldOwner) || (curProps != oldProps);

    // Build a compact response
    std::ostringstream os;
    os << "{"
       << "\"command\":\"live\","
       << "\"live_status\":\"" << JsonEscape(g_state.lock.liveStatus) << "\","
       << "\"uses\":" << uses << ",\"weaks\":" << weaks << ","
        << "\"fmod_sound\":\"" << Hex(curSound) << "\","
        << "\"old_fmod_sound\":\"" << Hex(oldSound) << "\","
        << "\"handle32\":" << curHandle << ","
        << "\"old_handle32\":" << oldHandle << ","
        << "\"sample_properties\":\"" << Hex(curProps) << "\","
       << "\"old_sample_properties\":\"" << Hex(oldProps) << "\","
       << "\"song_changed\":" << (songChanged ? "true" : "false") << ",";
    if (live.soundName.ok)
        os << "\"sound_name\":\"" << JsonEscape(live.soundName.value) << "\",";
    if (live.displayName.ok)
        os << "\"display_name\":\"" << JsonEscape(live.displayName.value) << "\",";
    if (live.artist.ok)
        os << "\"artist\":\"" << JsonEscape(live.artist.value) << "\",";
    os << "\"module\":{" << "\"base\":\"" << Hex(g_state.mod.base) << "\"}"
       << "}\n";
    return os.str();
}

// ── Handle Path ────────────────────────────────────────────────
bool EnsureModuleForCommand(ModuleInfo* out, std::string* error) {
    {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        if (g_state.mod.base) {
            *out = g_state.mod;
            return true;
        }
    }
    ModuleInfo mod{};
    if (!GetMainModuleInfo(&mod)) {
        if (error) *error = "GetMainModuleInfo failed; call /scan first";
        return false;
    }
    {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        g_state.mod = mod;
    }
    *out = mod;
    return true;
}

std::string NativePatternsJson(const std::vector<NativePatternResult>& results) {
    std::ostringstream os;
    os << "[";
    for (size_t i = 0; i < results.size(); ++i) {
        if (i) os << ",";
        const auto& r = results[i];
        os << "{"
           << "\"name\":\"" << JsonEscape(r.name) << "\","
           << "\"pattern\":\"" << JsonEscape(r.pattern) << "\","
           << "\"hit_count\":" << r.hits.size() << ","
           << "\"hits\":[";
        for (size_t j = 0; j < r.hits.size(); ++j) {
            if (j) os << ",";
            const auto& h = r.hits[j];
            os << "{"
               << "\"addr\":\"" << Hex(h.addr) << "\","
               << "\"rva\":\"0x" << std::hex << std::uppercase << h.rva << std::dec << "\","
               << "\"func_addr\":\"" << Hex(h.funcAddr) << "\","
               << "\"func_rva\":\"0x" << std::hex << std::uppercase << h.funcRva << std::dec << "\","
               << "\"alt\":" << h.alt
               << "}";
        }
        os << "]}";
    }
    os << "]";
    return os.str();
}

std::string SystemIHitsJson(const std::vector<SystemIContextHit>& hits, const ModuleInfo& mod) {
    std::ostringstream os;
    os << "[";
    for (size_t i = 0; i < hits.size(); ++i) {
        if (i) os << ",";
        const auto& h = hits[i];
        os << "{"
           << "\"ctx\":\"" << Hex(h.ctx) << "\","
           << "\"range_base\":\"" << Hex(h.rangeBase) << "\","
           << "\"range_size\":\"" << Hex(h.rangeSize) << "\","
           << "\"vec_begin\":\"" << Hex(h.vecBegin) << "\","
           << "\"vec_end\":\"" << Hex(h.vecEnd) << "\","
           << "\"vec_count\":" << h.vecCount << ","
           << "\"index\":" << h.index << ","
           << "\"slot\":\"" << Hex(h.slot) << "\","
           << "\"elem\":\"" << Hex(h.elem) << "\","
           << "\"p1\":\"" << Hex(h.p1) << "\","
           << "\"systemI\":\"" << Hex(h.systemI) << "\","
           << "\"vtable\":\"" << Hex(h.vtable) << "\","
           << "\"vtable_rva\":\"0x" << std::hex << std::uppercase << (h.vtable - mod.base) << std::dec << "\","
           << "\"vtable_section\":\"" << SectionName(mod, h.vtable) << "\","
           << "\"score\":" << h.score << ","
           << "\"entry64\":{"
           << "\"text\":" << h.textEntries << ","
           << "\"rdata\":" << h.rdataEntries << ","
           << "\"data\":" << h.dataEntries << ","
           << "\"image_other\":" << h.moduleEntries << ","
           << "\"external\":" << h.nonModuleEntries << ","
           << "\"ascii_like\":" << h.asciiLikeEntries << ","
           << "\"prefix_text\":" << h.prefixTextEntries << ","
           << "\"first_bad_index\":" << h.firstBadIndex
           << "}"
           << "}";
    }
    os << "]";
    return os.str();
}

std::string CmdResolveNativePatterns() {
    ModuleInfo mod{};
    std::string error;
    if (!EnsureModuleForCommand(&mod, &error)) {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        g_state.lastCommand = "fmod/resolve-native-patterns";
        g_state.lastError = error;
        return BuildStatusJson(g_state);
    }

    auto results = ResolveNativePatterns(mod);
    {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        g_state.nativePatterns = results;
        g_state.lastCommand = "fmod/resolve-native-patterns";
        g_state.lastError.clear();
    }
    AppendLog("native-patterns: specs=%llu", static_cast<unsigned long long>(results.size()));

    std::ostringstream os;
    os << "{"
       << "\"command\":\"fmod/resolve-native-patterns\","
       << "\"module_base\":\"" << Hex(mod.base) << "\","
       << "\"patterns\":" << NativePatternsJson(results)
       << "}\n";
    return os.str();
}

std::string CmdFindSystemIContexts(size_t maxHits) {
    ModuleInfo mod{};
    std::string error;
    if (!EnsureModuleForCommand(&mod, &error)) {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        g_state.lastCommand = "fmod/find-systemi-contexts";
        g_state.lastError = error;
        return BuildStatusJson(g_state);
    }

    auto hits = ScanSystemIContexts(mod, maxHits);
    {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        g_state.systemIHits = hits;
        g_state.lastCommand = "fmod/find-systemi-contexts";
        g_state.lastError = hits.empty() ? "no SystemI context candidates found" : "";
    }

    std::ostringstream os;
    os << "{"
       << "\"command\":\"fmod/find-systemi-contexts\","
       << "\"module_base\":\"" << Hex(mod.base) << "\","
       << "\"hit_count\":" << hits.size() << ","
       << "\"hits\":" << SystemIHitsJson(hits, mod) << ","
       << "\"error\":\"" << (hits.empty() ? "no SystemI context candidates found" : "") << "\""
       << "}\n";
    return os.str();
}

std::string CmdFindSystemIContextsStrict(size_t maxHits) {
    ModuleInfo mod{};
    std::string error;
    if (!EnsureModuleForCommand(&mod, &error)) {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        g_state.lastCommand = "fmod/find-systemi-contexts-strict";
        g_state.lastError = error;
        return BuildStatusJson(g_state);
    }

    auto raw = ScanSystemIContexts(mod, std::max<size_t>(maxHits * 8, 128));
    std::vector<SystemIContextHit> strict;
    for (const auto& h : raw) {
        // A real C++ vtable should live in read-only data and begin with a
        // substantial run of code pointers. Data may follow after the table ends.
        if (!InRdata(mod, h.vtable)) continue;
        if (h.prefixTextEntries < 12 && h.textEntries < 32) continue;
        if (h.score < 120) continue;
        strict.push_back(h);
        if (strict.size() >= maxHits) break;
    }
    {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        g_state.systemIHits = strict.empty() ? raw : strict;
        g_state.lastCommand = "fmod/find-systemi-contexts-strict";
        g_state.lastError = strict.empty() ? "no strict SystemI candidates; raw candidates saved for comparison" : "";
    }

    std::ostringstream os;
    os << "{"
       << "\"command\":\"fmod/find-systemi-contexts-strict\","
       << "\"module_base\":\"" << Hex(mod.base) << "\","
       << "\"raw_count\":" << raw.size() << ","
       << "\"strict_count\":" << strict.size() << ","
       << "\"strict_hits\":" << SystemIHitsJson(strict, mod) << ","
       << "\"raw_hits\":" << SystemIHitsJson(raw, mod) << ","
       << "\"error\":\"" << (strict.empty() ? "no strict SystemI candidates; raw candidates saved for comparison" : "") << "\""
       << "}\n";
    return os.str();
}

std::string CmdSystemIVtable(uintptr_t systemI, int count) {
    ModuleInfo mod{};
    std::string error;
    if (!EnsureModuleForCommand(&mod, &error)) {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        g_state.lastCommand = "fmod/systemi-vtable";
        g_state.lastError = error;
        return BuildStatusJson(g_state);
    }
    if (!systemI) {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        if (!g_state.systemIHits.empty()) systemI = g_state.systemIHits.front().systemI;
    }
    if (count <= 0) count = 128;
    if (count > 512) count = 512;

    uintptr_t vtable = 0;
    if (!systemI || !SafeRead(systemI, &vtable)) {
        return "{\"command\":\"fmod/systemi-vtable\",\"error\":\"missing or unreadable systemI\"}\n";
    }

    FmodApiResult api = ResolveFmodApis(mod, systemI);
    std::ostringstream os;
    os << "{"
       << "\"command\":\"fmod/systemi-vtable\","
       << "\"systemI\":\"" << Hex(systemI) << "\","
       << "\"vtable\":\"" << Hex(vtable) << "\","
       << "\"vtable_in_module\":" << (InModule(mod, vtable) ? "true" : "false") << ","
       << "\"entries\":[";
    for (int i = 0; i < count; ++i) {
        uintptr_t entry = 0;
        if (!SafeRead(vtable + i * sizeof(uintptr_t), &entry)) break;
        if (i) os << ",";
        os << "{"
           << "\"idx\":" << i << ","
           << "\"off\":\"0x" << std::hex << std::uppercase << (i * 8) << std::dec << "\","
           << "\"addr\":\"" << Hex(entry) << "\","
           << "\"in_module\":" << (InModule(mod, entry) ? "true" : "false") << ","
           << "\"section\":\"" << SectionName(mod, entry) << "\","
           << "\"ascii_like\":" << (LooksAsciiQword(entry) ? "true" : "false");
        if (InModule(mod, entry)) {
            os << ",\"rva\":\"0x" << std::hex << std::uppercase << (entry - mod.base) << std::dec << "\"";
        }
        std::string nearNames;
        for (const auto& e : api.entries) {
            if (!e.found || !e.funcAddr) continue;
            uintptr_t diff = entry > e.funcAddr ? entry - e.funcAddr : e.funcAddr - entry;
            if (diff < 0x1000) {
                if (!nearNames.empty()) nearNames += ",";
                nearNames += e.name + "@" + Hex(e.funcAddr);
            }
        }
        if (!nearNames.empty()) os << ",\"near\":\"" << JsonEscape(nearNames) << "\"";
        os << "}";
    }
    os << "],\"api_error\":\"" << JsonEscape(api.error) << "\""
       << "}\n";

    std::lock_guard<std::mutex> lock(g_stateMutex);
    g_state.lastCommand = "fmod/systemi-vtable";
    g_state.lastError.clear();
    return os.str();
}

std::string CmdNativeXrefs(const std::string& name, uintptr_t targetRva) {
    ModuleInfo mod{};
    std::string error;
    if (!EnsureModuleForCommand(&mod, &error)) {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        g_state.lastCommand = "fmod/native-xrefs";
        g_state.lastError = error;
        return BuildStatusJson(g_state);
    }

    uintptr_t target = targetRva ? mod.base + targetRva : 0;
    std::vector<NativePatternResult> patterns;
    if (!target && !name.empty()) {
        patterns = ResolveNativePatterns(mod);
        for (const auto& r : patterns) {
            if (r.name != name || r.hits.empty()) continue;
            target = r.hits.front().addr;
            targetRva = r.hits.front().rva;
            break;
        }
    }
    if (!target) {
        return "{\"command\":\"fmod/native-xrefs\",\"error\":\"no target; pass name or rva\"}\n";
    }

    std::vector<uint8_t> textBytes(mod.text.size);
    if (!SafeReadBytes(mod.text.start, textBytes.data(), mod.text.size)) {
        return "{\"command\":\"fmod/native-xrefs\",\"error\":\"cannot read .text\"}\n";
    }

    struct CallRef { uintptr_t addr; uintptr_t func; };
    std::vector<CallRef> calls;
    for (size_t i = 0; i + 5 <= textBytes.size(); ++i) {
        if (textBytes[i] != 0xE8) continue;
        int32_t disp = *reinterpret_cast<const int32_t*>(&textBytes[i + 1]);
        uintptr_t dest = mod.text.start + i + 5 + disp;
        if (dest != target) continue;
        uintptr_t funcOff = BacktrackFunctionStart(textBytes, i);
        calls.push_back({mod.text.start + i, funcOff ? mod.text.start + funcOff : 0});
        if (calls.size() >= 128) break;
    }

    struct PostCall {
        uintptr_t addr = 0;
        std::string kind;
        uintptr_t target = 0;
        uint32_t vtableOffset = 0;
    };
    auto postCallsNear = [&](uintptr_t callAddr) {
        std::vector<PostCall> out;
        size_t off = static_cast<size_t>(callAddr - mod.text.start);
        size_t end = (std::min)(textBytes.size(), off + 0xa0);
        for (size_t p = off + 5; p + 6 <= end; ++p) {
            if (textBytes[p] == 0xE8 && p + 5 <= textBytes.size()) {
                int32_t disp = *reinterpret_cast<const int32_t*>(&textBytes[p + 1]);
                PostCall c;
                c.addr = mod.text.start + p;
                c.kind = "direct";
                c.target = mod.text.start + p + 5 + disp;
                out.push_back(c);
            } else if (textBytes[p] == 0xFF && p + 6 <= textBytes.size() &&
                       (textBytes[p + 1] == 0x90 || textBytes[p + 1] == 0x91)) {
                PostCall c;
                c.addr = mod.text.start + p;
                c.kind = "vcall";
                c.vtableOffset = *reinterpret_cast<const uint32_t*>(&textBytes[p + 2]);
                out.push_back(c);
            } else if (p + 7 <= textBytes.size() &&
                       (textBytes[p] == 0x48 || textBytes[p] == 0x49) &&
                       textBytes[p + 1] == 0xFF &&
                       (textBytes[p + 2] == 0xA0 || textBytes[p + 2] == 0xA1)) {
                PostCall c;
                c.addr = mod.text.start + p;
                c.kind = "vjmp";
                c.vtableOffset = *reinterpret_cast<const uint32_t*>(&textBytes[p + 3]);
                out.push_back(c);
            } else if (p + 3 <= textBytes.size() && textBytes[p] == 0xFF &&
                       (textBytes[p + 1] == 0x50 || textBytes[p + 1] == 0x51)) {
                PostCall c;
                c.addr = mod.text.start + p;
                c.kind = "vcall";
                c.vtableOffset = textBytes[p + 2];
                out.push_back(c);
            } else if (p + 4 <= textBytes.size() &&
                       (textBytes[p] == 0x48 || textBytes[p] == 0x49) &&
                       textBytes[p + 1] == 0xFF &&
                       (textBytes[p + 2] == 0x60 || textBytes[p + 2] == 0x61)) {
                PostCall c;
                c.addr = mod.text.start + p;
                c.kind = "vjmp";
                c.vtableOffset = textBytes[p + 3];
                out.push_back(c);
            }
            if (out.size() >= 8) break;
        }
        return out;
    };

    auto qwordRefsIn = [&](uintptr_t start, size_t size) {
        std::vector<uintptr_t> refs;
        if (!start || !size) return refs;
        std::vector<uint8_t> bytes(size);
        if (!SafeReadBytes(start, bytes.data(), size)) return refs;
        for (size_t i = 0; i + sizeof(uintptr_t) <= size; i += 8) {
            uintptr_t v = *reinterpret_cast<const uintptr_t*>(bytes.data() + i);
            if (v == target) {
                refs.push_back(start + i);
                if (refs.size() >= 128) break;
            }
        }
        return refs;
    };

    auto rdataRefs = qwordRefsIn(mod.rdata.start, mod.rdata.size);
    auto dataRefs = qwordRefsIn(mod.data.start, mod.data.size);

    std::ostringstream os;
    os << "{"
       << "\"command\":\"fmod/native-xrefs\","
       << "\"name\":\"" << JsonEscape(name) << "\","
       << "\"target\":\"" << Hex(target) << "\","
       << "\"target_rva\":\"0x" << std::hex << std::uppercase << targetRva << std::dec << "\","
       << "\"direct_call_count\":" << calls.size() << ","
       << "\"direct_calls\":[";
    for (size_t i = 0; i < calls.size(); ++i) {
        if (i) os << ",";
        uintptr_t winStart = calls[i].addr > 0x30 ? calls[i].addr - 0x30 : calls[i].addr;
        os << "{"
           << "\"addr\":\"" << Hex(calls[i].addr) << "\","
           << "\"rva\":\"0x" << std::hex << std::uppercase << (calls[i].addr - mod.base) << std::dec << "\","
           << "\"func\":\"" << Hex(calls[i].func) << "\","
           << "\"func_rva\":\"0x" << std::hex << std::uppercase << (calls[i].func ? calls[i].func - mod.base : 0) << std::dec << "\","
            << "\"window_start\":\"" << Hex(winStart) << "\","
           << "\"window\":\"" << HexBytes(winStart, 0x80) << "\","
           << "\"post_calls\":[";
        auto post = postCallsNear(calls[i].addr);
        for (size_t j = 0; j < post.size(); ++j) {
            if (j) os << ",";
            os << "{"
               << "\"addr\":\"" << Hex(post[j].addr) << "\","
               << "\"rva\":\"0x" << std::hex << std::uppercase << (post[j].addr - mod.base) << std::dec << "\","
               << "\"kind\":\"" << post[j].kind << "\"";
            if (post[j].kind == "direct") {
                os << ",\"target\":\"" << Hex(post[j].target) << "\"";
                if (InModule(mod, post[j].target)) {
                    os << ",\"target_rva\":\"0x" << std::hex << std::uppercase
                       << (post[j].target - mod.base) << std::dec << "\"";
                }
            } else {
                os << ",\"vtable_offset\":\"0x" << std::hex << std::uppercase
                   << post[j].vtableOffset << std::dec << "\""
                   << ",\"vtable_index\":" << (post[j].vtableOffset / sizeof(uintptr_t));
            }
            os << "}";
        }
        os << "]"
           << "}";
    }
    os << "],\"rdata_qword_refs\":[";
    for (size_t i = 0; i < rdataRefs.size(); ++i) {
        if (i) os << ",";
        os << "{\"addr\":\"" << Hex(rdataRefs[i]) << "\",\"rva\":\"0x"
           << std::hex << std::uppercase << (rdataRefs[i] - mod.base) << std::dec << "\"}";
    }
    os << "],\"data_qword_refs\":[";
    for (size_t i = 0; i < dataRefs.size(); ++i) {
        if (i) os << ",";
        os << "{\"addr\":\"" << Hex(dataRefs[i]) << "\",\"rva\":\"0x"
           << std::hex << std::uppercase << (dataRefs[i] - mod.base) << std::dec << "\"}";
    }
    os << "]}\n";

    std::lock_guard<std::mutex> lock(g_stateMutex);
    g_state.lastCommand = "fmod/native-xrefs";
    g_state.lastError.clear();
    return os.str();
}

std::string CmdCodeWindow(uintptr_t rva, size_t span) {
    ModuleInfo mod{};
    std::string error;
    if (!EnsureModuleForCommand(&mod, &error)) {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        g_state.lastCommand = "fmod/code-window";
        g_state.lastError = error;
        return BuildStatusJson(g_state);
    }
    if (!rva) return "{\"command\":\"fmod/code-window\",\"error\":\"missing rva\"}\n";
    if (span < 0x20) span = 0x20;
    if (span > 0x400) span = 0x400;

    uintptr_t addr = mod.base + rva;
    if (!InText(mod, addr)) {
        return "{\"command\":\"fmod/code-window\",\"error\":\"rva is not in .text\"}\n";
    }
    uintptr_t funcStart = 0;
    {
        std::vector<uint8_t> textBytes(mod.text.size);
        if (SafeReadBytes(mod.text.start, textBytes.data(), mod.text.size)) {
            size_t off = static_cast<size_t>(addr - mod.text.start);
            uintptr_t funcOff = BacktrackFunctionStart(textBytes, off);
            if (funcOff) funcStart = mod.text.start + funcOff;
        }
    }

    std::vector<uint8_t> bytes(span);
    if (!SafeReadBytes(addr, bytes.data(), span)) {
        return "{\"command\":\"fmod/code-window\",\"error\":\"cannot read bytes\"}\n";
    }

    std::ostringstream os;
    os << "{"
       << "\"command\":\"fmod/code-window\","
       << "\"addr\":\"" << Hex(addr) << "\","
       << "\"rva\":\"0x" << std::hex << std::uppercase << rva << std::dec << "\","
       << "\"span\":\"0x" << std::hex << std::uppercase << span << std::dec << "\","
       << "\"function_start\":\"" << Hex(funcStart) << "\","
       << "\"function_rva\":\"0x" << std::hex << std::uppercase
       << (funcStart ? funcStart - mod.base : 0) << std::dec << "\","
       << "\"bytes\":\"" << HexBytes(addr, span) << "\","
       << "\"events\":[";
    bool first = true;
    for (size_t i = 0; i + 7 <= bytes.size(); ++i) {
        struct Ev { const char* kind; uintptr_t target; uint32_t off; };
        Ev ev{nullptr, 0, 0};
        if (bytes[i] == 0xE8 && i + 5 <= bytes.size()) {
            int32_t disp = *reinterpret_cast<const int32_t*>(&bytes[i + 1]);
            ev = {"direct_call", addr + i + 5 + disp, 0};
        } else if ((bytes[i] == 0x48 || bytes[i] == 0x4C) && i + 7 <= bytes.size() &&
                   (bytes[i + 1] == 0x8D || bytes[i + 1] == 0x8B) &&
                   ((bytes[i + 2] & 0xC7) == 0x05)) {
            int32_t disp = *reinterpret_cast<const int32_t*>(&bytes[i + 3]);
            ev = {bytes[i + 1] == 0x8D ? "rip_lea" : "rip_mov", addr + i + 7 + disp, 0};
            } else if (bytes[i] == 0xFF && i + 6 <= bytes.size() &&
                   (bytes[i + 1] == 0x90 || bytes[i + 1] == 0x91)) {
            ev = {"vcall", 0, *reinterpret_cast<const uint32_t*>(&bytes[i + 2])};
        } else if (i + 7 <= bytes.size() &&
                   (bytes[i] == 0x48 || bytes[i] == 0x49) &&
                   bytes[i + 1] == 0xFF &&
                   (bytes[i + 2] == 0xA0 || bytes[i + 2] == 0xA1)) {
            ev = {"vjmp", 0, *reinterpret_cast<const uint32_t*>(&bytes[i + 3])};
        } else if (bytes[i] == 0xFF && i + 3 <= bytes.size() &&
                   (bytes[i + 1] == 0x50 || bytes[i + 1] == 0x51)) {
            ev = {"vcall", 0, bytes[i + 2]};
        } else if (i + 4 <= bytes.size() &&
                   (bytes[i] == 0x48 || bytes[i] == 0x49) &&
                   bytes[i + 1] == 0xFF &&
                   (bytes[i + 2] == 0x60 || bytes[i + 2] == 0x61)) {
            ev = {"vjmp", 0, bytes[i + 3]};
        }
        if (!ev.kind) continue;
        if (!first) os << ",";
        first = false;
        os << "{"
           << "\"at\":\"" << Hex(addr + i) << "\","
           << "\"rva\":\"0x" << std::hex << std::uppercase << (rva + i) << std::dec << "\","
           << "\"kind\":\"" << ev.kind << "\"";
        if (strcmp(ev.kind, "vcall") == 0 || strcmp(ev.kind, "vjmp") == 0) {
            os << ",\"vtable_offset\":\"0x" << std::hex << std::uppercase << ev.off << std::dec
               << "\",\"vtable_index\":" << (ev.off / sizeof(uintptr_t));
        } else {
            os << ",\"target\":\"" << Hex(ev.target) << "\"";
            if (InModule(mod, ev.target)) {
                os << ",\"target_rva\":\"0x" << std::hex << std::uppercase
                   << (ev.target - mod.base) << std::dec << "\""
                   << ",\"target_section\":\"" << SectionName(mod, ev.target) << "\"";
            }
        }
        os << "}";
    }
    os << "]}\n";

    std::lock_guard<std::mutex> lock(g_stateMutex);
    g_state.lastCommand = "fmod/code-window";
    g_state.lastError.clear();
    return os.str();
}

std::string CmdVtableDump(uintptr_t addr, uintptr_t rva, size_t count) {
    ModuleInfo mod{};
    std::string error;
    if (!EnsureModuleForCommand(&mod, &error)) {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        g_state.lastCommand = "fmod/vtable";
        g_state.lastError = error;
        return BuildStatusJson(g_state);
    }
    if (!addr && rva) addr = mod.base + rva;
    if (!addr) return "{\"command\":\"fmod/vtable\",\"error\":\"missing addr or rva\"}\n";
    if (count < 1) count = 1;
    if (count > 96) count = 96;

    std::vector<uint8_t> textBytes;
    textBytes.resize(mod.text.size);
    bool haveText = SafeReadBytes(mod.text.start, textBytes.data(), mod.text.size);

    std::ostringstream os;
    os << "{"
       << "\"command\":\"fmod/vtable\","
       << "\"addr\":\"" << Hex(addr) << "\","
       << "\"rva\":\"0x" << std::hex << std::uppercase
       << (InModule(mod, addr) ? addr - mod.base : 0) << std::dec << "\","
       << "\"count\":" << count << ","
       << "\"entries\":[";

    for (size_t i = 0; i < count; ++i) {
        uintptr_t slot = addr + i * sizeof(uintptr_t);
        uintptr_t target = 0;
        bool ok = SafeRead(slot, &target);
        if (i) os << ",";
        os << "{"
           << "\"index\":" << i << ","
           << "\"slot\":\"" << Hex(slot) << "\","
           << "\"ok\":" << (ok ? "true" : "false") << ","
           << "\"target\":\"" << Hex(target) << "\"";
        if (ok && InModule(mod, target)) {
            os << ",\"target_rva\":\"0x" << std::hex << std::uppercase
               << (target - mod.base) << std::dec << "\""
               << ",\"target_section\":\"" << SectionName(mod, target) << "\"";
        }
        if (ok && haveText && InText(mod, target)) {
            size_t off = static_cast<size_t>(target - mod.text.start);
            uintptr_t funcOff = BacktrackFunctionStart(textBytes, off);
            uintptr_t func = funcOff ? mod.text.start + funcOff : 0;
            os << ",\"function_start\":\"" << Hex(func) << "\","
               << "\"function_rva\":\"0x" << std::hex << std::uppercase
               << (func ? func - mod.base : 0) << std::dec << "\"";
        }
        os << "}";
    }

    os << "]}\n";
    std::lock_guard<std::mutex> lock(g_stateMutex);
    g_state.lastCommand = "fmod/vtable";
    g_state.lastError.clear();
    return os.str();
}

std::string CmdHandlePath() {
    std::lock_guard<std::mutex> lock(g_stateMutex);
    uintptr_t sp = 0;
    if (!g_state.candidates.empty()) sp = g_state.candidates.front().sampleProperties;
    if (!sp) {
        g_state.lastCommand = "fmod/handle-path";
        g_state.lastError = "no sampleProperties from /scan";
        return BuildStatusJson(g_state);
    }
    g_state.handlePath = ReadHandlePathDump(sp, g_state.mod);
    g_state.lastCommand = "fmod/handle-path";
    g_state.lastError = g_state.handlePath.error;
    AppendLog("handle-path: ok=%d items=%llu err='%s'",
        g_state.handlePath.ok ? 1 : 0,
        static_cast<unsigned long long>(g_state.handlePath.items.size()),
        g_state.handlePath.error.c_str());

    std::ostringstream os;
    os << "{"
       << "\"command\":\"fmod/handle-path\","
       << "\"ok\":" << (g_state.handlePath.ok ? "true" : "false") << ","
       << "\"sample_properties\":\"" << Hex(sp) << "\","
       << "\"handle_base\":\"" << Hex(g_state.handlePath.handleBase) << "\","
       << "\"vec_size\":" << g_state.handlePath.vecSize << ","
       << "\"items\":[";
    for (size_t i = 0; i < g_state.handlePath.items.size(); ++i) {
        if (i) os << ",";
        auto& e = g_state.handlePath.items[i];
        os << "{"
           << "\"elem\":\"" << Hex(e.elemAddr) << "\","
           << "\"f0\":\"" << Hex(e.field_00) << "\","
           << "\"f8\":\"" << Hex(e.field_08) << "\","
           << "\"f10\":\"" << Hex(e.field_10) << "\","
           << "\"ptr18\":\"" << Hex(e.ptrAt18) << "\","
           << "\"sound_in_module\":" << (e.soundInModule ? "true" : "false") << ","
           << "\"f20\":\"" << Hex(e.field_20) << "\","
           << "\"f28\":\"" << Hex(e.field_28) << "\","
           << "\"handle\":" << e.handle << ","
           << "\"f34\":" << e.field_34 << ","
           << "\"f38\":\"" << Hex(e.field_38) << "\","
           << "\"f40\":\"" << Hex(e.field_40) << "\""
           << "}";
    }
    os << "],\"error\":\"" << JsonEscape(g_state.handlePath.error) << "\""
       << "}\n";
    return os.str();
}

// ── Resolve API ────────────────────────────────────────────────
std::string CmdResolveApi() {
    std::lock_guard<std::mutex> lock(g_stateMutex);
    if (!g_state.mod.base) {
        g_state.lastCommand = "fmod/resolve-api";
        g_state.lastError = "no module info; run /scan first";
        return BuildStatusJson(g_state);
    }

    uintptr_t systemI = 0;
    if (!g_state.systemIHits.empty()) systemI = g_state.systemIHits.front().systemI;

    g_state.apiResult = ResolveFmodApis(g_state.mod, systemI);
    g_state.lastCommand = "fmod/resolve-api";
    g_state.lastError = g_state.apiResult.error;
    AppendLog("resolve-api: ok=%d entries=%d systemI=%s err='%s'",
        g_state.apiResult.ok ? 1 : 0,
        static_cast<int>(g_state.apiResult.entries.size()),
        Hex(systemI).c_str(), g_state.apiResult.error.c_str());

    std::ostringstream os;
    os << "{"
       << "\"command\":\"fmod/resolve-api\","
       << "\"ok\":" << (g_state.apiResult.ok ? "true" : "false") << ","
        << "\"root_candidate\":\"" << Hex(systemI) << "\","
       << "\"entries\":[";
    for (size_t i = 0; i < g_state.apiResult.entries.size(); ++i) {
        if (i) os << ",";
        auto& e = g_state.apiResult.entries[i];
        os << "{"
           << "\"name\":\"" << JsonEscape(e.name) << "\","
           << "\"found\":" << (e.found ? "true" : "false") << ","
           << "\"string_rva\":\"0x" << std::hex << std::uppercase << e.stringRva << std::dec << "\","
           << "\"lea_rva\":\"0x" << std::hex << std::uppercase << e.leaRva << std::dec << "\","
           << "\"func_rva\":\"0x" << std::hex << std::uppercase << e.funcRva << std::dec << "\","
           << "\"func_addr\":\"" << Hex(e.funcAddr) << "\","
           << "\"note\":\"" << JsonEscape(e.note) << "\""
           << "}";
    }
    os << "],\"error\":\"" << JsonEscape(g_state.apiResult.error) << "\""
       << "}\n";
    return os.str();
}

// ── Topology Dump ──────────────────────────────────────────────
std::string CmdTopologyDump() {
    std::lock_guard<std::mutex> lock(g_stateMutex);

    // Prefer locked fmod_sound (live) over stale scan snapshot
    uintptr_t targetSound = g_state.lock.fmodSound;
    if (!targetSound && !g_state.candidates.empty())
        targetSound = g_state.candidates.front().fmodSound;

    g_state.topo = RunTopologyWalk(g_state.mod, targetSound, g_state.apiResult);
    g_state.lastCommand = "fmod/topology-dump";
    g_state.lastError = g_state.topo.error;

    // Include log entries in output
    std::string logJson;
    for (size_t i = 0; i < g_state.topo.log.size(); ++i) {
        if (i) logJson += ",";
        logJson += "\"" + JsonEscape(g_state.topo.log[i]) + "\"";
    }

    std::ostringstream os;
    os << "{"
       << "\"command\":\"fmod/topology-dump\","
       << "\"ok\":" << (g_state.topo.ok ? "true" : "false") << ","
       << "\"target_sound\":\"" << Hex(targetSound) << "\","
        << "\"root_candidate\":\"" << Hex(g_state.topo.systemI) << "\","
       << "\"vtable_matches\":" << g_state.topo.groupsSeen << ","
       << "\"log\":[" << logJson << "],"
       << "\"error\":\"" << JsonEscape(g_state.topo.error) << "\""
       << "}\n";
    return os.str();
}

// ── Stream Dump ───────────────────────────────────────────────
std::string CmdDumpStream(uintptr_t span) {
    std::lock_guard<std::mutex> lock(g_stateMutex);
    if (g_state.candidates.empty()) {
        return "{\"error\":\"no candidates; call /scan first\"}\n";
    }
    uintptr_t obj = g_state.candidates.front().streamObject;
    if (!obj) return "{\"error\":\"no streamObject\"}\n";
    if (span < 0x20) span = 0x20;
    if (span > 0x200) span = 0x200;

    std::vector<uint8_t> buf(static_cast<size_t>(span));
    if (!SafeReadBytes(obj, buf.data(), static_cast<size_t>(span))) {
        return "{\"error\":\"cannot read stream object memory\"}\n";
    }

    // Live-read fmod_sound and other known fields
    uintptr_t liveFmodSound = 0;
    SafeRead(obj + 0x08, &liveFmodSound);
    uintptr_t livePtr40 = 0, livePtr48 = 0, livePtr80 = 0, livePtr98 = 0;
    SafeRead(obj + 0x40, &livePtr40);
    SafeRead(obj + 0x48, &livePtr48);
    SafeRead(obj + 0x80, &livePtr80);
    SafeRead(obj + 0x98, &livePtr98);

    // Known string fields
    MsvcString busPath = ReadMsvcString(obj + 0x50);

    std::ostringstream os;
    os << "{"
       << "\"stream_object\":\"" << Hex(obj) << "\","
       << "\"span\":\"0x" << std::hex << std::uppercase << span << std::dec << "\","
       << "\"fmod_sound\":\"" << Hex(liveFmodSound) << "\","
       << "\"known_fields\":{"
       << "\"ptr40\":\"" << Hex(livePtr40) << "\","
       << "\"ptr48\":\"" << Hex(livePtr48) << "\","
       << "\"ptr80\":\"" << Hex(livePtr80) << "\","
       << "\"ptr98\":\"" << Hex(livePtr98) << "\""
       << "},";
    if (busPath.ok)
        os << "\"bus_path\":\"" << JsonEscape(busPath.value) << "\",";
    os << "\"qwords\":[";
    for (size_t off = 0; off + 8 <= static_cast<size_t>(span); off += 8) {
        if (off) os << ",";
        uintptr_t val = *reinterpret_cast<const uintptr_t*>(&buf[off]);
        bool inModule = InModule(g_state.mod, val);
        os << "{"
           << "\"off\":\"0x" << std::hex << std::uppercase << off << std::dec << "\","
           << "\"val\":\"" << Hex(val) << "\","
           << "\"in_module\":" << (inModule ? "true" : "false");
        if (inModule) {
            os << ",\"rva\":\"0x" << std::hex << std::uppercase << (val - g_state.mod.base) << std::dec << "\"";
        }
        if (val > 0x10000 && val < 0x1000000000000) {
            MsvcString s = ReadMsvcString(obj + off);
            if (s.ok && s.value.size() >= 3 && s.value.size() <= 64) {
                os << ",\"inline_msvc_str\":\"" << JsonEscape(s.value) << "\"";
                os << ",\"str_len\":" << s.len;
            }
        }
        os << "}";
    }
    os << "]}\n";
    return os.str();
}

// ── Ptr Dump ───────────────────────────────────────────────────
std::string CmdDumpPtr(uintptr_t addr, uintptr_t span) {
    if (!addr) return "{\"error\":\"no addr\"}\n";
    if (span < 0x20) span = 0x20;
    if (span > 0x200) span = 0x200;

    std::vector<uint8_t> buf(static_cast<size_t>(span));
    if (!SafeReadBytes(addr, buf.data(), static_cast<size_t>(span))) {
        return "{\"error\":\"cannot read address\"}\n";
    }

    std::ostringstream os;
    os << "{"
       << "\"addr\":\"" << Hex(addr) << "\","
       << "\"span\":\"0x" << std::hex << std::uppercase << span << std::dec << "\","
       << "\"qwords\":[";
    for (size_t off = 0; off + 8 <= static_cast<size_t>(span); off += 8) {
        if (off) os << ",";
        uintptr_t val = *reinterpret_cast<const uintptr_t*>(&buf[off]);
        bool inModule = InModule(g_state.mod, val);
        os << "{"
           << "\"off\":\"0x" << std::hex << std::uppercase << off << std::dec << "\","
           << "\"val\":\"" << Hex(val) << "\","
           << "\"in_module\":" << (inModule ? "true" : "false");
        if (inModule) {
            os << ",\"rva\":\"0x" << std::hex << std::uppercase << (val - g_state.mod.base) << std::dec << "\"";
        }
        if (val > 0x10000 && val < 0x1000000000000) {
            MsvcString s = ReadMsvcString(addr + off);
            if (s.ok && s.value.size() >= 3 && s.value.size() <= 64) {
                os << ",\"inline_msvc_str\":\"" << JsonEscape(s.value) << "\",\"str_len\":" << s.len;
            }
        }
        os << "}";
    }
    os << "]}\n";
    return os.str();
}

// ── Graph Dump ─────────────────────────────────────────────────
std::string CmdDumpGraph() {
    std::lock_guard<std::mutex> lock(g_stateMutex);
    if (g_state.candidates.empty()) {
        return "{\"error\":\"no candidates; call /scan first\"}\n";
    }
    uintptr_t stream = g_state.candidates.front().streamObject;
    if (!stream) return "{\"error\":\"no streamObject\"}\n";

    struct Node { std::string name; uintptr_t addr; int depth; };
    std::vector<Node> nodes;
    nodes.push_back({"stream", stream, 0});

    // Read known pointers from stream
    uintptr_t fmodSound = 0, ptr40 = 0, ptr48 = 0, ptr80 = 0, ptr98 = 0;
    MsvcString busPath;
    SafeRead(stream + 0x08, &fmodSound);
    SafeRead(stream + 0x40, &ptr40);
    SafeRead(stream + 0x48, &ptr48);
    SafeRead(stream + 0x80, &ptr80);
    SafeRead(stream + 0x98, &ptr98);
    busPath = ReadMsvcString(stream + 0x50);

    auto addNode = [&](const char* name, uintptr_t addr, int depth) {
        if (!addr) return;
        nodes.push_back({name, addr, depth});
    };
    addNode("fmod_sound", fmodSound, 1);
    addNode("ptr40", ptr40, 1);
    addNode("ptr48", ptr48, 1);
    addNode("ptr80", ptr80, 1);
    addNode("ptr98", ptr98, 1);

    // Helper to safely read +0x00 to +0x50 from any address
    auto readFields = [&](uintptr_t addr, uintptr_t* v00, uintptr_t* v08,
                          uintptr_t* v10, uintptr_t* v18, uintptr_t* v48) {
        SafeRead(addr + 0x00, v00);
        SafeRead(addr + 0x08, v08);
        SafeRead(addr + 0x10, v10);
        SafeRead(addr + 0x18, v18);
        SafeRead(addr + 0x48, v48);
    };

    // For each depth-1 node, dump key fields
    struct FieldSummary {
        std::string name;
        uintptr_t addr;
        uintptr_t v00, v08, v10, v18, v40, v48;
        bool v00inMod, v40inMod;
        MsvcString strAt10, strAt50;
    };
    std::vector<FieldSummary> summaries;

    for (size_t i = 1; i < nodes.size(); ++i) {
        FieldSummary fs;
        fs.name = nodes[i].name;
        fs.addr = nodes[i].addr;
        SafeRead(nodes[i].addr + 0x00, &fs.v00);
        SafeRead(nodes[i].addr + 0x08, &fs.v08);
        SafeRead(nodes[i].addr + 0x10, &fs.v10);
        SafeRead(nodes[i].addr + 0x18, &fs.v18);
        SafeRead(nodes[i].addr + 0x40, &fs.v40);
        SafeRead(nodes[i].addr + 0x48, &fs.v48);
        fs.v00inMod = InModule(g_state.mod, fs.v00);
        fs.v40inMod = InModule(g_state.mod, fs.v40);
        fs.strAt10 = ReadMsvcString(nodes[i].addr + 0x10);
        fs.strAt50 = ReadMsvcString(nodes[i].addr + 0x50);
        summaries.push_back(fs);
    }

    std::ostringstream os;
    os << "{"
       << "\"stream_object\":\"" << Hex(stream) << "\","
       << "\"fmod_sound\":\"" << Hex(fmodSound) << "\","
       << "\"bus_path\":\"" << (busPath.ok ? JsonEscape(busPath.value) : "") << "\","
       << "\"pointers\":{"
       << "\"ptr40\":\"" << Hex(ptr40) << "\","
       << "\"ptr48\":\"" << Hex(ptr48) << "\","
       << "\"ptr80\":\"" << Hex(ptr80) << "\","
       << "\"ptr98\":\"" << Hex(ptr98) << "\""
       << "},";
    // fmod_sound snapshot (extended fields for diff reference)
    if (fmodSound) {
        uintptr_t sv[8] = {};
        bool smod[8] = {};
        int soff[] = {0x00, 0x08, 0x10, 0x18, 0x20, 0x28, 0x40, 0x48};
        for (int i = 0; i < 8; ++i) {
            SafeRead(fmodSound + soff[i], &sv[i]);
            smod[i] = InModule(g_state.mod, sv[i]);
        }
        os << "\"sound_fields\":{"
           << "\"v00\":\"" << Hex(sv[0]) << "\",\"v00in_mod\":" << (smod[0] ? "true" : "false") << ","
           << "\"v08\":\"" << Hex(sv[1]) << "\",\"v08in_mod\":" << (smod[1] ? "true" : "false") << ","
           << "\"v10\":\"" << Hex(sv[2]) << "\",\"v10in_mod\":" << (smod[2] ? "true" : "false") << ","
           << "\"v18\":\"" << Hex(sv[3]) << "\",\"v18in_mod\":" << (smod[3] ? "true" : "false") << ","
           << "\"v20\":\"" << Hex(sv[4]) << "\",\"v20in_mod\":" << (smod[4] ? "true" : "false") << ","
           << "\"v28\":\"" << Hex(sv[5]) << "\",\"v28in_mod\":" << (smod[5] ? "true" : "false") << ","
           << "\"v40\":\"" << Hex(sv[6]) << "\",\"v40in_mod\":" << (smod[6] ? "true" : "false") << ","
           << "\"v48\":\"" << Hex(sv[7]) << "\",\"v48in_mod\":" << (smod[7] ? "true" : "false")
           << "},";
    } else {
        os << "\"sound_fields\":null,";
    }
    os << "\"nodes\":[";
    for (size_t i = 0; i < summaries.size(); ++i) {
        if (i) os << ",";
        auto& fs = summaries[i];
        os << "{"
           << "\"name\":\"" << fs.name << "\","
           << "\"addr\":\"" << Hex(fs.addr) << "\","
           << "\"v00\":\"" << Hex(fs.v00) << "\","
           << "\"v00in_mod\":" << (fs.v00inMod ? "true" : "false") << ","
           << "\"v08\":\"" << Hex(fs.v08) << "\","
           << "\"v10\":\"" << Hex(fs.v10) << "\","
           << "\"v18\":\"" << Hex(fs.v18) << "\","
           << "\"v40\":\"" << Hex(fs.v40) << "\","
           << "\"v40in_mod\":" << (fs.v40inMod ? "true" : "false") << ","
           << "\"v48\":\"" << Hex(fs.v48) << "\"";
        if (fs.strAt10.ok)
            os << ",\"str10\":\"" << JsonEscape(fs.strAt10.value) << "\"";
        if (fs.strAt50.ok)
            os << ",\"str50\":\"" << JsonEscape(fs.strAt50.value) << "\"";
        os << "}";
    }
    os << "]}\n";
    return os.str();
}

// ── Game Controls ──────────────────────────────────────────────
std::string CmdDumpPlaybackNode() {
    std::lock_guard<std::mutex> lock(g_stateMutex);
    if (g_state.candidates.empty()) {
        return "{\"error\":\"no candidates; call /scan first\"}\n";
    }

    uintptr_t stream = g_state.candidates.front().streamObject;
    if (!stream) return "{\"error\":\"no streamObject\"}\n";

    uintptr_t streamSound = 0, node = 0;
    SafeRead(stream + 0x08, &streamSound);
    SafeRead(stream + 0x80, &node);
    if (!node) return "{\"error\":\"stream +0x80 is null\"}\n";

    struct Field {
        const char* name;
        uintptr_t off;
        uintptr_t val;
    };

    Field fields[] = {
        {"v00", 0x00, 0}, {"current_sound", 0x08, 0}, {"ptr10", 0x10, 0},
        {"tag18", 0x18, 0}, {"stream_backref", 0x20, 0}, {"flags28", 0x28, 0},
        {"ptr90", 0x90, 0}, {"ptr98", 0x98, 0}, {"ptrA0", 0xA0, 0},
        {"ptrA8", 0xA8, 0}, {"ptrB0", 0xB0, 0}, {"ptrB8", 0xB8, 0},
        {"ptrC8", 0xC8, 0}, {"ptrD0", 0xD0, 0}, {"ptrD8", 0xD8, 0},
        {"ptrE0", 0xE0, 0}, {"ptrF8", 0xF8, 0}, {"ptr100", 0x100, 0},
    };
    for (auto& f : fields) SafeRead(node + f.off, &f.val);

    std::ostringstream os;
    os << "{"
       << "\"stream_object\":\"" << Hex(stream) << "\","
       << "\"stream_fmod_sound\":\"" << Hex(streamSound) << "\","
       << "\"playback_node\":\"" << Hex(node) << "\","
       << "\"current_sound_match\":" << (fields[1].val == streamSound ? "true" : "false") << ","
       << "\"stream_backref_match\":" << (fields[4].val == stream ? "true" : "false") << ","
       << "\"fields\":[";

    for (size_t i = 0; i < _countof(fields); ++i) {
        if (i) os << ",";
        bool inModule = InModule(g_state.mod, fields[i].val);
        os << "{"
           << "\"name\":\"" << fields[i].name << "\","
           << "\"off\":\"0x" << std::hex << std::uppercase << fields[i].off << std::dec << "\","
           << "\"val\":\"" << Hex(fields[i].val) << "\","
           << "\"in_module\":" << (inModule ? "true" : "false");
        if (inModule) {
            os << ",\"rva\":\"0x" << std::hex << std::uppercase
               << (fields[i].val - g_state.mod.base) << std::dec << "\"";
        }
        os << "}";
    }

    os << "],\"children\":[";
    bool firstChild = true;
    for (const auto& f : fields) {
        if (f.off < 0x90 || !f.val) continue;
        uintptr_t q[8] = {};
        bool readable = true;
        for (int i = 0; i < 8; ++i) {
            if (!SafeRead(f.val + i * sizeof(uintptr_t), &q[i])) {
                readable = false;
                break;
            }
        }
        if (!readable) continue;

        if (!firstChild) os << ",";
        firstChild = false;
        os << "{"
           << "\"from\":\"" << f.name << "\","
           << "\"addr\":\"" << Hex(f.val) << "\","
           << "\"qwords\":[";
        for (int i = 0; i < 8; ++i) {
            if (i) os << ",";
            bool inModule = InModule(g_state.mod, q[i]);
            os << "{"
               << "\"off\":\"0x" << std::hex << std::uppercase
               << (i * sizeof(uintptr_t)) << std::dec << "\","
               << "\"val\":\"" << Hex(q[i]) << "\","
               << "\"in_module\":" << (inModule ? "true" : "false");
            if (inModule) {
                os << ",\"rva\":\"0x" << std::hex << std::uppercase
                   << (q[i] - g_state.mod.base) << std::dec << "\"";
            }
            os << "}";
        }
        MsvcString str10 = ReadMsvcString(f.val + 0x10);
        MsvcString str50 = ReadMsvcString(f.val + 0x50);
        os << "]";
        if (str10.ok) os << ",\"str10\":\"" << JsonEscape(str10.value) << "\"";
        if (str50.ok) os << ",\"str50\":\"" << JsonEscape(str50.value) << "\"";
        os << "}";
    }

    os << "]}\n";
    return os.str();
}

// ── SEH-safe call wrappers (no C++ objects in __try scope) ───
// MSVC /EHsc forbids __try in functions with C++ unwindable objects.
// These helpers keep the SEH boundary isolated from std::string etc.

static bool SafeCallRaw4(uintptr_t fnAddr, uintptr_t rcx, uintptr_t rdx,
                         uintptr_t r8, uintptr_t r9, uintptr_t* outRax) {
    typedef uintptr_t (*Fn4)(uintptr_t, uintptr_t, uintptr_t, uintptr_t);
    auto callable = reinterpret_cast<Fn4>(fnAddr);
    __try { *outRax = callable(rcx, rdx, r8, r9); return true; }
    __except (EXCEPTION_EXECUTE_HANDLER) { return false; }
}

static bool SafeCallRaw3(uintptr_t fnAddr, uintptr_t rcx, uintptr_t rdx,
                         uintptr_t r8, uintptr_t* outRax) {
    typedef uintptr_t (*Fn3)(uintptr_t, uintptr_t, uintptr_t);
    auto callable = reinterpret_cast<Fn3>(fnAddr);
    __try { *outRax = callable(rcx, rdx, r8); return true; }
    __except (EXCEPTION_EXECUTE_HANDLER) { return false; }
}

// ── Generic Debug Endpoints ───────────────────────────────────

// /call?addr=RVA&rcx=HEX&rdx=HEX&r8=HEX&r9=HEX
// Calls any function at exeBase+RVA with up to 4 params (x64 fastcall).
// Returns {rax, crashed}.
std::string CmdCall(const std::string& addrStr, const std::string& rcxStr,
                    const std::string& rdxStr, const std::string& r8Str,
                    const std::string& r9Str) {
    uintptr_t exeBase = 0;
    { std::lock_guard<std::mutex> lk(g_stateMutex); exeBase = g_state.mod.base; }
    if (!exeBase) return "{\"error\":\"no module base; run /scan\"}\n";
    if (addrStr.empty()) return "{\"error\":\"missing addr param (hex RVA)\"}\n";

    uintptr_t fnAddr = exeBase + std::stoull(addrStr, nullptr, 16);
    uintptr_t rcx = rcxStr.empty() ? 0 : std::stoull(rcxStr, nullptr, 16);
    uintptr_t rdx = rdxStr.empty() ? 0 : std::stoull(rdxStr, nullptr, 16);
    uintptr_t r8  = r8Str.empty()  ? 0 : std::stoull(r8Str, nullptr, 16);
    uintptr_t r9  = r9Str.empty()  ? 0 : std::stoull(r9Str, nullptr, 16);

    typedef uintptr_t (*Call4_t)(uintptr_t, uintptr_t, uintptr_t, uintptr_t);

    uintptr_t rax = 0;
    bool crashed = !SafeCallRaw4(fnAddr, rcx, rdx, r8, r9, &rax);

    std::ostringstream os;
    os << "{\"command\":\"call\",\"addr\":\"" << Hex(fnAddr)
       << "\",\"rva\":\"0x" << std::hex << std::uppercase << (fnAddr - exeBase) << std::dec
       << "\",\"rcx\":\"" << Hex(rcx) << "\",\"rdx\":\"" << Hex(rdx)
       << "\",\"r8\":\"" << Hex(r8) << "\",\"r9\":\"" << Hex(r9)
       << "\",\"rax\":\"" << Hex(rax) << "\",\"crashed\":" << (crashed ? "true" : "false")
       << "}\n";
    AppendLog("call: addr=%s rcx=%s rdx=%s -> rax=%s crashed=%d",
        Hex(fnAddr).c_str(), Hex(rcx).c_str(), Hex(rdx).c_str(), Hex(rax).c_str(), crashed);
    return os.str();
}

// /vcall?object=HEX&index=N&rcx=HEX&rdx=HEX&r8=HEX
// Calls object->vtable[index](object_or_rcx, rdx, r8)
// If rcx is empty, passes 'object' as first arg (this call).
std::string CmdVCall(const std::string& objStr, const std::string& idxStr,
                     const std::string& rcxStr, const std::string& rdxStr,
                     const std::string& r8Str) {
    if (objStr.empty() || idxStr.empty())
        return "{\"error\":\"missing object or index\"}\n";

    uintptr_t obj   = std::stoull(objStr, nullptr, 16);
    int index       = std::stoi(idxStr, nullptr, 0);
    uintptr_t rcx   = rcxStr.empty() ? obj : std::stoull(rcxStr, nullptr, 16);
    uintptr_t rdx   = rdxStr.empty() ? 0 : std::stoull(rdxStr, nullptr, 16);
    uintptr_t r8    = r8Str.empty()  ? 0 : std::stoull(r8Str, nullptr, 16);

    uintptr_t vtable = 0;
    SafeRead(obj, &vtable);
    if (!vtable)
        return "{\"error\":\"cannot read vtable from object\"}\n";

    uintptr_t method = 0;
    SafeRead(vtable + index * sizeof(uintptr_t), &method);
    if (!method)
        return "{\"error\":\"vtable entry is null\"}\n";

    typedef uintptr_t (*Call3_t)(uintptr_t, uintptr_t, uintptr_t);

    uintptr_t rax = 0;
    bool crashed = !SafeCallRaw3(method, rcx, rdx, r8, &rax);

    std::ostringstream os;
    os << "{\"command\":\"vcall\",\"object\":\"" << Hex(obj)
       << "\",\"vtable\":\"" << Hex(vtable)
       << "\",\"index\":" << index << ",\"vtable_off\":\"0x" << std::hex << (index * 8) << std::dec
       << "\",\"method\":\"" << Hex(method)
       << "\",\"rcx\":\"" << Hex(rcx) << "\",\"rdx\":\"" << Hex(rdx)
       << "\",\"r8\":\"" << Hex(r8)
       << "\",\"rax\":\"" << Hex(rax) << "\",\"crashed\":" << (crashed ? "true" : "false")
       << "}\n";
    AppendLog("vcall: obj=%s idx=%d method=%s -> rax=%s crashed=%d",
        Hex(obj).c_str(), index, Hex(method).c_str(), Hex(rax).c_str(), crashed);
    return os.str();
}

// /ptrchain?base=HEX&offsets=0x18,0xC0
// Follows a chain: base → *(base+off1) → *(result+off2) → ...
// Returns all intermediate values + final vtable dump.
std::string CmdPtrChain(const std::string& baseStr, const std::string& offsetsStr) {
    if (baseStr.empty() || offsetsStr.empty())
        return "{\"error\":\"missing base or offsets (comma-separated hex)\"}\n";

    uintptr_t cur = std::stoull(baseStr, nullptr, 16);

    // Parse offsets
    std::vector<uintptr_t> offsets;
    std::string offs = offsetsStr;
    size_t pos = 0;
    while ((pos = offs.find(',')) != std::string::npos || !offs.empty()) {
        std::string token = offs.substr(0, pos);
        while (!token.empty() && token[0] == ' ') token.erase(0, 1);
        if (!token.empty()) offsets.push_back(std::stoull(token, nullptr, 16));
        if (pos == std::string::npos) break;
        offs.erase(0, pos + 1);
        if (offs.empty()) break;
    }

    std::ostringstream os;
    os << "{\"command\":\"ptrchain\",\"base\":\"" << Hex(cur) << "\",\"steps\":[";

    for (size_t i = 0; i < offsets.size(); ++i) {
        uintptr_t off = offsets[i];
        uintptr_t addr = cur + off;
        uintptr_t val = 0;
        SafeRead(addr, &val);

        ModuleInfo mod;
        { std::lock_guard<std::mutex> lk(g_stateMutex); mod = g_state.mod; }

        os << "{"
           << "\"step\":" << i
           << ",\"at\":\"" << Hex(addr) << "\""
           << ",\"off\":\"0x" << std::hex << off << std::dec << "\""
           << ",\"val\":\"" << Hex(val) << "\""
           << ",\"in_module\":" << (InModule(mod, val) ? "true" : "false");
        if (InModule(mod, val)) {
            os << ",\"rva\":\"0x" << std::hex << std::uppercase << (val - mod.base) << std::dec << "\""
               << ",\"section\":\"" << SectionName(mod, val) << "\"";
        }
        // Try MSVC string at this address
        if (val > 0x10000 && val < 0x1000000000000) {
            MsvcString s = ReadMsvcString(addr);
            if (s.ok && s.value.size() >= 1 && s.value.size() <= 128)
               os << ",\"msvc_str\":\"" << JsonEscape(s.value) << "\"";
        }
        os << "}";
        if (i + 1 < offsets.size()) os << ",";

        if (!val) break;  // Stop at null
        cur = val;
    }

    os << "]}\n";
    return os.str();
}

// /resolve-api enhanced: also returns code-window for each found API
// for quick disassembly inspection of the wrapper functions.
std::string CmdResolveApiEnhanced() {
    ModuleInfo mod;
    std::string error;
    {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        mod = g_state.mod;
    }
    if (!mod.base) return "{\"error\":\"no module; run /scan\"}\n";

    g_state.apiResult = ResolveFmodApis(mod, 0);

    std::ostringstream os;
    os << "{\"command\":\"fmod/resolve-api\",\"entries\":[";
    for (size_t i = 0; i < g_state.apiResult.entries.size(); ++i) {
        const auto& e = g_state.apiResult.entries[i];
        if (i) os << ",";
        os << "{\"name\":\"" << JsonEscape(e.name)
           << "\",\"found\":" << (e.found ? "true" : "false");
        if (e.found) {
            os << ",\"func_rva\":\"0x" << std::hex << std::uppercase << e.funcRva << std::dec << "\""
               << ",\"func_addr\":\"" << Hex(e.funcAddr) << "\""
               << ",\"lea_rva\":\"0x" << std::hex << std::uppercase << e.leaRva << std::dec << "\"";
        }
        if (!e.note.empty())
            os << ",\"note\":\"" << JsonEscape(e.note) << "\"";
        os << "}";
    }
    os << "]}\n";
    return os.str();
}

// ── FMOD API Direct Call ─────────────────────────────────────
// Wrappers are global functions taking SystemI* as first param (x64 fastcall).
// Addresses from resolve-api + code-window analysis of the game exe.

std::string CmdFmodCallGetMasterCG() {
    std::string error;
    uintptr_t systemI = 0;
    uintptr_t exeBase = 0;
    {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        exeBase = g_state.mod.base;
        if (g_state.lock.locked && g_state.lock.refcount) {
            uintptr_t fs = 0;
            SafeRead(g_state.lock.refcount + 0x18, &fs);
            if (fs) SafeRead(fs + 0xC0, &systemI);
        }
        if (!systemI && !g_state.candidates.empty()) {
            uintptr_t cRefcount = g_state.candidates.front().refcount;
            uintptr_t fs = 0;
            SafeRead(cRefcount + 0x18, &fs);
            if (fs) SafeRead(fs + 0xC0, &systemI);
        }
    }
    if (!systemI) { error = "SystemI not found; run /scan and /lock first"; }
    else if (!exeBase) { error = "module base not known; run /scan first"; }

    if (error.empty()) {
        uintptr_t fnAddr = exeBase + 0x5715FC0;
        void* masterCG = nullptr;
        uintptr_t result = 0;
        bool crashed = !SafeCallRaw4(fnAddr, systemI, reinterpret_cast<uintptr_t>(&masterCG), 0, 0, &result);

        if (!crashed) {
            std::lock_guard<std::mutex> lock(g_stateMutex);
            g_state.lastCommand = "fmod/call-get-master-cg";
            g_state.lastError.clear();
            std::ostringstream os;
            os << "{"
               << "\"command\":\"fmod/call-get-master-cg\","
               << "\"systemI\":\"" << Hex(systemI) << "\","
               << "\"wrapper_addr\":\"" << Hex(fnAddr) << "\","
               << "\"fmod_result\":" << result << ","
               << "\"master_cg\":\"" << Hex(reinterpret_cast<uintptr_t>(masterCG)) << "\","
               << "\"module_base\":\"" << Hex(exeBase) << "\""
               << "}\n";
            AppendLog("fmod-call-get-master-cg: systemI=%s result=%lu masterCG=%s",
                Hex(systemI).c_str(), static_cast<unsigned long>(result), Hex(reinterpret_cast<uintptr_t>(masterCG)).c_str());
            return os.str();
        } else {
            error = "SEH exception calling getMasterChannelGroup at " + Hex(fnAddr);
        }
    }

    {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        g_state.lastCommand = "fmod/call-get-master-cg";
        g_state.lastError = error;
        AppendLog("fmod-call-get-master-cg: ERROR: %s", error.c_str());
    }
    std::ostringstream os;
    os << "{\"command\":\"fmod/call-get-master-cg\",\"error\":\"" << JsonEscape(error) << "\"}\n";
    return os.str();
}

// /fmod/cg-get-num-groups  — getNumGroups on the master CG
std::string CmdCgGetNumGroups() {
    // First get master CG
    uintptr_t systemI = 0, exeBase = 0, masterCG = 0;
    {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        exeBase = g_state.mod.base;
        if (g_state.lock.locked && g_state.lock.refcount) {
            uintptr_t fs = 0;
            SafeRead(g_state.lock.refcount + 0x18, &fs);
            if (fs) SafeRead(fs + 0xC0, &systemI);
        }
    }
    if (!systemI || !exeBase) {
        std::ostringstream os;
        os << "{\"error\":\"SystemI not available; run /scan and /lock\"}\n";
        return os.str();
    }

    // getMasterChannelGroup
    {
        typedef int (*GetMCG)(void*, void**);
        auto fn = reinterpret_cast<GetMCG>(exeBase + 0x5715FC0);
        int r = fn(reinterpret_cast<void*>(systemI), reinterpret_cast<void**>(&masterCG));
        if (r != 0 || !masterCG) {
            std::ostringstream os;
            os << "{\"error\":\"getMasterChannelGroup failed r=" << r << "\"}\n";
            return os.str();
        }
    }

    // getNumGroups
    int numGroups = -1;
    {
        typedef int (*GetNumGroups)(void*, int*);
        auto fn = reinterpret_cast<GetNumGroups>(exeBase + 0x571BC40);
        int r = fn(reinterpret_cast<void*>(masterCG), &numGroups);
        if (r != 0) {
            std::ostringstream os;
            os << "{\"error\":\"getNumGroups failed r=" << r << "\"}\n";
            return os.str();
        }
    }

    std::ostringstream os;
    os << "{"
       << "\"command\":\"fmod/cg-get-num-groups\","
       << "\"systemI\":\"" << Hex(systemI) << "\","
       << "\"master_cg\":\"" << Hex(masterCG) << "\","
       << "\"num_groups\":" << numGroups
       << "}\n";
    AppendLog("cg-get-num-groups: masterCG=%s numGroups=%d", Hex(masterCG).c_str(), numGroups);
    return os.str();
}

// /fmod/cg-get-group?index=N  — getGroup(masterCG, index, &childCG)
std::string CmdCgGetGroup(int index) {
    uintptr_t systemI = 0, exeBase = 0, masterCG = 0;
    {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        exeBase = g_state.mod.base;
        if (g_state.lock.locked && g_state.lock.refcount) {
            uintptr_t fs = 0;
            SafeRead(g_state.lock.refcount + 0x18, &fs);
            if (fs) SafeRead(fs + 0xC0, &systemI);
        }
    }
    if (!systemI || !exeBase) {
        std::ostringstream os;
        os << "{\"error\":\"SystemI not available\"}\n";
        return os.str();
    }

    {
        typedef int (*GetMCG)(void*, void**);
        auto fn = reinterpret_cast<GetMCG>(exeBase + 0x5715FC0);
        int r = fn(reinterpret_cast<void*>(systemI), reinterpret_cast<void**>(&masterCG));
        if (r != 0 || !masterCG) {
            std::ostringstream os;
            os << "{\"error\":\"getMasterChannelGroup failed r=" << r << "\"}\n";
            return os.str();
        }
    }

    void* childCG = nullptr;
    {
        typedef int (*GetGroup)(void*, int, void**);
        auto fn = reinterpret_cast<GetGroup>(exeBase + 0x571BC40);
        int r = fn(reinterpret_cast<void*>(masterCG), index, &childCG);
        if (r != 0) {
            std::ostringstream os;
            os << "{\"error\":\"getGroup(" << index << ") failed r=" << r << "\"}\n";
            return os.str();
        }
    }

    std::ostringstream os;
    os << "{"
       << "\"command\":\"fmod/cg-get-group\","
       << "\"master_cg\":\"" << Hex(masterCG) << "\","
       << "\"index\":" << index << ","
       << "\"child_cg\":\"" << Hex(reinterpret_cast<uintptr_t>(childCG)) << "\""
       << "}\n";
    AppendLog("cg-get-group: masterCG=%s idx=%d childCG=%s",
        Hex(masterCG).c_str(), index, Hex(reinterpret_cast<uintptr_t>(childCG)).c_str());
    return os.str();
}

// /fmod/cg-get-child?index=N  — vtable-based getGroup
std::string CmdCgGetChildGroup(int groupIndex) {
    uintptr_t systemI = 0, exeBase = 0, masterCG = 0;
    {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        exeBase = g_state.mod.base;
        if (g_state.lock.locked && g_state.lock.refcount) {
            uintptr_t fs = 0;
            SafeRead(g_state.lock.refcount + 0x18, &fs);
            if (fs) SafeRead(fs + 0xC0, &systemI);
        }
    }
    if (!systemI || !exeBase)
        return "{\"error\":\"SystemI not available\"}\n";

    {
        typedef int (*GetMCG)(void*, void**);
        auto fn = reinterpret_cast<GetMCG>(exeBase + 0x5715FC0);
        int r = fn(reinterpret_cast<void*>(systemI), reinterpret_cast<void**>(&masterCG));
        if (r != 0 || !masterCG)
            return "{\"error\":\"getMasterChannelGroup failed\"}\n";
    }

    // Read CG vtable
    uintptr_t vtable = 0;
    SafeRead(masterCG, &vtable);

    // Try vtable indices 8, 9, 10, 11 as getGroup candidates
    // getGroup(int index, ChannelGroup** out): rcx=this, rdx=index, r8=&out
    void* childCG = nullptr;
    int usedIndex = -1;
    for (int vi = 8; vi <= 11; ++vi) {
        uintptr_t method = 0;
        SafeRead(vtable + vi * sizeof(uintptr_t), &method);
        if (!method) continue;

        uintptr_t rax = 0;
        bool crashed = !SafeCallRaw4(method,
            masterCG,
            static_cast<uintptr_t>(groupIndex),
            reinterpret_cast<uintptr_t>(&childCG),
            0, &rax);

        if (!crashed && rax == 0 && childCG) {
            usedIndex = vi;
            break;
        }
        childCG = nullptr;
    }

    std::ostringstream os;
    os << "{"
       << "\"command\":\"fmod/cg-get-child\","
       << "\"master_cg\":\"" << Hex(masterCG) << "\","
       << "\"vtable\":\"" << Hex(vtable) << "\","
       << "\"group_index\":" << groupIndex << ","
       << "\"vtable_index\":" << usedIndex << ","
       << "\"child_cg\":\"" << Hex(reinterpret_cast<uintptr_t>(childCG)) << "\""
       << "}\n";
    AppendLog("cg-get-child: masterCG=%s idx=%d vtableIdx=%d childCG=%s",
        Hex(masterCG).c_str(), groupIndex, usedIndex, Hex(reinterpret_cast<uintptr_t>(childCG)).c_str());
    return os.str();
}

// /vcall-buf?object=HEX&index=N  — vcall with out1 as rdx (for 2-param getters)
std::string CmdVCallBuf(uintptr_t obj, int idx) {
    if (!obj) return "{\"error\":\"missing object\"}\n";
    uintptr_t vtable = 0; SafeRead(obj, &vtable);
    if (!vtable) return "{\"error\":\"cannot read vtable\"}\n";
    uintptr_t method = 0; SafeRead(vtable + idx * sizeof(uintptr_t), &method);
    if (!method) return "{\"error\":\"vtable entry null\"}\n";

    uintptr_t out1 = 0, rax = 0;
    bool crashed = !SafeCallRaw4(method, obj,
        reinterpret_cast<uintptr_t>(&out1), 0, 0, &rax);

    std::ostringstream os;
    os << "{\"command\":\"vcall-buf\",\"object\":\"" << Hex(obj)
       << "\",\"index\":" << idx << ",\"method\":\"" << Hex(method)
       << "\",\"rax\":" << rax << ",\"out1\":\"" << Hex(out1) << "\""
       << ",\"crashed\":" << (crashed ? "true" : "false") << "}\n";
    return os.str();
}

std::string CmdVCallOut(uintptr_t obj, int idx, uintptr_t rdxVal) {
    if (!obj) return "{\"error\":\"missing object\"}\n";
    uintptr_t vtable = 0; SafeRead(obj, &vtable);
    if (!vtable) return "{\"error\":\"cannot read vtable\"}\n";
    uintptr_t method = 0; SafeRead(vtable + idx * sizeof(uintptr_t), &method);
    if (!method) return "{\"error\":\"vtable entry null\"}\n";

    uintptr_t out1 = 0, out2 = 0, rax = 0;
    bool crashed = !SafeCallRaw4(method, obj, rdxVal,
        reinterpret_cast<uintptr_t>(&out1), reinterpret_cast<uintptr_t>(&out2), &rax);

    ModuleInfo mod; { std::lock_guard<std::mutex> lk(g_stateMutex); mod = g_state.mod; }
    std::ostringstream os;
    os << "{\"command\":\"vcall-out\",\"object\":\"" << Hex(obj)
       << "\",\"vtable\":\"" << Hex(vtable) << "\",\"index\":" << idx
       << ",\"method\":\"" << Hex(method) << "\",\"rdx\":\"" << Hex(rdxVal)
       << "\",\"rax\":" << rax
       << ",\"out1\":\"" << Hex(out1) << "\",\"out1_in_mod\":" << (InModule(mod, out1) ? "true" : "false")
       << ",\"out2\":\"" << Hex(out2) << "\",\"out2_in_mod\":" << (InModule(mod, out2) ? "true" : "false")
       << ",\"crashed\":" << (crashed ? "true" : "false") << "}\n";
    return os.str();
}

std::string CmdCgVCall(int vtableIndex) {
    uintptr_t systemI = 0, exeBase = 0, masterCG = 0;
    {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        exeBase = g_state.mod.base;
        if (g_state.lock.locked && g_state.lock.refcount) {
            uintptr_t fs = 0;
            SafeRead(g_state.lock.refcount + 0x18, &fs);
            if (fs) SafeRead(fs + 0xC0, &systemI);
        }
    }
    if (!systemI || !exeBase)
        return "{\"error\":\"SystemI not available\"}\n";

    {
        typedef int (*GetMCG)(void*, void**);
        auto fn = reinterpret_cast<GetMCG>(exeBase + 0x5715FC0);
        int r = fn(reinterpret_cast<void*>(systemI), reinterpret_cast<void**>(&masterCG));
        if (r != 0 || !masterCG)
            return "{\"error\":\"getMasterChannelGroup failed\"}\n";
    }

    // Read CG vtable
    uintptr_t vtable = 0;
    SafeRead(masterCG, &vtable);
    if (!vtable) return "{\"error\":\"cannot read CG vtable\"}\n";

    // Read vtable entry
    uintptr_t method = 0;
    SafeRead(vtable + vtableIndex * sizeof(uintptr_t), &method);
    if (!method) return "{\"error\":\"vtable entry is null\"}\n";

    // Output buffer on stack
    uintptr_t out1 = 0, out2 = 0;

    uintptr_t rax = 0;
    bool crashed = !SafeCallRaw4(method,
        masterCG,
        reinterpret_cast<uintptr_t>(&out1),
        reinterpret_cast<uintptr_t>(&out2),
        0,
        &rax);

    std::ostringstream os;
    os << "{"
       << "\"command\":\"fmod/cg-vcall\","
       << "\"master_cg\":\"" << Hex(masterCG) << "\","
       << "\"vtable\":\"" << Hex(vtable) << "\","
       << "\"index\":" << vtableIndex << ","
       << "\"method\":\"" << Hex(method) << "\","
       << "\"rax\":" << rax << ","
       << "\"out1\":\"" << Hex(out1) << "\","
       << "\"out2\":\"" << Hex(out2) << "\","
       << "\"crashed\":" << (crashed ? "true" : "false")
       << "}\n";
    return os.str();
}

// /fmod/cg-scan-getgroup  — brute force scan all vtable indices for getGroup
std::string CmdCgScanGetGroup() {
    uintptr_t systemI = 0, exeBase = 0, masterCG = 0;
    {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        exeBase = g_state.mod.base;
        if (g_state.lock.locked && g_state.lock.refcount) {
            uintptr_t fs = 0;
            SafeRead(g_state.lock.refcount + 0x18, &fs);
            if (fs) SafeRead(fs + 0xC0, &systemI);
        }
    }
    if (!systemI || !exeBase)
        return "{\"error\":\"SystemI not available\"}\n";

    {
        typedef int (*GetMCG)(void*, void**);
        auto fn = reinterpret_cast<GetMCG>(exeBase + 0x5715FC0);
        int r = fn(reinterpret_cast<void*>(systemI), reinterpret_cast<void**>(&masterCG));
        if (r != 0 || !masterCG)
            return "{\"error\":\"getMasterChannelGroup failed\"}\n";
    }

    uintptr_t vtable = 0;
    SafeRead(masterCG, &vtable);

    std::ostringstream os;
    os << "{\"command\":\"fmod/cg-scan-getgroup\","
       << "\"master_cg\":\"" << Hex(masterCG) << "\","
       << "\"vtable\":\"" << Hex(vtable) << "\","
       << "\"hits\":[";

    bool first = true;
    for (int vi = 0; vi < 80; ++vi) {
        uintptr_t method = 0;
        SafeRead(vtable + vi * sizeof(uintptr_t), &method);
        if (!method) continue;
        if (!InModule(g_state.mod, method)) break; // vtable end

        void* child = nullptr;
        uintptr_t rax = 0;
        bool crashed = !SafeCallRaw4(method,
            masterCG, 0, reinterpret_cast<uintptr_t>(&child), 0, &rax);

        if (!crashed && rax == 0 && child != nullptr) {
            if (!first) os << ",";
            first = false;
            os << "{\"vi\":" << vi
               << ",\"method\":\"" << Hex(method)
               << "\",\"child\":\"" << Hex(reinterpret_cast<uintptr_t>(child)) << "\"}";
        }

        // Also try rdx=1, rdx=2 to see if those return different children
        if (!crashed && rax == 0 && child == nullptr) {
            // Maybe getGroup returned OK but child is null (no group at index 0)
            // Try index 1
            void* child2 = nullptr;
            uintptr_t rax2 = 0;
            SafeCallRaw4(method, masterCG, 1, reinterpret_cast<uintptr_t>(&child2), 0, &rax2);
            if (rax2 == 0 && child2 != nullptr) {
                if (!first) os << ",";
                first = false;
                os << "{\"vi\":" << vi
                   << ",\"method\":\"" << Hex(method)
                   << "\",\"child_idx1\":\"" << Hex(reinterpret_cast<uintptr_t>(child2)) << "\"}";
            }
        }
    }

    os << "]}\n";
    return os.str();
}

std::string CmdFmodCallCreateSound() {
    std::string error;
    uintptr_t systemI = 0;
    uintptr_t exeBase = 0;
    {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        exeBase = g_state.mod.base;
        if (g_state.lock.locked && g_state.lock.refcount) {
            uintptr_t fs = 0;
            SafeRead(g_state.lock.refcount + 0x18, &fs);
            if (fs) SafeRead(fs + 0xC0, &systemI);
        }
        if (!systemI && !g_state.candidates.empty()) {
            uintptr_t cRefcount = g_state.candidates.front().refcount;
            uintptr_t fs = 0;
            SafeRead(cRefcount + 0x18, &fs);
            if (fs) SafeRead(fs + 0xC0, &systemI);
        }
    }
    if (!systemI || !exeBase) {
        error = "SystemI or module base not available; run /scan and /lock first";
    }

    if (error.empty()) {
        uintptr_t fnAddr = exeBase + 0x5714F90;

        // FMOD_CREATESOUNDEXINFO as used by the original mod (cbsize=0xE0, 224 bytes)
        struct CreateSoundExInfo {
            int cbsize; uint32_t length; uint32_t numchannels;
            int defaultfrequency; int format; uint32_t decodebuffersize;
        };
        char exRaw[0xE0] = {};
        auto* ex = reinterpret_cast<CreateSoundExInfo*>(exRaw);
        ex->cbsize = 0xE0;
        ex->length = 0xFFFFFFFF;
        ex->numchannels = 2;
        ex->defaultfrequency = 44100;
        ex->format = 2;
        ex->decodebuffersize = 0x800;

        void* sound = nullptr;
        uintptr_t result = 0;
        bool crashed = !SafeCallRaw4(fnAddr, systemI,
            reinterpret_cast<uintptr_t>(nullptr), 0x48a,
            reinterpret_cast<uintptr_t>(ex), &result);
        if (!crashed) {
            std::lock_guard<std::mutex> lock(g_stateMutex);
            g_state.lastCommand = "fmod/call-create-sound";
            g_state.lastError.clear();
            std::ostringstream os;
            os << "{"
               << "\"command\":\"fmod/call-create-sound\","
               << "\"systemI\":\"" << Hex(systemI) << "\","
               << "\"wrapper_addr\":\"" << Hex(fnAddr) << "\","
               << "\"fmod_result\":" << result << ","
               << "\"sound\":\"" << Hex(reinterpret_cast<uintptr_t>(sound)) << "\""
               << "}\n";
            AppendLog("fmod-call-create-sound: systemI=%s result=%d sound=%s",
                Hex(systemI).c_str(), result, Hex(reinterpret_cast<uintptr_t>(sound)).c_str());
            return os.str();
        }
    }

    {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        g_state.lastCommand = "fmod/call-create-sound";
        g_state.lastError = error;
        AppendLog("fmod-call-create-sound: ERROR: %s", error.c_str());
    }
    std::ostringstream os;
    os << "{\"command\":\"fmod/call-create-sound\",\"error\":\"" << JsonEscape(error) << "\"}\n";
    return os.str();
}

std::string CmdFmodGetSystemI() {
    std::lock_guard<std::mutex> lock(g_stateMutex);
    uintptr_t systemI = 0;
    int source = 0; // 0=none 1=lock 2=cached

    if (g_state.lock.locked && g_state.lock.refcount) {
        uintptr_t fs = 0;
        SafeRead(g_state.lock.refcount + 0x18, &fs);
        if (fs) SafeRead(fs + 0xC0, &systemI);
        if (systemI) source = 1;
    }
    if (!systemI && !g_state.candidates.empty()) {
        uintptr_t cRefcount = g_state.candidates.front().refcount;
        uintptr_t fs = 0;
        SafeRead(cRefcount + 0x18, &fs);
        if (fs) SafeRead(fs + 0xC0, &systemI);
        if (systemI) source = 2;
    }

    uintptr_t vtable = 0;
    if (systemI) SafeRead(systemI, &vtable);

    std::ostringstream os;
    os << "{"
       << "\"command\":\"fmod/get-systemi\","
       << "\"systemI\":\"" << Hex(systemI) << "\","
       << "\"vtable\":\"" << Hex(vtable) << "\","
       << "\"vtable_rva\":\"0x" << std::hex << std::uppercase
       << (vtable ? vtable - g_state.mod.base : 0) << std::dec << "\","
       << "\"source\":" << source << ","
       << "\"in_module\":" << (g_state.mod.base && vtable >= g_state.mod.base
                               && vtable < g_state.mod.base + g_state.mod.size ? "true" : "false")
       << "}\n";
    return os.str();
}

// ── Handle → Channel resolver ───────────────────────────────
// Resolves the uint32 FMOD Channel handle from refcount+0x30
// into a Channel* via the game's Handle::open wrapper.
// Uses native pattern scan to locate the resolver dynamically.
std::string CmdResolveChannel(bool unlock) {
    uintptr_t exeBase = 0;
    uint32_t handle = 0;
    {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        exeBase = g_state.mod.base;
        if (g_state.lock.locked && g_state.lock.refcount) {
            uint32_t tmp = 0;
            SafeRead(g_state.lock.refcount + 0x30, &tmp);
            handle = tmp;
        }
        if (!handle && !g_state.candidates.empty()) {
            uint32_t tmp = 0;
            SafeRead(g_state.candidates.front().refcount + 0x30, &tmp);
            handle = tmp;
        }
    }
    if (!exeBase) return "{\"command\":\"fmod/resolve-channel\",\"error\":\"no module; run /scan\"}\n";
    if (!handle)  return "{\"command\":\"fmod/resolve-channel\",\"error\":\"handle32 is 0; game may not be on Streamer Mode\"}\n";

    // Resolve function addresses via native pattern scan (dynamic, not hardcoded)
    ModuleInfo mod; { std::lock_guard<std::mutex> lk(g_stateMutex); mod = g_state.mod; }
    auto np = ResolveNativePatterns(mod);
    uintptr_t resolverAddr = 0;
    uintptr_t unlockAddr = 0;
    for (const auto& r : np) {
        if (r.name == "fmod_handle_resolver" && !r.hits.empty())
            resolverAddr = r.hits.front().addr;
        if (r.name == "fmod_handle_unlock" && !r.hits.empty())
            unlockAddr = r.hits.front().addr;
    }
    if (!resolverAddr) return "{\"command\":\"fmod/resolve-channel\",\"error\":\"fmod_handle_resolver not found by native pattern\"}\n";
    if (!InText(mod, resolverAddr)) {
        std::ostringstream os;
        os << "{\"command\":\"fmod/resolve-channel\",\"error\":\"resolver not in .text\""
           << ",\"addr\":\"" << Hex(resolverAddr) << "\""
           << ",\"rva\":\"0x" << std::hex << std::uppercase << (resolverAddr - mod.base) << std::dec << "\"}\n";
        return os.str();
    }

    // Handle::open(uint32_t handle, void** out_inst, uint64_t* out_kind) → uint32_t result
    uintptr_t channel = 0;
    uint64_t kind = 0;
    bool crashed = !SafeCallRaw3(resolverAddr, handle,
        reinterpret_cast<uintptr_t>(&channel),
        reinterpret_cast<uintptr_t>(&kind), nullptr);

    if (crashed || !channel) {
        std::ostringstream os;
        os << "{\"command\":\"fmod/resolve-channel\",\"handle\":" << handle
           << ",\"error\":\"fmod_handle_resolver crashed or returned null\""
           << ",\"crashed\":" << (crashed ? "true" : "false") << "}\n";
        return os.str();
    }

    // Verify channel vtable in module (reuse mod from above)
    uintptr_t channelVt = 0;
    SafeRead(channel, &channelVt);
    bool vtInModule = InModule(mod, channelVt);
    uint32_t vtRva = vtInModule ? static_cast<uint32_t>(channelVt - mod.base) : 0;

    // Handle::unlock(kind) — single uint64_t param
    if (unlock && kind && unlockAddr) {
        SafeCallRaw3(unlockAddr, kind, 0, 0, nullptr);
    }

    // Dump first 8 vtable entries of the channel
    uintptr_t vtDump[8] = {};
    for (int i = 0; i < 8; ++i)
        SafeRead(channelVt + i * sizeof(uintptr_t), &vtDump[i]);

    std::ostringstream os;
    os << "{"
       << "\"command\":\"fmod/resolve-channel\","
       << "\"resolver_rva\":\"0x" << std::hex << std::uppercase << (resolverAddr - mod.base) << std::dec << "\","
       << "\"handle\":" << handle << ","
       << "\"channel\":\"" << Hex(channel) << "\","
       << "\"kind\":\"" << Hex(kind) << "\","
       << "\"channel_vtable\":\"" << Hex(channelVt) << "\","
       << "\"vtable_rva\":\"0x" << std::hex << std::uppercase << vtRva << std::dec << "\","
       << "\"vtable_in_module\":" << (vtInModule ? "true" : "false") << ","
       << "\"unlocked\":" << (unlock ? "true" : "false") << ","
       << "\"vtable_sample\":[";
    for (int i = 0; i < 8; ++i) {
        if (i) os << ",";
        os << "{"
           << "\"idx\":" << i << ","
           << "\"addr\":\"" << Hex(vtDump[i]) << "\""
           << ",\"in_module\":" << (InModule(mod, vtDump[i]) ? "true" : "false");
        if (InModule(mod, vtDump[i]))
            os << ",\"rva\":\"0x" << std::hex << std::uppercase << (vtDump[i] - mod.base) << std::dec << "\"";
        os << "}";
    }
    os << "]}\n";

    AppendLog("resolve-channel: handle=%u resolver_rva=0x%X channel=%s vtable=%s vtInModule=%d kind=0x%llX",
        handle, static_cast<uint32_t>(resolverAddr - mod.base),
        Hex(channel).c_str(), Hex(channelVt).c_str(), vtInModule ? 1 : 0,
        static_cast<unsigned long long>(kind));
    return os.str();
}

std::string CmdStopActive() {
    std::lock_guard<std::mutex> lock(g_stateMutex);
    if (!g_state.topo.matched || !g_state.topo.matchedChannel) {
        g_state.lastCommand = "game/stop-active";
        g_state.lastError = "no matched channel; run /fmod/topology-dump first";
        return BuildStatusJson(g_state);
    }
    g_state.lastCommand = "game/stop-active";
    g_state.lastError = "Channel::stop vtable offset not resolved; FMOD function call not wired yet";
    return BuildStatusJson(g_state);
}

void SendJson(httplib::Response& res, const std::string& body) {
    res.set_content(body, "application/json; charset=utf-8");
}

DWORD ProbeMain() {
    AppendLog("probe http controller starting, pid=%lu", GetCurrentProcessId());
    {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        g_state.phase = "http_listening";
        g_state.lastCommand = "startup";
        g_state.lastError.clear();
    }
    WriteStatusJson("http_listening", "controller listening at http://0.0.0.0:8104", 0);

    httplib::Server server;
    server.Get("/state", [](const httplib::Request&, httplib::Response& res) {
        std::lock_guard<std::mutex> lock(g_stateMutex);
        SendJson(res, BuildStatusJson(g_state));
    });
    server.Post("/scan", [](const httplib::Request&, httplib::Response& res) {
        SendJson(res, RunScan("scan"));
    });
    server.Post("/meta", [](const httplib::Request& req, httplib::Response& res) {
        std::string title = req.has_param("title") ? req.get_param_value("title") : "";
        std::string artist = req.has_param("artist") ? req.get_param_value("artist") : "";
        std::string sound = req.has_param("sound") ? req.get_param_value("sound") : "";
        SendJson(res, ApplyMetadata(title, artist, sound));
    });
    server.Post("/meta/restore", [](const httplib::Request&, httplib::Response& res) {
        SendJson(res, RestoreMetadata());
    });
    server.Post("/skip", [](const httplib::Request&, httplib::Response& res) {
        SendJson(res, MarkPendingCommand("skip", "FMOD/PCM bridge not wired"));
    });
    server.Post("/tone", [](const httplib::Request& req, httplib::Response& res) {
        std::string id = req.has_param("id") ? req.get_param_value("id") : "";
        SendJson(res, MarkPendingCommand("tone:" + id, "FMOD/PCM bridge not wired"));
    });

    // ── Phase 1: Lock + Live ────────────────────────────────
    server.Post("/lock", [](const httplib::Request&, httplib::Response& res) {
        SendJson(res, CmdLock());
    });
    server.Post("/unlock", [](const httplib::Request&, httplib::Response& res) {
        SendJson(res, CmdUnlock());
    });
    server.Get("/live", [](const httplib::Request&, httplib::Response& res) {
        SendJson(res, CmdLive());
    });

    // ── Phase 2a: Handle Path (pure reads) ──────────────────
    server.Post("/fmod/resolve-native-patterns", [](const httplib::Request&, httplib::Response& res) {
        SendJson(res, CmdResolveNativePatterns());
    });
    server.Post("/fmod/find-systemi-contexts", [](const httplib::Request& req, httplib::Response& res) {
        size_t maxHits = 32;
        if (req.has_param("max")) maxHits = static_cast<size_t>(std::stoul(req.get_param_value("max"), nullptr, 0));
        if (maxHits == 0) maxHits = 32;
        if (maxHits > 256) maxHits = 256;
        SendJson(res, CmdFindSystemIContexts(maxHits));
    });
    server.Post("/fmod/find-systemi-contexts-strict", [](const httplib::Request& req, httplib::Response& res) {
        size_t maxHits = 16;
        if (req.has_param("max")) maxHits = static_cast<size_t>(std::stoul(req.get_param_value("max"), nullptr, 0));
        if (maxHits == 0) maxHits = 16;
        if (maxHits > 128) maxHits = 128;
        SendJson(res, CmdFindSystemIContextsStrict(maxHits));
    });
    server.Get("/fmod/systemi-vtable", [](const httplib::Request& req, httplib::Response& res) {
        uintptr_t systemI = 0;
        int count = 128;
        if (req.has_param("system")) systemI = std::stoull(req.get_param_value("system"), nullptr, 16);
        if (req.has_param("count")) count = std::stoi(req.get_param_value("count"), nullptr, 0);
        SendJson(res, CmdSystemIVtable(systemI, count));
    });
    server.Get("/fmod/native-xrefs", [](const httplib::Request& req, httplib::Response& res) {
        std::string name = req.has_param("name") ? req.get_param_value("name") : "fmod_handle_resolver";
        uintptr_t rva = 0;
        if (req.has_param("rva")) rva = std::stoull(req.get_param_value("rva"), nullptr, 16);
        SendJson(res, CmdNativeXrefs(name, rva));
    });
    server.Get("/fmod/code-window", [](const httplib::Request& req, httplib::Response& res) {
        uintptr_t rva = 0;
        size_t span = 0x120;
        if (req.has_param("rva")) rva = std::stoull(req.get_param_value("rva"), nullptr, 16);
        if (req.has_param("span")) span = static_cast<size_t>(std::stoul(req.get_param_value("span"), nullptr, 16));
        SendJson(res, CmdCodeWindow(rva, span));
    });
    server.Get("/fmod/vtable", [](const httplib::Request& req, httplib::Response& res) {
        uintptr_t addr = 0, rva = 0;
        size_t count = 32;
        if (req.has_param("addr")) addr = std::stoull(req.get_param_value("addr"), nullptr, 16);
        if (req.has_param("rva")) rva = std::stoull(req.get_param_value("rva"), nullptr, 16);
        if (req.has_param("count")) count = static_cast<size_t>(std::stoul(req.get_param_value("count"), nullptr, 0));
        SendJson(res, CmdVtableDump(addr, rva, count));
    });

    // Deprecated: kept only as a historical comparison. This path was disproven.
    server.Post("/fmod/handle-path", [](const httplib::Request&, httplib::Response& res) {
        SendJson(res, CmdHandlePath());
    });

    // ── Phase 2b: FMOD API Resolver ─────────────────────────
    server.Post("/fmod/resolve-api", [](const httplib::Request&, httplib::Response& res) {
        SendJson(res, CmdResolveApi());
    });

    // ── Phase 2c: Topology Dump ─────────────────────────────
    // Deprecated: this still uses the old fmod_sound -> SystemI guess.
    server.Post("/fmod/topology-dump", [](const httplib::Request&, httplib::Response& res) {
        SendJson(res, CmdTopologyDump());
    });

    // ── Phase 2d: Dump tools ────────────────────────────────
    server.Get("/dump/stream", [](const httplib::Request& req, httplib::Response& res) {
        uintptr_t span = 0x100;
        if (req.has_param("span")) span = std::stoul(req.get_param_value("span"), nullptr, 16);
        SendJson(res, CmdDumpStream(span));
    });
    server.Get("/dump/ptr", [](const httplib::Request& req, httplib::Response& res) {
        uintptr_t addr = 0;
        if (req.has_param("addr")) addr = std::stoull(req.get_param_value("addr"), nullptr, 16);
        uintptr_t span = 0x100;
        if (req.has_param("span")) span = std::stoul(req.get_param_value("span"), nullptr, 16);
        SendJson(res, CmdDumpPtr(addr, span));
    });
    server.Get("/dump/graph", [](const httplib::Request&, httplib::Response& res) {
        SendJson(res, CmdDumpGraph());
    });
    server.Get("/dump/playback-node", [](const httplib::Request&, httplib::Response& res) {
        SendJson(res, CmdDumpPlaybackNode());
    });

    // ── Phase 3: Generic Debug & FMOD Calls ───────────────────
    server.Post("/call", [](const httplib::Request& req, httplib::Response& res) {
        SendJson(res, CmdCall(
            req.has_param("addr") ? req.get_param_value("addr") : "",
            req.has_param("rcx")  ? req.get_param_value("rcx") : "",
            req.has_param("rdx")  ? req.get_param_value("rdx") : "",
            req.has_param("r8")   ? req.get_param_value("r8") : "",
            req.has_param("r9")   ? req.get_param_value("r9") : ""));
    });
    server.Post("/vcall", [](const httplib::Request& req, httplib::Response& res) {
        SendJson(res, CmdVCall(
            req.has_param("object") ? req.get_param_value("object") : "",
            req.has_param("index")  ? req.get_param_value("index") : "",
            req.has_param("rcx")    ? req.get_param_value("rcx") : "",
            req.has_param("rdx")    ? req.get_param_value("rdx") : "",
            req.has_param("r8")     ? req.get_param_value("r8") : ""));
    });
    server.Get("/ptrchain", [](const httplib::Request& req, httplib::Response& res) {
        SendJson(res, CmdPtrChain(
            req.has_param("base")    ? req.get_param_value("base") : "",
            req.has_param("offsets") ? req.get_param_value("offsets") : ""));
    });
    server.Get("/fmod/get-systemi", [](const httplib::Request&, httplib::Response& res) {
        SendJson(res, CmdFmodGetSystemI());
    });
    server.Get("/fmod/resolve-channel", [](const httplib::Request& req, httplib::Response& res) {
        bool unlock = req.has_param("unlock") && req.get_param_value("unlock") == "1";
        SendJson(res, CmdResolveChannel(unlock));
    });
    server.Post("/fmod/call-get-master-cg", [](const httplib::Request&, httplib::Response& res) {
        SendJson(res, CmdFmodCallGetMasterCG());
    });
    server.Post("/fmod/call-create-sound", [](const httplib::Request&, httplib::Response& res) {
        SendJson(res, CmdFmodCallCreateSound());
    });
    server.Get("/fmod/cg-get-num-groups", [](const httplib::Request&, httplib::Response& res) {
        SendJson(res, CmdCgGetNumGroups());
    });
    server.Get("/fmod/cg-get-group", [](const httplib::Request& req, httplib::Response& res) {
        int idx = req.has_param("index") ? std::stoi(req.get_param_value("index")) : 0;
        SendJson(res, CmdCgGetGroup(idx));
    });
    server.Get("/fmod/cg-vcall", [](const httplib::Request& req, httplib::Response& res) {
        int idx = req.has_param("index") ? std::stoi(req.get_param_value("index")) : 0;
        SendJson(res, CmdCgVCall(idx));
    });
    server.Get("/fmod/cg-scan-getgroup", [](const httplib::Request&, httplib::Response& res) {
        SendJson(res, CmdCgScanGetGroup());
    });
    server.Get("/vcall-out", [](const httplib::Request& req, httplib::Response& res) {
        uintptr_t obj = req.has_param("object") ? std::stoull(req.get_param_value("object"), nullptr, 16) : 0;
        int idx = req.has_param("index") ? std::stoi(req.get_param_value("index")) : 0;
        uintptr_t rd = req.has_param("rdx") ? std::stoull(req.get_param_value("rdx"), nullptr, 16) : 0;
        SendJson(res, CmdVCallOut(obj, idx, rd));
    });
    server.Get("/vcall-buf", [](const httplib::Request& req, httplib::Response& res) {
        uintptr_t obj = req.has_param("object") ? std::stoull(req.get_param_value("object"), nullptr, 16) : 0;
        int idx = req.has_param("index") ? std::stoi(req.get_param_value("index")) : 0;
        SendJson(res, CmdVCallBuf(obj, idx));
    });

    // ── Phase 4: Game Controls ──────────────────────────────
    server.Post("/game/stop-active", [](const httplib::Request&, httplib::Response& res) {
        SendJson(res, CmdStopActive());
    });

    AppendLog("HTTP controller listening at http://0.0.0.0:8104");
    if (!server.listen("0.0.0.0", 8104)) {
        AppendLog("HTTP controller failed to listen on 0.0.0.0:8104");
        std::lock_guard<std::mutex> lock(g_stateMutex);
        g_state.phase = "error";
        g_state.lastError = "failed to listen on 0.0.0.0:8104";
    }
    AppendLog("probe http controller stopped");
    return 0;
}

DWORD WINAPI ProbeThread(void*) {
    __try {
        return ProbeMain();
    } __except (EXCEPTION_EXECUTE_HANDLER) {
        AppendLog("probe crashed with SEH exception 0x%08lX", GetExceptionCode());
        WriteStatusJson("crashed", "SEH exception during probe; see forza_radio_probe.log", -1);
        return 0;
    }
}

} // namespace

// All 17 version.dll proxy exports
extern "C" __declspec(dllexport) BOOL WINAPI GetFileVersionInfoA(LPCSTR a,DWORD b,DWORD c,LPVOID d){
    auto fn=RealProc<BOOL(WINAPI*)(LPCSTR,DWORD,DWORD,LPVOID)>("GetFileVersionInfoA");return fn?fn(a,b,c,d):FALSE;}
extern "C" __declspec(dllexport) BOOL WINAPI GetFileVersionInfoW(LPCWSTR a,DWORD b,DWORD c,LPVOID d){
    auto fn=RealProc<BOOL(WINAPI*)(LPCWSTR,DWORD,DWORD,LPVOID)>("GetFileVersionInfoW");return fn?fn(a,b,c,d):FALSE;}
extern "C" __declspec(dllexport) BOOL WINAPI GetFileVersionInfoExA(DWORD a,LPCSTR b,DWORD c,DWORD d,LPVOID e){
    auto fn=RealProc<BOOL(WINAPI*)(DWORD,LPCSTR,DWORD,DWORD,LPVOID)>("GetFileVersionInfoExA");return fn?fn(a,b,c,d,e):FALSE;}
extern "C" __declspec(dllexport) BOOL WINAPI GetFileVersionInfoExW(DWORD a,LPCWSTR b,DWORD c,DWORD d,LPVOID e){
    auto fn=RealProc<BOOL(WINAPI*)(DWORD,LPCWSTR,DWORD,DWORD,LPVOID)>("GetFileVersionInfoExW");return fn?fn(a,b,c,d,e):FALSE;}
extern "C" __declspec(dllexport) DWORD WINAPI GetFileVersionInfoSizeA(LPCSTR a,LPDWORD b){
    auto fn=RealProc<DWORD(WINAPI*)(LPCSTR,LPDWORD)>("GetFileVersionInfoSizeA");return fn?fn(a,b):0;}
extern "C" __declspec(dllexport) DWORD WINAPI GetFileVersionInfoSizeW(LPCWSTR a,LPDWORD b){
    auto fn=RealProc<DWORD(WINAPI*)(LPCWSTR,LPDWORD)>("GetFileVersionInfoSizeW");return fn?fn(a,b):0;}
extern "C" __declspec(dllexport) DWORD WINAPI GetFileVersionInfoSizeExA(DWORD a,LPCSTR b,LPDWORD c){
    auto fn=RealProc<DWORD(WINAPI*)(DWORD,LPCSTR,LPDWORD)>("GetFileVersionInfoSizeExA");return fn?fn(a,b,c):0;}
extern "C" __declspec(dllexport) DWORD WINAPI GetFileVersionInfoSizeExW(DWORD a,LPCWSTR b,LPDWORD c){
    auto fn=RealProc<DWORD(WINAPI*)(DWORD,LPCWSTR,LPDWORD)>("GetFileVersionInfoSizeExW");return fn?fn(a,b,c):0;}
extern "C" __declspec(dllexport) BOOL WINAPI GetFileVersionInfoByHandle(DWORD a,DWORD b,DWORD c,LPVOID d){
    auto fn=RealProc<BOOL(WINAPI*)(DWORD,DWORD,DWORD,LPVOID)>("GetFileVersionInfoByHandle");return fn?fn(a,b,c,d):FALSE;}
extern "C" __declspec(dllexport) DWORD WINAPI VerFindFileA(DWORD a,LPCSTR b,LPCSTR c,LPCSTR d,LPSTR e,PUINT f,LPSTR g,PUINT h){
    auto fn=RealProc<DWORD(WINAPI*)(DWORD,LPCSTR,LPCSTR,LPCSTR,LPSTR,PUINT,LPSTR,PUINT)>("VerFindFileA");return fn?fn(a,b,c,d,e,f,g,h):0;}
extern "C" __declspec(dllexport) DWORD WINAPI VerFindFileW(DWORD a,LPCWSTR b,LPCWSTR c,LPCWSTR d,LPWSTR e,PUINT f,LPWSTR g,PUINT h){
    auto fn=RealProc<DWORD(WINAPI*)(DWORD,LPCWSTR,LPCWSTR,LPCWSTR,LPWSTR,PUINT,LPWSTR,PUINT)>("VerFindFileW");return fn?fn(a,b,c,d,e,f,g,h):0;}
extern "C" __declspec(dllexport) DWORD WINAPI VerInstallFileA(DWORD a,LPCSTR b,LPCSTR c,LPCSTR d,LPCSTR e,LPCSTR f,LPSTR g,PUINT h){
    auto fn=RealProc<DWORD(WINAPI*)(DWORD,LPCSTR,LPCSTR,LPCSTR,LPCSTR,LPCSTR,LPSTR,PUINT)>("VerInstallFileA");return fn?fn(a,b,c,d,e,f,g,h):0;}
extern "C" __declspec(dllexport) DWORD WINAPI VerInstallFileW(DWORD a,LPCWSTR b,LPCWSTR c,LPCWSTR d,LPCWSTR e,LPCWSTR f,LPWSTR g,PUINT h){
    auto fn=RealProc<DWORD(WINAPI*)(DWORD,LPCWSTR,LPCWSTR,LPCWSTR,LPCWSTR,LPCWSTR,LPWSTR,PUINT)>("VerInstallFileW");return fn?fn(a,b,c,d,e,f,g,h):0;}
extern "C" __declspec(dllexport) DWORD WINAPI VerLanguageNameA(DWORD a,LPSTR b,DWORD c){
    auto fn=RealProc<DWORD(WINAPI*)(DWORD,LPSTR,DWORD)>("VerLanguageNameA");return fn?fn(a,b,c):0;}
extern "C" __declspec(dllexport) DWORD WINAPI VerLanguageNameW(DWORD a,LPWSTR b,DWORD c){
    auto fn=RealProc<DWORD(WINAPI*)(DWORD,LPWSTR,DWORD)>("VerLanguageNameW");return fn?fn(a,b,c):0;}
extern "C" __declspec(dllexport) BOOL WINAPI VerQueryValueA(LPCVOID a,LPCSTR b,LPVOID* c,PUINT d){
    auto fn=RealProc<BOOL(WINAPI*)(LPCVOID,LPCSTR,LPVOID*,PUINT)>("VerQueryValueA");return fn?fn(a,b,c,d):FALSE;}
extern "C" __declspec(dllexport) BOOL WINAPI VerQueryValueW(LPCVOID a,LPCWSTR b,LPVOID* c,PUINT d){
    auto fn=RealProc<BOOL(WINAPI*)(LPCVOID,LPCWSTR,LPVOID*,PUINT)>("VerQueryValueW");return fn?fn(a,b,c,d):FALSE;}

BOOL WINAPI DllMain(HINSTANCE i, DWORD r, LPVOID) {
    if (r == DLL_PROCESS_ATTACH) {
        g_self = i;
        DisableThreadLibraryCalls(i);
        HANDLE t = CreateThread(nullptr, 0, ProbeThread, nullptr, 0, nullptr);
        if (t) CloseHandle(t);
    }
    return TRUE;
}
