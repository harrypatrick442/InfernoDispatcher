namespace InfernoDispatcher
{
    public class ThreadSafeTaskWrapperNoResult : ThreadSafeTaskWrapperNoResultBase
    {
        private Action _Callback;
        internal ThreadSafeTaskWrapperNoResult(Action callback, params ThreadSafeTaskWrapper[] froms) : base(froms)
        {
            _Callback = callback;
        }

        public override void Run(object[]? arguments)
        {
            try
            {
                CheckNotAlreadyCompleted();
                _Callback();
                Success();
            }
            catch (Exception ex)
            {
                Fail(ex);
            }
        }
    }
}