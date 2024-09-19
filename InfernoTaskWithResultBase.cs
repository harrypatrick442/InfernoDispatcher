namespace InfernoDispatcher
{
    public abstract class InfernoTaskWithResultBase<TThisResult> : InfernoTask
    {
        internal InfernoTaskWithResultBase(params InfernoTask[] froms)
            : base(froms)
        {

        }
        protected void Success(TThisResult result)
        {
            Success(new object[] { result! });
        }
        public InfernoTaskNoResultWithArgument<TThisResult> Then(
            Action<TThisResult> callback)
        {
            return ExecuteOrScheduleTask(
                new InfernoTaskNoResultWithArgument<TThisResult>(callback, this)
            );
        }
        public InfernoTaskWithResultArgument<TThisResult, TNextResult> Then<TNextResult>(
            Func<TThisResult, TNextResult> callback)
        {
            return ExecuteOrScheduleTask(new InfernoTaskWithResultArgument<TThisResult, TNextResult>(
                callback, this)
            );
        }
        public InfernoTaskPromiseNoArgument<TNextResult> Then<TNextResult>(
            Promise<TNextResult> promise)
        {
            return ExecuteOrScheduleTask(new InfernoTaskPromiseNoArgument<TNextResult>(
                promise, this));
        }
        public InfernoTaskPromiseReturnWithArgument<TThisResult, TNextResult> Then<TNextResult>(
            Func<TThisResult, Promise<TNextResult>> promise)
        {
            return ExecuteOrScheduleTask(new InfernoTaskPromiseReturnWithArgument<TThisResult, TNextResult>(
                promise, this));
        }
        public InfernoTaskVoidPromiseReturn<TThisResult> Then<TNextResult>(
            Func<TThisResult, PromiseVoid> promise)
        {
            return ExecuteOrScheduleTask(new InfernoTaskVoidPromiseReturn<TThisResult>(
                promise, this));
        }
        public InfernoTaskPromiseWithArgument<TThisResult, TNextResult> Then<TNextResult>(PromiseParametrized<TThisResult, TNextResult> promise)
        {
            return ExecuteOrScheduleTask(new InfernoTaskPromiseWithArgument<TThisResult, TNextResult>(
                promise, this));
        }
        public InfernoTaskWithResultBase<TNextResult> ThenCreateTask<TNextResult>(
            Func<TThisResult, InfernoTaskWithResult<TNextResult>> callback)
        {
            return ThenCreateTask((a=> (InfernoTaskWithResultBase<TNextResult>)callback(a)));
        }
        public InfernoTaskWithResultBase<TNextResult> ThenCreateTask<TNextResult>(
            Func<TThisResult, InfernoTaskWithResultBase<TNextResult>> callback)
        {
            InfernoTaskWithResultArgument<TNextResult, TNextResult>? toReturn = null;
            InfernoTaskNoResult task = new InfernoTaskNoResult(
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
            toReturn = new InfernoTaskWithResultArgument<TNextResult, TNextResult>((r) => r!, task);
            ExecuteOrScheduleTask(task);
            return toReturn;
        }
        #region Task Joining
        public InfernoTaskWithResultTwoArguments<TThisResult, TOtherResult, TNextResult> Join<TOtherResult, TNextResult>(
            InfernoTaskWithResultBase<TOtherResult> other,
            Func<TThisResult, TOtherResult, TNextResult> callback)
        {
            object lockObject = new object();
            bool doneOne = false;
            TOtherResult? otherResult = default(TOtherResult);
            TThisResult? thisResult = default(TThisResult);
            var taskToReturn = new InfernoTaskWithResultTwoArguments<TThisResult, TOtherResult, TNextResult>(
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
        public InfernoTaskWithResultThreeArguments<TThisResult, TOtherResult1, TOtherResult2, TNextResult> Join<TOtherResult1, TOtherResult2, TNextResult>(
    InfernoTaskWithResultBase<TOtherResult1> other1,
    InfernoTaskWithResultBase<TOtherResult2> other2,
    Func<TThisResult, TOtherResult1, TOtherResult2, TNextResult> callback)
        {
            object lockObject = new object();
            int doneCount = 0;
            TOtherResult1? otherResult1 = default(TOtherResult1);
            TOtherResult2? otherResult2 = default(TOtherResult2);
            TThisResult? thisResult = default(TThisResult);

            InfernoTaskWithResultThreeArguments<TThisResult, TOtherResult1, TOtherResult2, TNextResult> taskToReturn = 
                new InfernoTaskWithResultThreeArguments<TThisResult, TOtherResult1, TOtherResult2, TNextResult>(
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
        public InfernoTaskWithResultFourArguments<TThisResult, TOtherResult1, TOtherResult2, TOtherResult3, TNextResult> 
            Join<TOtherResult1, TOtherResult2, TOtherResult3, TNextResult>(
    InfernoTaskWithResultBase<TOtherResult1> other1,
    InfernoTaskWithResultBase<TOtherResult2> other2,
    InfernoTaskWithResultBase<TOtherResult3> other3,
    Func<TThisResult, TOtherResult1, TOtherResult2, TOtherResult3, TNextResult> callback)
        {
            object lockObject = new object();
            int doneCount = 0; // Track completion of all tasks
            TOtherResult1? otherResult1 = default(TOtherResult1);
            TOtherResult2? otherResult2 = default(TOtherResult2);
            TOtherResult3? otherResult3 = default(TOtherResult3);
            TThisResult? thisResult = default(TThisResult);

            var taskToReturn = new InfernoTaskWithResultFourArguments<TThisResult, TOtherResult1, TOtherResult2, TOtherResult3, TNextResult>(callback,
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