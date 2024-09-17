namespace InfernoDispatcher
{
    public class ThreadSafeTaskWrapperVoidPromiseNoArguments : ThreadSafeTaskWrapperNoResultBase
    {
        private VoidPromise _Promise;
        internal ThreadSafeTaskWrapperVoidPromiseNoArguments(VoidPromise promise, params ThreadSafeTaskWrapper[] froms) : base(froms)
        {
            _Promise = promise;
        }
        public override void Run(object[]? arguments)
        {
            try
            {
                CheckNotAlreadyCompleted();
                _Promise.Run(Success, Fail);
            }
            catch (Exception ex)
            {
                Fail(ex);
            }
        }
    }
}