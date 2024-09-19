namespace InfernoDispatcher.Tasks
{
    public class InfernoTaskWithResultTwoArguments<TArgument1, TArgument2, TThisResult> : InfernoTaskWithResultBase<TThisResult>
    {
        private readonly Func<TArgument1, TArgument2, TThisResult> _Callback;
        internal InfernoTaskWithResultTwoArguments(Func<TArgument1, TArgument2, TThisResult> callback, params InfernoTask[] froms)
            : base(froms)
        {
            _Callback = callback;
        }
        public override void Run(object[]? arguments)
        {
            CheckNotAlreadyCompleted();
            try
            {

                var result = _Callback((TArgument1)arguments![0], (TArgument2)arguments![1]);
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