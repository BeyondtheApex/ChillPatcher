using System;

namespace OneJS {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class EventfulPropertyAttribute : Attribute {
        public bool CheckEquality { get; }

        /// <summary>
        /// Generate additional property and event that are compatible with useEventfulState
        /// </summary>
        /// <param name="checkEquality">Add an equality check before setting the value and firing the event</param>
        public EventfulPropertyAttribute(bool checkEquality = false) {
            CheckEquality = checkEquality;
        }
    }
}
