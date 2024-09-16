using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace InfernoDispatcher.Exceptions
{
    [DataContract]
    public class AlreadyInitializedException : InvalidOperationException
    {
        protected AlreadyInitializedException() : base() { }
        public AlreadyInitializedException(string message) : base(message) { }
        public AlreadyInitializedException(Type type) : base(GetMessage(type)) { }
        private static string GetMessage(Type type)
        {
            return $"{type} was already initialized";
        }
        private static Type GetCallingType()
        {
            return new StackTrace().GetFrame(2).GetMethod().DeclaringType;
        }
        public static AlreadyInitializedException ForThis() { return new AlreadyInitializedException(GetMessage(GetCallingType())); }
    }
}
