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
            List<ThreadSafeTaskWrapperNoResult>? catchs;
            lock (_LockObject)
            {
                _Result = result;
                if (_IsCompleted) return;
                _IsCompleted = true;
                thens = _Thens;
                _Thens = null;
                catchs = _Catchs;
                _Catchs = null;
                _CountdownLatchWait?.Signal();
            }
            if (thens != null)
            {
                foreach (ThreadSafeTaskWrapper then in thens)
                {
                    Dispatcher.Instance.Run(then);
                }
            }
            if (catchs != null)
            {
                foreach (ThreadSafeTaskWrapperNoResult catcher in catchs)
                {
                    catcher.CompleteCatcherWithoutException();
                }
            }
        }
        public ThreadSafeTaskWrapperNoResult Then(
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
}