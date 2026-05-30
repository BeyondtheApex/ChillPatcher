using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ChillPatcher.Patches.Rime
{
    /// <summary>
    /// Windows VK_CODE 到 Rime Keycode 转换
    /// 参考 weasel/WeaselTSF/KeyEvent.cpp
    /// </summary>
    public static class KeyEventConverter
    {
        [DllImport("user32.dll")]
        private static extern int ToUnicodeEx(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags,
            IntPtr dwhkl);

        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        // ibus::Keycode 定义(来自 weasel/include/KeyEvent.h)
        private const int BackSpace = 0xFF08;
        private const int Tab = 0xFF09;
        private const int Return = 0xFF0D;
        private const int Escape = 0xFF1B;
        private const int Delete = 0xFFFF;
        private const int Home = 0xFF50;
        private const int Left = 0xFF51;
        private const int Up = 0xFF52;
        private const int Right = 0xFF53;
        private const int Down = 0xFF54;
        private const int Prior = 0xFF55;  // Page Up
        private const int Next = 0xFF56;   // Page Down
        private const int End = 0xFF57;
        private const int Space = 0x0020;
        private const int KP_Enter = 0xFF8D;

        /// <summary>
        /// 转换 Windows VK_CODE 为 Rime keycode
        /// </summary>
        public static bool ConvertKeyEvent(uint vkCode, uint scanCode, out int keycode, out int mask)
        {
            keycode = 0;
            mask = 0; // ibus::NULL_MASK

            // 获取键盘状态
            byte[] keyState = new byte[256];
            if (!GetKeyboardState(keyState))
            {
                return false;
            }

            // 设置 mask (修饰键状态)
            const byte KEY_DOWN = 0x80;
            const byte TOGGLED = 0x01;

            if ((keyState[0x10] & KEY_DOWN) != 0) // VK_SHIFT
                mask |= 1 << 0; // SHIFT_MASK

            if ((keyState[0x14] & TOGGLED) != 0) // VK_CAPITAL
                mask |= 1 << 1; // LOCK_MASK

            if ((keyState[0x11] & KEY_DOWN) != 0) // VK_CONTROL
                mask |= 1 << 2; // CONTROL_MASK

            if ((keyState[0x12] & KEY_DOWN) != 0) // VK_MENU (Alt)
                mask |= 1 << 3; // ALT_MASK

            // 先尝试特殊键转换
            int specialKey = TranslateKeycode(vkCode);
            if (specialKey != 0)
            {
                keycode = specialKey;
                return true;
            }

            // 普通字符键:使用 ToUnicodeEx 转换
            StringBuilder buffer = new StringBuilder(8);
            byte[] tempKeyState = new byte[256];
            Array.Copy(keyState, tempKeyState, 256);

            // 清除 Ctrl 和 Alt 状态以获取字符
            tempKeyState[0x11] = 0; // VK_CONTROL
            tempKeyState[0x12] = 0; // VK_MENU

            IntPtr hkl = GetKeyboardLayout(0);
            int result = ToUnicodeEx(vkCode, scanCode, tempKeyState, buffer, buffer.Capacity, 0, hkl);

            if (result == 1)
            {
                // 成功转换为单个字符
                keycode = buffer[0];
                return true;
            }

            // 无法转换
            return false;
        }

        /// <summary>
        /// 转换特殊键 VK_CODE 为 ibus::Keycode
        /// </summary>
        private static int TranslateKeycode(uint vkCode)
        {
            switch (vkCode)
            {
                case 0x08: return BackSpace; // VK_BACK
                case 0x09: return Tab;       // VK_TAB
                case 0x0D: return Return;    // VK_RETURN
                case 0x1B: return Escape;    // VK_ESCAPE
                case 0x20: return Space;     // VK_SPACE
                case 0x21: return Prior;     // VK_PRIOR (Page Up)
                case 0x22: return Next;      // VK_NEXT (Page Down)
                case 0x23: return End;       // VK_END
                case 0x24: return Home;      // VK_HOME
                case 0x25: return Left;      // VK_LEFT
                case 0x26: return Up;        // VK_UP
                case 0x27: return Right;     // VK_RIGHT
                case 0x28: return Down;      // VK_DOWN
                case 0x2E: return Delete;    // VK_DELETE
                
                // 功能键 F1-F12
                case 0x70: return 0xFFBE; // F1
                case 0x71: return 0xFFBF; // F2
                case 0x72: return 0xFFC0; // F3
                case 0x73: return 0xFFC1; // F4
                case 0x74: return 0xFFC2; // F5
                case 0x75: return 0xFFC3; // F6
                case 0x76: return 0xFFC4; // F7
                case 0x77: return 0xFFC5; // F8
                case 0x78: return 0xFFC6; // F9
                case 0x79: return 0xFFC7; // F10
                case 0x7A: return 0xFFC8; // F11
                case 0x7B: return 0xFFC9; // F12

                default: return 0; // 不是特殊键
            }
        }
    }
}
