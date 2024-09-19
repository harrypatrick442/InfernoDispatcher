namespace InfernoDispatcher
{
    public class InfernoTaskWithResultThreeArguments<TArgument1, TArgument2, TArgument3, TThisResult> : InfernoTaskWithResultBase<TThisResult>
    {
        private readonly Func<TArgument1, TArgument2, TArgument3, TThisResult> _Callback;
        internal InfernoTaskWithResultThreeArguments(Func<TArgument1, TArgument2, TArgument3, TThisResult> callback, params InfernoTask[] froms)
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