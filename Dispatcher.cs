using InfernoDispatcher.Exceptions;

namespace InfernoDispatcher
{
    public sealed class Dispatcher
    {
        private static DispatcherBase? _Instance;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="handleUncaughtException"></param>
        /// <param name="infernoElseNativeThreadPool"></param>
        /// <param name="nDegreesParallelism"></param>
        /// <param name="maxIOThreads">Only applies if using native</param>
        /// <returns></returns>
        /// <exception cref="AlreadyInitializedException"></exception>
        public static DispatcherBase Initialize(
            Action<Exception>? handleUncaughtException,
            bool infernoElseNativeThreadPool = true,
            int ? nDegreesParallelism = null,
            int? maxIOThreads= null) {
            if (_Instance != null) throw new AlreadyInitializedException(nameof(Dispatcher));
            _Instance = infernoElseNativeThreadPool
                ?new InfernoDispatcher(
                    nDegreesParallelism?? Environment.ProcessorCount,
                    handleUncaughtException
                )
                :new ThreadPoolDispatcher
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