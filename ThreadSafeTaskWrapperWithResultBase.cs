namespace InfernoDispatcher
{
    public abstract class ThreadSafeTaskWrapperWithResultBase<TThisResult> : ThreadSafeTaskWrapper
    {
        internal ThreadSafeTaskWrapperWithResultBase(params ThreadSafeTaskWrapper[] froms)
            : base(froms)
        {

        }
        protected void Success(TThisResult result)
        {
            Success(new object[] { result! });
        }
        public ThreadSafeTaskWrapperNoResultWithArgument<TThisResult> Then(
            Action<TThisResult> callback)
        {
            return ExecuteOrScheduleTask(
                new ThreadSafeTaskWrapperNoResultWithArgument<TThisResult>(callback, this)
            );
        }
        public ThreadSafeTaskWrapperWithResultArgument<TThisResult, TNextResult> Then<TNextResult>(
            Func<TThisResult, TNextResult> callback)
        {
            return ExecuteOrScheduleTask(new ThreadSafeTaskWrapperWithResultArgument<TThisResult, TNextResult>(
                callback, this)
            );
        }
        public ThreadSafeTaskWrapperPromiseNoArgument<TNextResult> Then<TNextResult>(
            Promise<TNextResult> promise)
        {
            return ExecuteOrScheduleTask(new ThreadSafeTaskWrapperPromiseNoArgument<TNextResult>(
                promise, this));
        }
        public ThreadSafeTaskWrapperPromiseReturnWithArgument<TThisResult, TNextResult> Then<TNextResult>(
            Func<TThisResult, Promise<TNextResult>> promise)
        {
            return ExecuteOrScheduleTask(new ThreadSafeTaskWrapperPromiseReturnWithArgument<TThisResult, TNextResult>(
                promise, this));
        }
        public ThreadSafeTaskWrapperVoidPromiseReturn<TThisResult> Then<TNextResult>(
            Func<TThisResult, PromiseVoid> promise)
        {
            return ExecuteOrScheduleTask(new ThreadSafeTaskWrapperVoidPromiseReturn<TThisResult>(
                promise, this));
        }
        public ThreadSafeTaskWrapperPromiseWithArgument<TThisResult, TNextResult> Then<TNextResult>(PromiseParametrized<TThisResult, TNextResult> promise)
        {
            return ExecuteOrScheduleTask(new ThreadSafeTaskWrapperPromiseWithArgument<TThisResult, TNextResult>(
                promise, this));
        }
        public ThreadSafeTaskWrapperWithResultBase<TNextResult> ThenCreateTask<TNextResult>(
            Func<TThisResult, ThreadSafeTaskWrapperWithResult<TNextResult>> callback)
        {
            return ThenCreateTask((a=> (ThreadSafeTaskWrapperWithResultBase<TNextResult>)callback(a)));
        }
        public ThreadSafeTaskWrapperWithResultBase<TNextResult> ThenCreateTask<TNextResult>(
            Func<TThisResult, ThreadSafeTaskWrapperWithResultBase<TNextResult>> callback)
        {
            ThreadSafeTaskWrapperWithResultArgument<TNextResult, TNextResult>? toReturn = null;
            ThreadSafeTaskWrapperNoResult task = new ThreadSafeTaskWrapperNoResult(
                () =>
                {
                    try
                    {
                        var childTask = callback(CheckIsCompletedGetResultInLock());
                        childTask.ThenExistingTask(toReturn!);
                    }
                    catch (Exception ex)
                    {
                        toReturn!.Fail(ex);
                    }

                }, this
            );
            toReturn = new ThreadSafeTaskWrapperWithResultArgument<TNextResult, TNextResult>((r) => r!, task);
            ExecuteOrScheduleTask(task);
            return toReturn;
        }
        #region Task Joining
        public ThreadSafeTaskWrapperWithResultTwoArguments<TThisResult, TOtherResult, TNextResult> Join<TOtherResult, TNextResult>(
            ThreadSafeTaskWrapperWithResultBase<TOtherResult> other,
            Func<TThisResult, TOtherResult, TNextResult> callback)
        {
            object lockObject = new object();
            bool doneOne = false;
            TOtherResult? otherResult = default(TOtherResult);
            TThisResult? thisResult = default(TThisResult);
            var taskToReturn = new ThreadSafeTaskWrapperWithResultTwoArguments<TThisResult, TOtherResult, TNextResult>(
                callback,
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
                Dispatcher.Instance.Run(taskToReturn, new object[] {thisResult!, otherResult!});
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
        public ThreadSafeTaskWrapperWithResultThreeArguments<TThisResult, TOtherResult1, TOtherResult2, TNextResult> Join<TOtherResult1, TOtherResult2, TNextResult>(
    ThreadSafeTaskWrapperWithResultBase<TOtherResult1> other1,
    ThreadSafeTaskWrapperWithResultBase<TOtherResult2> other2,
    Func<TThisResult, TOtherResult1, TOtherResult2, TNextResult> callback)
        {
            object lockObject = new object();
            int doneCount = 0;
            TOtherResult1? otherResult1 = default(TOtherResult1);
            TOtherResult2? otherResult2 = default(TOtherResult2);
            TThisResult? thisResult = default(TThisResult);

            ThreadSafeTaskWrapperWithResultThreeArguments<TThisResult, TOtherResult1, TOtherResult2, TNextResult> taskToReturn = 
                new ThreadSafeTaskWrapperWithResultThreeArguments<TThisResult, TOtherResult1, TOtherResult2, TNextResult>(
                callback,
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
                Dispatcher.Instance.Run(taskToReturn, new object[] { });
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
        public ThreadSafeTaskWrapperWithResultFourArguments<TThisResult, TOtherResult1, TOtherResult2, TOtherResult3, TNextResult> 
            Join<TOtherResult1, TOtherResult2, TOtherResult3, TNextResult>(
    ThreadSafeTaskWrapperWithResultBase<TOtherResult1> other1,
    ThreadSafeTaskWrapperWithResultBase<TOtherResult2> other2,
    ThreadSafeTaskWrapperWithResultBase<TOtherResult3> other3,
    Func<TThisResult, TOtherResult1, TOtherResult2, TOtherResult3, TNextResult> callback)
        {
            object lockObject = new object();
            int doneCount = 0; // Track completion of all tasks
            TOtherResult1? otherResult1 = default(TOtherResult1);
            TOtherResult2? otherResult2 = default(TOtherResult2);
            TOtherResult3? otherResult3 = default(TOtherResult3);
            TThisResult? thisResult = default(TThisResult);

            var taskToReturn = new ThreadSafeTaskWrapperWithResultFourArguments<TThisResult, TOtherResult1, TOtherResult2, TOtherResult3, TNextResult>(callback,
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
                    Dispatcher.Instance.Run(taskToReturn, new object[] { thisResult!, otherResult1!, otherResult2!, otherResult3!});
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
                    return (TThisResult)_Result![0];
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
                return (TThisResult)_Result![0];
            }
        }
        #endregion
        private TThisResult CheckIsCompletedGetResultInLock()
        {
            lock (_LockObject)
            {
                if (!_IsCompleted)
                    throw new InvalidOperationException("Task is not completed yet.");
                return (TThisResult)_Result![0];
            }
        }
    }
}