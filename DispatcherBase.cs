﻿namespace InfernoDispatcher
{
    public abstract class DispatcherBase
    {
        protected Action<Exception>? _HandleUncaughtException;
        protected DispatcherBase(Action<Exception>? handleUncaughtException)
        {
            _HandleUncaughtException = handleUncaughtException;
        }
        public InfernoTaskNoResult Run(Action callback)
        {
                InfernoTaskNoResult task = new InfernoTaskNoResult(callback);
                Run(task, null);
                return task;
        }
        public InfernoTaskWithResult<T> Run<T>(Func<T> callback)
        {
            InfernoTaskWithResult<T> task = new InfernoTaskWithResult<T>(callback);
            Run(task, null);
            return task;
        }
        protected abstract void Run(InfernoTask task, object[]? arguments);
    }
}