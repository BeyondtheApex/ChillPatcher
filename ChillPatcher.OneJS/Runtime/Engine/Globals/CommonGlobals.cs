using System;

namespace OneJS {
    public class CommonGlobals {
        /*
         * There's an extremely subtle issue here if we need to decode a string that contains a null terminator in the
         * middle of the string. Default JS behavior is that the full string will be returned. But when interop is
         * involved, the null terminator will be treated as the end of the string, so only a partial string is returned.
         *
         * This is especially problematic when we use 3rdparty libs that do WebAssembly.instantiate() using
         * Uint8Array.from(atob(data)) because it'll expect magic word 00 61 73 6d. So far the only workaround is to
         * completely implement atob() and btoa() in JS.
         */
        
        public static string atob(string str) {
            return System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(str));
        }

        public static string btoa(string str) {
            return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(str));
        }

        static string ConvertArrayToString(int[] asciiArray) {
            char[] charArray = new char[asciiArray.Length];
            for (int i = 0; i < asciiArray.Length; i++) {
                charArray[i] = (char)asciiArray[i];
            }
            return new string(charArray);
        }
    }
}