namespace InfernoDispatcher
{
    public class ThreadSafeTaskWrapperWithResult<TThisResult> : ThreadSafeTaskWrapper
    {
        private readonly Func<TThisResult> _Callback;
        private TThisResult? _Result;
        internal ThreadSafeTaskWrapperWithResult(Func<TThisResult> callback, params ThreadSafeTaskWrapper[] froms)
            : base(froms)
        {
            _Callback = callback;
        }
        public override void Run()
        {
            CheckNotAlreadyCompleted();
            TThisResult result;
            try
            {

                result = _Callback();
            }
            catch (Exception ex)
            {
                Fail(ex);
                return;
            }
            List<ThreadSafeTaskWrapper>? thens;
            lock (_LockObject)
            {
                _Result = result;
                if (_IsCompleted) return;
                _IsCompleted = true;
                thens = _Thens;
                _Thens = null;
                _CountdownLatchWait?.Signal();
            }
            if (thens != null)
            {
                foreach (ThreadSafeTaskWrapper then in thens)
                {
                    Dispatcher.Instance.Run(then);
                }
            }
        }
        public ThreadSafeTaskWrapper Then(
            Action<TThisResult> callback)
        {
            ThreadSafeTaskWrapperNoResult task = new ThreadSafeTaskWrapperNoResult(
                () => callback(CheckIsCompletedGetResultInLock()), this
            );
            ExecuteOrScheduleTask(task);
            return task;
        }
        public ThreadSafeTaskWrapperWithResult<TNextResult> Then<TNextResult>(
            Func<TThisResult, TNextResult> callback)
        {
            ThreadSafeTaskWrapperWithResult<TNextResult> task = new ThreadSafeTaskWrapperWithResult<TNextResult>(
                () => callback(CheckIsCompletedGetResultInLock()), this
            );
            ExecuteOrScheduleTask(task);
            return task;
        }
        public ThreadSafeTaskWrapperWithResult<TNextResult> Then<TNextResult>(
            ThreadSafeTaskWrapperWithResult<TNextResult> task)
        {
            task.AddFrom(this);
            ExecuteOrScheduleTask(task);
            return task;
        }
        public ThreadSafeTaskWrapperWithResult<TNextResult> Then<TNextResult>(
            Func<TThisResult, ThreadSafeTaskWrapperWithResult<TNextResult>> callback)
        {
            TNextResult? result = default(TNextResult);
            ThreadSafeTaskWrapperWithResult<TNextResult>? toReturn = null;
            ThreadSafeTaskWrapperNoResult task = new ThreadSafeTaskWrapperNoResult(
                () =>
                {
                    try
                    {
                        var childTask = callback(CheckIsCompletedGetResultInLock());
                        childTask.Then(toReturn!);
                    }
                    catch (Exception ex) {
                        toReturn!.Fail(ex);
                    }

                }, this
            );
            toReturn = new ThreadSafeTaskWrapperWithResult<TNextResult>(()=>result!, task);
            ExecuteOrScheduleTask(task);
            return toReturn;
        }
        #region Task Joining
        public ThreadSafeTaskWrapperWithResult<TNextResult> Join<TOtherResult, TNextResult>(
            ThreadSafeTaskWrapperWithResult<TOtherResult> other,
            Func<TThisResult, TOtherResult, TNextResult> callback)
        {
            object lockObject = new object();
            bool doneOne = false;
            TOtherResult? otherResult = default(TOtherResult);
            TThisResult? thisResult = default(TThisResult);
            ThreadSafeTaskWrapperWithResult<TNextResult> taskToReturn = new ThreadSafeTaskWrapperWithResult<TNextResult>(
                () => callback(thisResult!, otherResult!),
                this, other);
            Action checkIfDoneAndRunIfIs = () =>
            {
                lock (lockObject)
                {
                    if (!doneOne)
                    {
                        doneOne = true;
                        return;
                    }
                }
                Dispatcher.Instance.Run(taskToReturn);
            };
            other.Then((otherResultIn) =>
            {
                otherResult = otherResultIn;
                checkIfDoneAndRunIfIs();
            });
            this.Then((resultIn) =>
            {
                thisResult = resultIn;
                checkIfDoneAndRunIfIs();
            });
            return taskToReturn;
        }
        public ThreadSafeTaskWrapperWithResult<TNextResult> Join<TOtherResult1, TOtherResult2, TNextResult>(
    ThreadSafeTaskWrapperWithResult<TOtherResult1> other1,
    ThreadSafeTaskWrapperWithResult<TOtherResult2> other2,
    Func<TThisResult, TOtherResult1, TOtherResult2, TNextResult> callback)
        {
            object lockObject = new object();
            int doneCount = 0;
            TOtherResult1? otherResult1 = default(TOtherResult1);
            TOtherResult2? otherResult2 = default(TOtherResult2);
            TThisResult? thisResult = default(TThisResult);

            ThreadSafeTaskWrapperWithResult<TNextResult> taskToReturn = new ThreadSafeTaskWrapperWithResult<TNextResult>(
                () => callback(thisResult!, otherResult1!, otherResult2!),
                this, other1, other2
            );

            Action checkIfDoneAndRunIfIs = () =>
            {
                lock (lockObject)
                {
                    if (doneCount < 2)
                    {
                        doneCount++;
                        return;
                    }
                }
                Dispatcher.Instance.Run(taskToReturn);
            };

            other1.Then((otherResultIn) =>
            {
                lock (lockObject)
                {
                    otherResult1 = otherResultIn;
                }
                checkIfDoneAndRunIfIs();
            });

            other2.Then((otherResultIn) =>
            {
                lock (lockObject)
                {
                    otherResult2 = otherResultIn;
                }
                checkIfDoneAndRunIfIs();
            });

            this.Then((resultIn) =>
            {
                lock (lockObject)
                {
                    thisResult = resultIn;
                }
                checkIfDoneAndRunIfIs();
            });

            return taskToReturn;
        }
        public ThreadSafeTaskWrapperWithResult<TNextResult> Join<TOtherResult1, TOtherResult2, TOtherResult3, TNextResult>(
    ThreadSafeTaskWrapperWithResult<TOtherResult1> other1,
    ThreadSafeTaskWrapperWithResult<TOtherResult2> other2,
    ThreadSafeTaskWrapperWithResult<TOtherResult3> other3,
    Func<TThisResult, TOtherResult1, TOtherResult2, TOtherResult3, TNextResult> callback)
        {
            object lockObject = new object();
            int doneCount = 0; // Track completion of all tasks
            TOtherResult1? otherResult1 = default(TOtherResult1);
            TOtherResult2? otherResult2 = default(TOtherResult2);
            TOtherResult3? otherResult3 = default(TOtherResult3);
            TThisResult? thisResult = default(TThisResult);

            ThreadSafeTaskWrapperWithResult<TNextResult> taskToReturn = new ThreadSafeTaskWrapperWithResult<TNextResult>(
                () => callback(thisResult!, otherResult1!, otherResult2!, otherResult3!),
                this, other1, other2, other3
            );

            Action checkIfDoneAndRunIfIs = () =>
            {
                lock (lockObject)
                {
                    if (doneCount < 3) // Check if all 4 tasks are completed
                    {
                        doneCount++;
                        return;
                    }
                    Dispatcher.Instance.Run(taskToReturn);
                }
            };

            other1.Then((otherResultIn) =>
            {
                lock (lockObject)
                {
                    otherResult1 = otherResultIn;
                }
                checkIfDoneAndRunIfIs();
            });

            other2.Then((otherResultIn) =>
            {
                lock (lockObject)
                {
                    otherResult2 = otherResultIn;
                }
                checkIfDoneAndRunIfIs();
            });

            other3.Then((otherResultIn) =>
            {
                lock (lockObject)
                {
                    otherResult3 = otherResultIn;
                }
                checkIfDoneAndRunIfIs();
            });

            this.Then((resultIn) =>
            {
                lock (lockObject)
                {
                    thisResult = resultIn;
                }
                checkIfDoneAndRunIfIs();
            });

            return taskToReturn;
        }


        #endregion
        #region Wait
        public TThisResult? Wait()
        {
            lock (_LockObject)
            {
                if (_IsCompleted)
                {
                    if (_Cancelled)
                    {
                        throw new OperationCanceledException();
                    }
                    if (_Exception != null)
                    {
                        ThrowException();
                        return default(TThisResult);
                    }
                    return _Result!;
                }
                if (_CountdownLatchWait == null)
                {
                    _CountdownLatchWait = new CountdownLatch();
                }
            }
            _CountdownLatchWait.Wait();
            lock (_LockObject)
            {
                if (_Cancelled)
                {
                    throw new OperationCanceledException();
                }
                if (_Exception != null)
                {
                    ThrowException();
                    return default(TThisResult);
                }
                return _Result!;
            }
        }
        #endregion
        private TThisResult CheckIsCompletedGetResultInLock()
        {
            lock (_LockObject)
            {
                if (!_IsCompleted)
                    throw new InvalidOperationException("Task is not completed yet.");
                return _Result!;
            }
        }
    }
    public class ThreadSafeTaskWrapperNoResult : ThreadSafeTaskWrapper
    {
        private Action _Callback;
        internal ThreadSafeTaskWrapperNoResult(Action callback, params ThreadSafeTaskWrapper[] froms) : base(froms)
        {
            _Callback = callback;
        }

        public override void Run()
        {
            try
            {
                CheckNotAlreadyCompleted();
                List<ThreadSafeTaskWrapper>? thens;
                _Callback();
                lock (_LockObject)
                {
                    if (_IsCompleted) return;
                    _IsCompleted = true;
                    thens = _Thens;
                    _Thens = null;
                    _CountdownLatchWait?.Signal();
                }
                if (thens != null)
                {
                    foreach (ThreadSafeTaskWrapper then in thens)
                    {
                        Dispatcher.Instance.Run(then);
                    }
                }
            }
            catch (Exception ex)
            {
                Fail(ex);
            }
        }
        public void Wait()
        {
            lock (_LockObject)
            {
                if (_IsCompleted)
                {
                    if (_Cancelled)
                    {
                        throw new OperationCanceledException();
                    }
                    if (_Exception != null)
                    {
                        ThrowException();
                        return;
                    }
                    return;
                }
                if (_CountdownLatchWait == null)
                {
                    _CountdownLatchWait = new CountdownLatch();
                }
            }
            _CountdownLatchWait.Wait();
            lock (_LockObject)
            {
                if (_Cancelled)
                {
                    throw new OperationCanceledException();
                }
                if (_Exception != null)
                {
                    ThrowException();
                    return;
                }
            }
        }
    }
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
        protected bool _IsCompleted = false;
        protected Exception? _Exception;
        internal CountdownLatch? _CountdownLatchWait;
        private ThreadSafeTaskWrapper[] _Froms;
        internal ThreadSafeTaskWrapper(params ThreadSafeTaskWrapper[] froms)
        {
            _Froms = froms;
        }
        protected void AddFrom(ThreadSafeTaskWrapper from)
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
        public abstract void Run();
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
        protected void ExecuteOrScheduleTask(ThreadSafeTaskWrapper task)
        {

            Exception? exception;
            bool cancelled;
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
            Dispatcher.Instance.Run(task);
        }
        protected void Fail(Exception ex)
        {
            List<ThreadSafeTaskWrapper>? thens;
            lock (_LockObject)
            {
                if (_IsCompleted) return;
                _Exception = ex;
                _IsCompleted = true;
                thens = _Thens;
                _Thens = null;
                _CountdownLatchWait?.Signal();
            }
            if (thens == null) return;
            foreach (ThreadSafeTaskWrapper then in thens)
            {
                then.Fail(ex);
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
    }
}