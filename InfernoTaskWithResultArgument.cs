namespace InfernoDispatcher
{
    public class InfernoTaskWithResultArgument<TArgument1, TThisResult> : InfernoTaskWithResultBase<TThisResult>
    {
        private readonly Func<TArgument1, TThisResult> _Callback;
        internal InfernoTaskWithResultArgument(Func<TArgument1, TThisResult> callback, params InfernoTask[] froms)
            : base(froms)
        {
            _Callback = callback;
        }
        public override void Run(object[]? arguments)
        {
            CheckNotAlreadyCompleted();
            try
            {

                var result = _Callback((TArgument1)arguments![0]);
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