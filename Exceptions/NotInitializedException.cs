using System.Diagnostics;
using System.Runtime.Serialization;

namespace InfernoDispatcher.Exceptions
{
    [DataContract]
    public class NotInitializedException:InvalidOperationException
    {
        protected NotInitializedException() : base() { }
        public NotInitializedException(string message) : base(message) { }
        public NotInitializedException(Type type) : base(GetMessage(type)){ }
        private static string GetMessage(Type type) {
            return $"{type} was not initialized";
        }
        private static Type GetCallingType() {
            return new StackTrace().GetFrame(2).GetMethod().DeclaringType;
        }
        public static NotInitializedException ForThis() { return new NotInitializedException(GetMessage(GetCallingType())); }
    }
}
