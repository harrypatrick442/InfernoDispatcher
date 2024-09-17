namespace InfernoDispatcher
{
    public class ThreadSafeTaskWrapperWithResultFourArguments<TArgument1, TArgument2, TArgument3, TArgument4, TThisResult> : ThreadSafeTaskWrapperWithResultBase<TThisResult>
    {
        private readonly Func<TArgument1, TArgument2, TArgument3, TArgument4, TThisResult> _Callback;
        internal ThreadSafeTaskWrapperWithResultFourArguments(
            Func<TArgument1, TArgument2, TArgument3, TArgument4, TThisResult> callback, params ThreadSafeTaskWrapper[] froms)
            : base(froms)
        {
            _Callback = callback;
        }
        public override void Run(object[]? arguments)
        {
            CheckNotAlreadyCompleted();
            try
            {

                var result = _Callback((TArgument1)arguments![0], (TArgument2)arguments![1], (TArgument3)arguments![2], (TArgument4)arguments![3]);
                Success(result);
            }
            catch (Exception ex)
            {
                Fail(ex);
                return;
            }
        }

        protected override object[]? ResultAsRunArguments()
        {
            return new object[] { _Result! };
        }
    }
}