using InfernoDispatcher.Exceptions;

namespace InfernoDispatcher
{
    public sealed class Dispatcher
    {
        private static DispatcherBase? _Instance;
        public static DispatcherBase InitializeWithInferno(
            Action<Exception>? handleUncaughtException,
                int ? nDegreesParallelism = null,
                int? maxTaskQueueSize = null
            ) {
            if (_Instance != null) throw new AlreadyInitializedException(nameof(Dispatcher));
            _Instance = new InfernoDispatcher(
                    nDegreesParallelism ?? Environment.ProcessorCount,
                    maxTaskQueueSize,
                    handleUncaughtException
                );
            return _Instance;
        }
        public static DispatcherBase InitializeWithNative(
            Action<Exception>? handleUncaughtException,
            int? nDegreesParallelism = null,
            int? maxIOThreads = null)
        {
            if (_Instance != null) throw new AlreadyInitializedException(nameof(Dispatcher));
            _Instance = new ThreadPoolDispatcher
                (
                    handleUncaughtException,
                    nDegreesParallelism,
                    maxIOThreads
                );
            return _Instance;
        }
        public static DispatcherBase Instance { 
            get {
                if (_Instance == null) throw new NotInitializedException((nameof(Dispatcher)));
                return _Instance; 
            }
        }
    }
}