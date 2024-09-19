using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using InfernoDispatcher.Promises;
using InfernoDispatcher.Core;
namespace InfernoDispatcher.Tasks
{
    public abstract class InfernoTask : INotifyCompletion
    {
        protected bool _Cancelled;
        public bool Cancelled
        {
            get
            {
                lock (_LockObject)
                {
                    return _Cancelled;
                }
            }
        }
        internal object[]? _Result;
        internal object[]? Arguments { get; set; }
        protected readonly object _LockObject = new object();
        protected List<InfernoTask>? _Thens;
        protected List<InfernoTaskNoResult>? _Catchs;
        protected bool _IsCompleted = false;
        public bool IsCompleted
        {
            get
            {
                lock (_LockObject)
                {
                    return _IsCompleted;
                }
            }
        }
        protected Exception? _Exception;
        internal CountdownLatch? _CountdownLatchWait;
        private InfernoTask[] _Froms;
        internal InfernoTask(params InfernoTask[] froms)
        {
            _Froms = froms;
        }
        internal void AddFrom(InfernoTask from)
        {
            bool cancelled;
            lock (_LockObject)
            {
                if (!_IsCompleted)
                {
                    var oldFroms = _Froms;
                    int length = oldFroms.Length;
                    _Froms = new InfernoTask[length + 1];
                    Array.Copy(oldFroms, _Froms, length);
                    _Froms[length] = from;
                    return;
                }
                cancelled = _Cancelled;
            }
            if (cancelled)
            {
                from.Cancel(this);
                return;
            }
        }
        public abstract void Run(object[]? arguments);
        public void Cancel()
        {
            Cancel(null);
        }
        protected void Cancel(InfernoTask? doingCancelIfNotNullConsiderOtherDependencies)
        {
            List<InfernoTask>? thens;
            InfernoTask[] froms;
            lock (_LockObject)
            {
                if (_IsCompleted) return;
                if (doingCancelIfNotNullConsiderOtherDependencies != null)
                {
                    bool hasOtherDependenciesNotCancelled =
                        _Thens == null/*just incase somehow*/? false : _Thens
                        .Where(t => !t.Equals(doingCancelIfNotNullConsiderOtherDependencies) && !t.Cancelled)
                        .Any();
                    if (hasOtherDependenciesNotCancelled)
                    {
                        return;
                    }
                }
                _IsCompleted = true;
                _Cancelled = true;
                thens = _Thens;
                _Thens = null;
                froms = _Froms;
                _Froms = null;
                _CountdownLatchWait?.Signal();
            }
            foreach (InfernoTask from in froms)
            {
                from.Cancel(this);
            }
            if (thens != null)
            {
                foreach (InfernoTask then in thens)
                {
                    then.Cancel();
                }
            }
        }
        internal TTask ExecuteOrScheduleTask<TTask>(TTask task, bool noRunCosAlreadyRun = false) where TTask : InfernoTask
        {
            Exception? exception;
            bool cancelled;
            object[]? runArguments;
            lock (_LockObject)
            {
                if (!_IsCompleted)
                {
                    if (_Thens == null)
                    {
                        _Thens = new List<InfernoTask>(1) { task };
                    }
                    else
                    {
                        _Thens.Add(task);
                    }
                    return task;
                }
                runArguments = _Result;
                exception = _Exception;
                cancelled = _Cancelled;
            }
            if (cancelled)
            {
                task.Cancel();
                return task;
            }
            if (exception != null)
            {
                task.Fail(exception);
                return task;
            }
            if (!noRunCosAlreadyRun)
            {
                Dispatcher.Instance.Run(task, runArguments);
            }
            return task;
        }
        internal void Success(object[]? result)
        {
            List<InfernoTask>? thens;
            List<InfernoTaskNoResult>? catchs;
            lock (_LockObject)
            {
                if (_IsCompleted) return;
                _IsCompleted = true;
                _Result = result;
                thens = _Thens;
                _Thens = null;
                catchs = _Catchs;
                _Catchs = null;
                _CountdownLatchWait?.Signal();
            }
            if (thens != null)
            {
                foreach (InfernoTask then in thens)
                {
                    Dispatcher.Instance.Run(then, result);
                }
            }
            if (catchs != null)
            {
                foreach (InfernoTaskNoResult catcher in catchs)
                {
                    catcher.CompleteCatcherWithoutException(result);
                }
            }
        }
        internal void Fail(Exception ex)
        {
            List<InfernoTask>? thens;
            List<InfernoTaskNoResult>? catches;
            lock (_LockObject)
            {
                if (_IsCompleted) return;
                _Exception = ex;
                _IsCompleted = true;
                thens = _Thens;
                _Thens = null;
                catches = _Catchs;
                _Catchs = null;
                _CountdownLatchWait?.Signal();
            }
            if (thens == null) return;
            foreach (InfernoTask then in thens)
            {
                then.Fail(ex);
            }
            if (catches != null)
            {
                foreach (var catcher in catches)
                {
                    catcher.Run(new object[] { ex });
                }
            }
        }
        internal void CheckNotAlreadyCompleted()
        {
            lock (_LockObject)
            {
                if (_IsCompleted) throw new InvalidOperationException($"Task already completed and {nameof(Run)} should not be getting called again");

            }
        }
        internal void ThrowException()
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(_Exception!).Throw();
        }
        #region Then
        public InfernoTaskVoidPromiseNoArguments Then(PromiseVoid promise)
        {
            // Create a new InfernoTaskNoResult that runs the callback after this task completes
            InfernoTaskVoidPromiseNoArguments task = new InfernoTaskVoidPromiseNoArguments(
                promise, this);
            ExecuteOrScheduleTask(task);
            return task;
        }
        public InfernoTaskPromiseNoArgument<TNextResult> Then<TNextResult>(
            Promise<TNextResult> promise)
        {
            return ExecuteOrScheduleTask(new InfernoTaskPromiseNoArgument<TNextResult>(
                promise, this));
        }
        public InfernoTaskNoResult Then(Action callback)
        {
            // Create a new InfernoTaskNoResult that runs the callback after this task completes
            InfernoTaskNoResult task = new InfernoTaskNoResult(callback, this);
            ExecuteOrScheduleTask(task);
            return task;
        }

        public InfernoTaskNoResult Then(InfernoTaskNoResult task)
        {
            // Chain the existing task after this one
            task.AddFrom(this);
            ExecuteOrScheduleTask(task);
            return task;
        }

        public InfernoTaskWithResult<TNextResult> Then<TNextResult>(Func<TNextResult> callback)
        {
            // Create a new InfernoTaskWithResult to chain a result-producing task
            InfernoTaskWithResult<TNextResult> task = new InfernoTaskWithResult<TNextResult>(callback, this);
            ExecuteOrScheduleTask(task);
            return task;
        }
        public InfernoTaskVoidPromiseReturnNoArgument Then(
            Func<PromiseVoid> promise)
        {
            return ExecuteOrScheduleTask(new InfernoTaskVoidPromiseReturnNoArgument(
                promise, this));
        }

        public InfernoTaskWithResult<TNextResult> Then<TNextResult>(InfernoTaskWithResult<TNextResult> task)
        {
            // Chain the existing result-producing task after this one
            task.AddFrom(this);
            ExecuteOrScheduleTask(task, noRunCosAlreadyRun: true);
            return task;
        }
        public InfernoTaskPromiseReturnNoArgument<TNextResult> Then<TNextResult>(
            Func<Promise<TNextResult>> promise)
        {
            return ExecuteOrScheduleTask(new InfernoTaskPromiseReturnNoArgument<TNextResult>(
                promise, this));
        }
        #endregion
        internal InfernoTaskWithResultBase<TNextResult> ThenExistingTask<TNextResult>(
            InfernoTaskWithResultArgument<TNextResult, TNextResult> task)
        {
            // Chain the existing result-producing task after this one
            task.AddFrom(this);
            ExecuteOrScheduleTask(task, noRunCosAlreadyRun: true);
            return task;
        }
        public InfernoTaskNoResult ThenCreateTask(Func<InfernoTaskNoResult> callback)
        {
            InfernoTaskNoResult? toReturn = null;
            InfernoTaskNoResult task = new InfernoTaskNoResult(
                () =>
                {
                    try
                    {
                        var nextTask = callback();
                        nextTask.Then(toReturn!);
                    }
                    catch (Exception ex)
                    {
                        toReturn!.Fail(ex);
                    }
                }, this
            );
            toReturn = new InfernoTaskNoResult(() => { }, task);
            ExecuteOrScheduleTask(task);
            return toReturn;
        }

        public InfernoTaskWithResult<TNextResult> ThenCreateTask<TNextResult>(
            Func<InfernoTaskWithResult<TNextResult>> callback)
        {
            // Chain a new result-producing task that is returned by the callback function
            TNextResult? result = default;
            InfernoTaskWithResult<TNextResult>? toReturn = null;
            InfernoTaskNoResult task = new InfernoTaskNoResult(
                () =>
                {
                    try
                    {
                        var childTask = callback();
                        childTask.Then(toReturn!);
                    }
                    catch (Exception ex)
                    {
                        toReturn!.Fail(ex);
                    }
                }, this
            );
            toReturn = new InfernoTaskWithResult<TNextResult>(() => result!, task);
            ExecuteOrScheduleTask(task);
            return toReturn;
        }
        #region Join
        public InfernoTaskNoResult Join(InfernoTaskNoResult other, Action callback)
        {
            return Join(callback, other);
        }

        public InfernoTaskNoResult Join(
            InfernoTaskNoResult other1,
            InfernoTaskNoResult other2,
            Action callback)
        {
            return Join(callback, other1, other2);
        }

        public InfernoTaskNoResult Join(
            InfernoTaskNoResult other1,
            InfernoTaskNoResult other2,
            InfernoTaskNoResult other3,
            Action callback)
        {
            return Join(callback, other1, other2, other3);
        }
        public InfernoTaskNoResult Join(
                    Action callback,
                    params InfernoTaskNoResult[] others)
        {
            // Lock object for thread-safe operation
            object lockObject = new object();
            // Track how many tasks have completed
            int doneCount = 0;
            // Total number of tasks to wait for (including 'this')
            int totalTasks = others.Length + 1;

            // Create the new task that will run the callback after all others are done
            InfernoTaskNoResult taskToReturn = new InfernoTaskNoResult(callback, this);

            // Method to check if all tasks are done
            Action checkIfDoneAndRunIfIs = () =>
            {
                lock (lockObject)
                {
                    doneCount++;
                    // If all tasks are done, run the final callback
                    if (doneCount == totalTasks)
                    {
                        Dispatcher.Instance.Run(taskToReturn, null);
                    }
                }
            };

            // Register completion handlers for all the other tasks
            foreach (var other in others)
            {
                other.Then(checkIfDoneAndRunIfIs);
            }

            // Also register the completion handler for 'this' task
            Then(checkIfDoneAndRunIfIs);

            return taskToReturn;
        }
        #endregion
        #region Catch
        public InfernoTaskNoResult Catch(Action<Exception> callback)
        {
            InfernoTaskNoResult catcher = new InfernoTaskNoResult(() =>
            {
                Exception ex;
                lock (_LockObject)
                {
                    ex = _Exception!;
                }
                callback(ex);
            }, this);
            Exception? exception;
            bool cancelled;
            object[]? resultAsRunArguments;
            lock (_LockObject)
            {
                if (!_IsCompleted)
                {
                    _Catchs!.Add(catcher);
                    return catcher;
                }
                exception = _Exception;
                cancelled = _Cancelled;
                resultAsRunArguments = _Result;
            }
            if (cancelled)
            {
                catcher.Cancel();
                return catcher;
            }
            if (exception == null)
            {
                catcher.CompleteCatcherWithoutException(resultAsRunArguments);
                return catcher;
            }
            Dispatcher.Instance.Run(catcher, new object[] { exception });
            return catcher;
        }
        #endregion
        internal void CompleteCatcherWithoutException(object[]? arguments)
        {
            List<InfernoTask>? thens;
            lock (_LockObject)
            {
                _IsCompleted = true;
                thens = _Thens;
                _Thens = null;
                _CountdownLatchWait?.Signal();
            }
            if (thens != null)
            {
                foreach (InfernoTask then in thens)
                {
                    Dispatcher.Instance.Run(then, arguments);
                }
            }
        }
        #region Delay
        public InfernoTaskVoidPromiseNoArguments Delay(int millisecondsDelay, PromiseVoid promise)
        {
            return Then(new PromiseVoid((resolve, reject) =>
            {
                Task.Delay(millisecondsDelay).ContinueWith((ignore) => resolve());
            })).Then(promise);
        }
        public InfernoTaskPromiseNoArgument<TNextResult> Delay<TNextResult>(
            int millisecondsDelay,
            Promise<TNextResult> promise)
        {
            return Then(new PromiseVoid((resolve, reject) =>
            {
                Task.Delay(millisecondsDelay).ContinueWith((ignore) => resolve());
            })).Then(promise);
        }
        public InfernoTaskNoResult Delay(int millisecondsDelay, Action callback)
        {
            return Then(new PromiseVoid((resolve, reject) =>
            {
                Task.Delay(millisecondsDelay).ContinueWith((ignore) => resolve());
            })).Then(callback);
        }

        public InfernoTaskNoResult Delay(int millisecondsDelay, InfernoTaskNoResult task)
        {
            return Then(new PromiseVoid((resolve, reject) =>
            {
                Task.Delay(millisecondsDelay).ContinueWith((ignore) => resolve());
            })).Then(task);
        }

        public InfernoTaskWithResult<TNextResult> Delay<TNextResult>(int millisecondsDelay, Func<TNextResult> callback)
        {
            return Then(new PromiseVoid((resolve, reject) =>
            {
                Task.Delay(millisecondsDelay).ContinueWith((ignore) => resolve());
            })).Then(callback);
        }

        public InfernoTaskWithResult<TNextResult> Delay<TNextResult>(int millisecondsDelay, InfernoTaskWithResult<TNextResult> task)
        {
            return Then(new PromiseVoid((resolve, reject) =>
            {
                Task.Delay(millisecondsDelay).ContinueWith((ignore) => resolve());
            })).Then(task);
        }
        public InfernoTaskPromiseReturnNoArgument<TNextResult> Delay<TNextResult>(
            int millisecondsDelay,
            Func<Promise<TNextResult>> promise)
        {
            return Then(new PromiseVoid((resolve, reject) =>
            {
                Task.Delay(millisecondsDelay).ContinueWith((ignore) => resolve());
            })).Then(promise);
        }
        public InfernoTaskVoidPromiseReturnNoArgument Delay<TNextResult>(
            int millisecondsDelay,
            Func<PromiseVoid> func)
        {
            return Then(new PromiseVoid((resolve, reject) =>
            {
                Task.Delay(millisecondsDelay).ContinueWith((ignore) => resolve());
            })).Then(func);
        }
        #endregion
        #region Awaitable methods
        public void OnCompleted(Action continuation)
        {
            Then(continuation);
            Catch((ex) => continuation());
        }
        #endregion
    }
}