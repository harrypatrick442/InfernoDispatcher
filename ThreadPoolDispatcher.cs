namespace InfernoDispatcher
{
    public sealed class ThreadPoolDispatcher:DispatcherBase
    {

        public ThreadPoolDispatcher(
            Action<Exception>? handleUncaughtException = null, 
            int? maxWorkerThreads = null,
            int? maxIOThreads = null):base(handleUncaughtException)
        {
            _HandleUncaughtException = handleUncaughtException;
            if (maxWorkerThreads != null || maxIOThreads != null)
            {
                ThreadPool.GetMaxThreads(out int _maxWorkerThreads, out int _maxIOThreads);
                ThreadPool.SetMaxThreads(maxWorkerThreads ?? _maxWorkerThreads, maxIOThreads ?? _maxIOThreads);
            }
        }
        protected override void Run(InfernoTask task, object[]? arguments)
        {
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
