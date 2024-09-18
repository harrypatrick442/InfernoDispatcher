namespace InfernoDispatcher
{
    public class ThreadSafeTaskWrapperNoResultWithArgument<TArgument> : ThreadSafeTaskWrapperNoResultBase
    {
        private Action<TArgument> _Callback;
        internal ThreadSafeTaskWrapperNoResultWithArgument(Action<TArgument> callback, params ThreadSafeTaskWrapper[] froms) : base(froms)
        {
            _Callback = callback;
        }

        public override void Run(object[]? arguments)
        {
            try
            {
                CheckNotAlreadyCompleted();
                _Callback((TArgument)arguments![0]);
                Success();
            }
            catch (Exception ex)
            {
                Fail(ex);
            }
        }
    }
}