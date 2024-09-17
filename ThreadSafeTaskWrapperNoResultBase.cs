namespace InfernoDispatcher
{
    public abstract class ThreadSafeTaskWrapperNoResultBase : ThreadSafeTaskWrapper
    {
        internal ThreadSafeTaskWrapperNoResultBase(params ThreadSafeTaskWrapper[] froms) : base(froms)
        {

        }
        protected void Success()
        {
            List<ThreadSafeTaskWrapper>? thens;
            List<ThreadSafeTaskWrapperNoResult>? catchs;
            lock (_LockObject)
            {
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
                    Dispatcher.Instance.Run(then, null);
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
        protected override object[]? ResultAsRunArguments()
        {
            return null;
        }
    }
}