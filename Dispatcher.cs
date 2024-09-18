﻿using InfernoDispatcher.Exceptions;

namespace InfernoDispatcher
{
    public sealed class Dispatcher
    {
        private static Dispatcher? _Instance;
        public static Dispatcher Initialize(Action<Exception>? handleUncaughtException, int? nDegreesParallelism = null) {
            if (_Instance != null) throw new AlreadyInitializedException(nameof(Dispatcher));
            _Instance = new Dispatcher(nDegreesParallelism?? Environment.ProcessorCount, handleUncaughtException);
            return _Instance;
        }
        public static Dispatcher Instance { 
            get {
                if (_Instance == null) throw new NotInitializedException((nameof(Dispatcher)));
                return _Instance; 
            }
        }
        private int _NDegreesParallelism;
        private int? _MaxTaskQueueSize;
        private readonly object _LockObject = new object();
        private readonly HashSet<Thread> _Threads = new HashSet<Thread>();
        private readonly LinkedList<ThreadSafeTaskWrapper> _TasksWaitingToBeRun = new LinkedList<ThreadSafeTaskWrapper>();
        private Action<Exception>? _HandleUncaughtException;
        private Dispatcher(int nDegreesParallelism, Action<Exception>? handleUncaughtException)
        {
            _HandleUncaughtException = handleUncaughtException;
            _NDegreesParallelism = nDegreesParallelism;
            if (nDegreesParallelism <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nDegreesParallelism), "Degrees of parallelism must be greater than zero.");
            }
        }
        public ThreadSafeTaskWrapperNoResult Run(Action callback)
        {
                ThreadSafeTaskWrapperNoResult task = new ThreadSafeTaskWrapperNoResult(callback);
                Run(task, null);
                return task;
        }
        public ThreadSafeTaskWrapperWithResult<T> Run<T>(Func<T> callback)
        {
            ThreadSafeTaskWrapperWithResult<T> task = new ThreadSafeTaskWrapperWithResult<T>(callback);
            Run(task, null);
            return task;
        }
        public void Run(ThreadSafeTaskWrapper task, object[]? arguments)
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
        private void SpinUpNewThread(ThreadSafeTaskWrapper? task) {
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
        private ThreadSafeTaskWrapper? TakeTaskNoLock() {
            var node = _TasksWaitingToBeRun.First;
            if (node == null) return null;
            _TasksWaitingToBeRun.RemoveFirst();
            return node.Value;
        }
    }
}