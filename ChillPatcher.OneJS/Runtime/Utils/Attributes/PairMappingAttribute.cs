using UnityEngine;

namespace OneJS {
    public class PairMappingAttribute : PropertyAttribute {
        public readonly string from;
        public readonly string to;
        public readonly string separator;
        public readonly string label;

        public PairMappingAttribute(string from, string to, string separator = ">", string label = null) {
            this.from = from;
            this.to = to;
            this.separator = separator;
            this.label = label;
        }
    }
}