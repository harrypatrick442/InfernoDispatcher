namespace InfernoDispatcher
{
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
}