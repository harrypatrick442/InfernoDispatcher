using System.Threading.Tasks;

namespace InfernoDispatcher
{
    public abstract class ThreadSafeTaskWrapper
    {
        protected bool _Cancelled;
        public bool Cancelled { 
            get {
                lock (_LockObject)
                {
                    return _Cancelled;
                }
            } 
        }
        protected readonly object _LockObject = new object();
        protected List<ThreadSafeTaskWrapper>? _Thens;
        protected List<ThreadSafeTaskWrapperNoResult>? _Catchs;
        protected bool _IsCompleted = false;
        protected Exception? _Exception;
        internal CountdownLatch? _CountdownLatchWait;
        private ThreadSafeTaskWrapper[] _Froms;
        internal ThreadSafeTaskWrapper(params ThreadSafeTaskWrapper[] froms)
        {
            _Froms = froms;
        }
        internal void AddFrom(ThreadSafeTaskWrapper from)
        {
            bool cancelled;
            lock (_LockObject) {
                if (!_IsCompleted) {
                    var oldFroms = _Froms;
                    int length = oldFroms.Length;
                    _Froms = new ThreadSafeTaskWrapper[length + 1];
                    Array.Copy(oldFroms, _Froms, length);
                    _Froms[length] = from;
                    return;
                 }
                cancelled = _Cancelled;
             }
            if (cancelled) {
                from.Cancel(this);
                return;
            }
        }
        public abstract void Run(object[]? arguments);
        public void Cancel() {
            Cancel(null);
        }
        protected void Cancel(ThreadSafeTaskWrapper? doingCancelIfNotNullConsiderOtherDependencies)
        {
            List<ThreadSafeTaskWrapper>? thens;
            ThreadSafeTaskWrapper[] froms;
            lock (_LockObject)
            {
                if (_IsCompleted) return;
                if (doingCancelIfNotNullConsiderOtherDependencies != null) {
                    bool hasOtherDependenciesNotCancelled =
                        _Thens==null/*just incase somehow*/?false:_Thens
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
            foreach (ThreadSafeTaskWrapper from in froms)
            {
                from.Cancel(this);
            }
            if (thens != null)
            {
                foreach (ThreadSafeTaskWrapper then in thens)
                {
                    then.Cancel();
                }
            }
        }
        protected abstract object[]? ResultAsRunArguments();
        protected void ExecuteOrScheduleTask(ThreadSafeTaskWrapper task)
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
                        _Thens = new List<ThreadSafeTaskWrapper>(1) { task };
                    }
                    else
                    {
                        _Thens.Add(task);
                    }
                    return;
                }
                runArguments = ResultAsRunArguments();
                exception = _Exception;
                cancelled = _Cancelled;
            }
            if (cancelled)
            {
                task.Cancel();
                return;
            }
            if (exception != null)
            {
                task.Fail(exception);
                return;
            }
            Dispatcher.Instance.Run(task, runArguments);
        }
        internal void Fail(Exception ex)
        {
            List<ThreadSafeTaskWrapper>? thens;
            List<ThreadSafeTaskWrapperNoResult>? catches;
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
            foreach (ThreadSafeTaskWrapper then in thens)
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
        protected void CheckNotAlreadyCompleted()
        {
            lock (_LockObject)
            {
                if (_IsCompleted) throw new InvalidOperationException($"Task already completed and {nameof(Run)} should not be getting called again");

            }
        }
        protected void ThrowException()
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(_Exception!).Throw();
        }
        public ThreadSafeTaskWrapperVoidPromiseNoArguments Then(VoidPromise promise)
        {
            // Create a new ThreadSafeTaskWrapperNoResult that runs the callback after this task completes
            ThreadSafeTaskWrapperVoidPromiseNoArguments task = new ThreadSafeTaskWrapperVoidPromiseNoArguments(
                promise, this);
            ExecuteOrScheduleTask(task);
            return task;
        }
        public ThreadSafeTaskWrapperNoResult Then(Action callback)
        {
            // Create a new ThreadSafeTaskWrapperNoResult that runs the callback after this task completes
            ThreadSafeTaskWrapperNoResult task = new ThreadSafeTaskWrapperNoResult(callback, this);
            ExecuteOrScheduleTask(task);
            return task;
        }

        public ThreadSafeTaskWrapperNoResult Then(ThreadSafeTaskWrapperNoResult task)
        {
            // Chain the existing task after this one
            task.AddFrom(this);
            ExecuteOrScheduleTask(task);
            return task;
        }

        public ThreadSafeTaskWrapperWithResult<TNextResult> Then<TNextResult>(Func<TNextResult> callback)
        {
            // Create a new ThreadSafeTaskWrapperWithResult to chain a result-producing task
            ThreadSafeTaskWrapperWithResult<TNextResult> task = new ThreadSafeTaskWrapperWithResult<TNextResult>(callback, this);
            ExecuteOrScheduleTask(task);
            return task;
        }

        public ThreadSafeTaskWrapperWithResult<TNextResult> Then<TNextResult>(ThreadSafeTaskWrapperWithResult<TNextResult> task)
        {
            // Chain the existing result-producing task after this one
            task.AddFrom(this);
            ExecuteOrScheduleTask(task);
            return task;
        }

        public ThreadSafeTaskWrapperNoResult Then(Func<ThreadSafeTaskWrapperNoResult> callback)
        {
            ThreadSafeTaskWrapperNoResult? toReturn = null;
            ThreadSafeTaskWrapperNoResult task = new ThreadSafeTaskWrapperNoResult(
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
            toReturn = new ThreadSafeTaskWrapperNoResult(() => { }, task);
            ExecuteOrScheduleTask(task);
            return toReturn;
        }

        public ThreadSafeTaskWrapperWithResult<TNextResult> Then<TNextResult>(Func<ThreadSafeTaskWrapperWithResult<TNextResult>> callback)
        {
            // Chain a new result-producing task that is returned by the callback function
            TNextResult? result = default;
            ThreadSafeTaskWrapperWithResult<TNextResult>? toReturn = null;
            ThreadSafeTaskWrapperNoResult task = new ThreadSafeTaskWrapperNoResult(
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
            toReturn = new ThreadSafeTaskWrapperWithResult<TNextResult>(() => result!, task);
            ExecuteOrScheduleTask(task);
            return toReturn;
        }
        public ThreadSafeTaskWrapperNoResult Join(ThreadSafeTaskWrapperNoResult other, Action callback)
        {
            return Join(callback, other);
        }

        public ThreadSafeTaskWrapperNoResult Join(
            ThreadSafeTaskWrapperNoResult other1,
            ThreadSafeTaskWrapperNoResult other2,
            Action callback)
        {
            return Join(callback, other1, other2);
        }

        public ThreadSafeTaskWrapperNoResult Join(
            ThreadSafeTaskWrapperNoResult other1,
            ThreadSafeTaskWrapperNoResult other2,
            ThreadSafeTaskWrapperNoResult other3,
            Action callback)
        {
            return Join(callback, other1, other2, other3);
        }
        public ThreadSafeTaskWrapperNoResult Join(
                    Action callback,
                    params ThreadSafeTaskWrapperNoResult[] others)
        {
            // Lock object for thread-safe operation
            object lockObject = new object();
            // Track how many tasks have completed
            int doneCount = 0;
            // Total number of tasks to wait for (including 'this')
            int totalTasks = others.Length + 1;

            // Create the new task that will run the callback after all others are done
            ThreadSafeTaskWrapperNoResult taskToReturn = new ThreadSafeTaskWrapperNoResult(callback, this);

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
                other.Then(() =>
                {
                    checkIfDoneAndRunIfIs();
                });
            }

            // Also register the completion handler for 'this' task
            this.Then(() =>
            {
                checkIfDoneAndRunIfIs();
            });

            return taskToReturn;
        }
        public ThreadSafeTaskWrapperNoResult Catch(Action<Exception> callback) {
            ThreadSafeTaskWrapperNoResult catcher = new ThreadSafeTaskWrapperNoResult(() => {
                Exception ex;
                lock (_LockObject) {
                    ex = _Exception!;
                }
                callback(ex);
            }, this);
            Exception? exception;
            bool cancelled;
            lock (_LockObject) {
                if (!_IsCompleted)
                {
                    _Catchs!.Add(catcher);
                    return catcher;
                }
                exception = _Exception;
                cancelled = _Cancelled;
            }
            if (cancelled)
            {
                catcher.Cancel();
                return catcher;
            }
            if (exception == null)
            {
                catcher.CompleteCatcherWithoutException();
                return catcher;
            }
            Dispatcher.Instance.Run(catcher, new object[] { exception });
            return catcher;
        }
        internal void CompleteCatcherWithoutException()
        {
            List<ThreadSafeTaskWrapper>? thens;
            lock (_LockObject)
            {
                _IsCompleted = true;
                thens = _Thens;
                _Thens = null;
                _CountdownLatchWait?.Signal();
            }
            if (thens != null)
            {
                foreach (ThreadSafeTaskWrapper then in thens)
                {
                    Dispatcher.Instance.Run(then, );
                }
            }
        }
    }
}