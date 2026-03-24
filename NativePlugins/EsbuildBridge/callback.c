#include "callback.h"

void callProgressCallback(progress_callback cb, const char* pkgPath, const char* status, const char* msg) {
    cb(pkgPath, status, msg);
}
