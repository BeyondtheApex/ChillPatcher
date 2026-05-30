using System.IO;
using System.Text.RegularExpressions;

namespace OneJS.Utils {
    public class StringUtil {
        public static string SanitizeFilename(string filename) {
            // Remove invalid characters
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            filename = Regex.Replace(filename, invalidRegStr, "_");

            // Ensure the filename isn't empty and doesn't consist only of periods or spaces
            filename = filename.Trim('.', ' ');
            if (string.IsNullOrWhiteSpace(filename)) {
                filename = "_";
            }

            // Truncate filename if it's too long (Windows has a 260 character path limit)
            int maxFilenameLengthOnWindows = 255; // To be safe, we'll use 255 instead of 260
            if (filename.Length > maxFilenameLengthOnWindows) {
                filename = filename.Substring(0, maxFilenameLengthOnWindows);
            }

            return filename;
        }
    }
}