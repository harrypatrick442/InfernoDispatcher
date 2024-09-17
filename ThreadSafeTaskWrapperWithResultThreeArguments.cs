namespace InfernoDispatcher
{
    public class ThreadSafeTaskWrapperWithResultThreeArguments<TArgument1, TArgument2, TArgument3, TThisResult> : ThreadSafeTaskWrapperWithResultBase<TThisResult>
    {
        private readonly Func<TArgument1, TArgument2, TArgument3, TThisResult> _Callback;
        internal ThreadSafeTaskWrapperWithResultThreeArguments(Func<TArgument1, TArgument2, TArgument3, TThisResult> callback, params ThreadSafeTaskWrapper[] froms)
            : base(froms)
        {
            _Callback = callback;
        }
        public override void Run(object[]? arguments)
        {
            CheckNotAlreadyCompleted();
            try
            {

                var result = _Callback((TArgument1)arguments![0], (TArgument2)arguments![1], (TArgument3)arguments![2]);
                Success(result);
            }
            catch (Exception ex)
            {
                Fail(ex);
                return;
            }
        }
    }
}