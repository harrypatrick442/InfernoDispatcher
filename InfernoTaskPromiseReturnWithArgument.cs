namespace InfernoDispatcher
{
    public class InfernoTaskPromiseReturnWithArgument<TArgument, TThisResult> : InfernoTaskWithResultBase<TThisResult>
    {
        private readonly Func<TArgument, Promise<TThisResult>> _Func;
        internal InfernoTaskPromiseReturnWithArgument(Func<TArgument, Promise<TThisResult>> func, 
            params InfernoTask[] froms)
            : base(froms)
        {
            _Func = func;
        }
        public override void Run(object[]? arguments)
        {
            CheckNotAlreadyCompleted();
            try
            {
                _Func((TArgument)arguments![0]).Run(Success, Fail);
            }
            catch (Exception ex)
            {
                Fail(ex);
                return;
            }
        }
    }
}