using InfernoDispatcher.Tasks;
using System.Diagnostics;

namespace InfernoDispatcher.Dispatchers
{
    public sealed class ThreadPoolDispatcher : DispatcherBase
    {

        public ThreadPoolDispatcher(
            Action<Exception>? handleUncaughtException = null,
            int? maxWorkerThreads = null,
            int? maxIOThreads = null) : base(handleUncaughtException)
        {
            _HandleUncaughtException = handleUncaughtException;
            if (maxWorkerThreads != null || maxIOThreads != null)
            {
                ThreadPool.GetMaxThreads(out int _maxWorkerThreads, out int _maxIOThreads);
                ThreadPool.SetMaxThreads(maxWorkerThreads ?? _maxWorkerThreads, maxIOThreads ?? _maxIOThreads);
            }
        }
        public override void Run(InfernoTask task, object[]? arguments)
        {
            if (task.GetType().Name.Contains("Four")) { 
                
            }
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    task.Run(arguments);
                }
                catch (Exception ex)
                {
                    _HandleUncaughtException?.Invoke(ex);
                }
            });
        }
    }
}
