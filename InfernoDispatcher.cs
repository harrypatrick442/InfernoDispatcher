using InfernoDispatcher.Exceptions;

namespace InfernoDispatcher
{
    public sealed class InfernoDispatcher : DispatcherBase
    {
        private int _NDegreesParallelism;
        private int? _MaxTaskQueueSize;
        private readonly object _LockObject = new object();
        private readonly HashSet<Thread> _Threads = new HashSet<Thread>();
        private readonly LinkedList<InfernoTask> _TasksWaitingToBeRun = new LinkedList<InfernoTask>();
        internal InfernoDispatcher(
            int nDegreesParallelism, Action<Exception>? handleUncaughtException
            ):base(handleUncaughtException)
        {
            _NDegreesParallelism = nDegreesParallelism;
            if (nDegreesParallelism <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nDegreesParallelism), "Degrees of parallelism must be greater than zero.");
            }
        }
        protected override void Run(InfernoTask task, object[]? arguments)
        {
            lock (_LockObject)
            {
                task.Arguments = arguments;
                if (_Threads.Count >= _NDegreesParallelism)
                {
                    if (_MaxTaskQueueSize!=null&&(_TasksWaitingToBeRun.Count > (int)_MaxTaskQueueSize)) {
                        throw new Exception($"Maximum task queue size of {_MaxTaskQueueSize} exceeded");
                    }
                    _TasksWaitingToBeRun.AddLast(task);
                    return;
                }
                SpinUpNewThread(task);
            }
        }
        private void SpinUpNewThread(InfernoTask? task) {
            if (task == null) return;
            Thread? thread = null;
            object[]? arguments = task.Arguments;
            thread = new Thread(() => {
                if (!task.Cancelled)
                {
                    try
                    {
                        task.Run(arguments);
                    }
                    catch (Exception ex)
                    {
                        //$"Should not be handling any errors here, should be handled within {nameof(task)}.{nameof(task.Run)}: {ex.StackTrace}"
                        _HandleUncaughtException?.Invoke(ex);
                    }
                }
                lock (_LockObject)
                {
                    task = TakeTaskNoLock();
                    if (task == null)
                    {
                        _Threads.Remove(thread!);
                        return;
                    }
                    arguments = task.Arguments;
                }
            });
            _Threads.Add(thread);
            thread.Start();
        }
        private InfernoTask? TakeTaskNoLock() {
            var node = _TasksWaitingToBeRun.First;
            if (node == null) return null;
            _TasksWaitingToBeRun.RemoveFirst();
            return node.Value;
        }
    }
}