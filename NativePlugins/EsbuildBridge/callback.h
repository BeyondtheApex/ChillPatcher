#ifndef CALLBACK_H
#define CALLBACK_H

typedef void (*progress_callback)(const char* pkgPath, const char* status, const char* msg);

void callProgressCallback(progress_callback cb, const char* pkgPath, const char* status, const char* msg);

#endif
