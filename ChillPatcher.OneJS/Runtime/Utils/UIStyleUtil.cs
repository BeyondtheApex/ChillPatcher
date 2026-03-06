using UnityEngine.UIElements;

namespace OneJS.Utils {
    public class UIStyleUtil {
        /**
         * This is a workaround for passing float vs enum from javascript
         * because the other constructor of StyleFloat accepts an enum.
         *
         * So when you do `new StyleFloat(1.0)` from JS, it'll actually
         * use the float version of the ctor instead of the enum one.
         */
        public static StyleFloat GetStyleFloat(float n) {
            return new StyleFloat(n);
        }
        
        /**
         * Same with int
         */
        public static StyleInt GetStyleInt(int n) {
            return new StyleInt(n);
        }
    }
}