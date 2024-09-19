using System.Threading.Tasks;
using InfernoDispatcher.Core;
using InfernoDispatcher.Promises;

namespace InfernoDispatcher.Tasks
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
        #region Then
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
        public InfernoTaskPromiseReturnWithArgument<TThisResult, TNextResult> Then<TNextResult>(
            Func<TThisResult, Promise<TNextResult>> promise)
        {
            return ExecuteOrScheduleTask(new InfernoTaskPromiseReturnWithArgument<TThisResult, TNextResult>(
                promise, this));
        }
        public InfernoTaskVoidPromiseReturnWithArgument<TThisResult> Then<TNextResult>(
            Func<TThisResult, PromiseVoid> promise)
        {
            return ExecuteOrScheduleTask(new InfernoTaskVoidPromiseReturnWithArgument<TThisResult>(
                promise, this));
        }
        public InfernoTaskPromiseWithArgument<TThisResult, TNextResult> Then<TNextResult>(
            PromiseParametrized<TThisResult, TNextResult> promise)
        {
            return ExecuteOrScheduleTask(new InfernoTaskPromiseWithArgument<TThisResult, TNextResult>(
                promise, this));
        }
        #endregion
        #region ThenCreateTask
        public InfernoTaskWithResultBase<TNextResult> ThenCreateTask<TNextResult>(
            Func<TThisResult, InfernoTaskWithResult<TNextResult>> callback)
        {
            return ThenCreateTask(a => (InfernoTaskWithResultBase<TNextResult>)callback(a));
        }
        public InfernoTaskWithResultBase<TNextResult> ThenCreateTask<TNextResult>(
            Func<TThisResult, InfernoTaskWithResultBase<TNextResult>> callback)
        {
            InfernoTaskWithResultArgument<TNextResult, TNextResult>? toReturn = null;
            InfernoTaskNoResultWithArgument<TThisResult> task = new InfernoTaskNoResultWithArgument<TThisResult>(
                (result) =>
                {
                    try
                    {
                        var childTask = callback(result);
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
        #endregion
        #region Task Joining
        public InfernoTaskWithResultTwoArguments<TThisResult, TOtherResult, TNextResult> Join<TOtherResult, TNextResult>(
            InfernoTaskWithResultBase<TOtherResult> other,
            Func<TThisResult, TOtherResult, TNextResult> callback)
        {
            object lockObject = new object();
            bool doneOne = false;
            TOtherResult? otherResult = default;
            TThisResult? thisResult = default;
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
                Dispatcher.Instance.Run(taskToReturn, new object[] { thisResult!, otherResult! });
            };
            other.Then((otherResultIn) =>
            {
                otherResult = otherResultIn;
                checkIfDoneAndRunIfIs();
            });
            Then((resultIn) =>
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
            TOtherResult1? otherResult1 = default;
            TOtherResult2? otherResult2 = default;
            TThisResult? thisResult = default;

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

            Then((resultIn) =>
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
            TOtherResult1? otherResult1 = default;
            TOtherResult2? otherResult2 = default;
            TOtherResult3? otherResult3 = default;
            TThisResult? thisResult = default;

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
                    Dispatcher.Instance.Run(taskToReturn, new object[] { thisResult!, otherResult1!, otherResult2!, otherResult3! });
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

            Then((resultIn) =>
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
        #region Delay

        public InfernoTaskNoResultWithArgument<TThisResult> Delay(
            int millisecondsDelay,
            Action<TThisResult> callback)
        {
            return Then(new PromiseParametrized<TThisResult, TThisResult>((a, resolve, reject) =>
            {
                Task.Delay(millisecondsDelay).ContinueWith((ignore) => resolve(a));
            })).Then(callback);
        }
        public InfernoTaskWithResultArgument<TThisResult, TNextResult> Delay<TNextResult>(
            int millisecondsDelay,
            Func<TThisResult, TNextResult> callback)
        {
            return Then(new PromiseParametrized<TThisResult, TThisResult>((a, resolve, reject) =>
            {
                Task.Delay(millisecondsDelay).ContinueWith((ignore) => resolve(a));
            })).Then(callback);
        }
        public InfernoTaskPromiseReturnWithArgument<TThisResult, TNextResult> Delay<TNextResult>(
            int millisecondsDelay,
            Func<TThisResult, Promise<TNextResult>> promise)
        {
            return Then(new PromiseParametrized<TThisResult, TThisResult>((a, resolve, reject) =>
            {
                Task.Delay(millisecondsDelay).ContinueWith((ignore) => resolve(a));
            })).Then(promise);
        }
        public InfernoTaskVoidPromiseReturnWithArgument<TThisResult> Delay<TNextResult>(
            int millisecondsDelay,
            Func<TThisResult, PromiseVoid> func)
        {
            return Then(new PromiseParametrized<TThisResult, TThisResult>((a, resolve, reject) =>
            {
                Task.Delay(millisecondsDelay).ContinueWith((ignore) => resolve(a));
            })).Then<TThisResult>(func);
        }
        public InfernoTaskPromiseWithArgument<TThisResult, TNextResult> Delay<TNextResult>(
            int millisecondsDelay, PromiseParametrized<TThisResult, TNextResult> promise)
        {
            return Then(new PromiseParametrized<TThisResult, TThisResult>((a, resolve, reject) =>
            {
                Task.Delay(millisecondsDelay).ContinueWith((ignore) => resolve(a));
            })).Then(promise);
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
                        return default;
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
                    return default;
                }
                return (TThisResult)_Result![0];
            }
        }
        #endregion
        #region Awaitable methods
        public virtual TThisResult GetResult()
        {
            lock (_LockObject)
            {
                if (_Cancelled)
                {
                    throw new OperationCanceledException();
                }
                if (_Exception != null)
                {
                    ThrowException();
                    return default;
                }
                return (TThisResult)_Result![0];
            }
        }
        #endregion
    }
}