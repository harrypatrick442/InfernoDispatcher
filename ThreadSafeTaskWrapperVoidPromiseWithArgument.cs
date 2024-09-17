namespace InfernoDispatcher
{
    public class ThreadSafeTaskWrapperVoidPromiseWithArgument<TArgument> : ThreadSafeTaskWrapperNoResultBase
    {
        private VoidPromise<TArgument> _Promise;
        internal ThreadSafeTaskWrapperVoidPromiseWithArgument(VoidPromise<TArgument> promise, params ThreadSafeTaskWrapper[] froms) : base(froms)
        {
            _Promise = promise;
        }
        public override void Run(object[]? arguments)
        {
            try
            {
                CheckNotAlreadyCompleted();
                _Promise.Run((TArgument)arguments![0], Success, Fail);
            }
            catch (Exception ex)
            {
                Fail(ex);
            }
        }
    }
}