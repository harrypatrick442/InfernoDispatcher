namespace InfernoDispatcher
{
    public class InfernoTaskPromiseReturnNoArgument<TThisResult> : InfernoTaskWithResultBase<TThisResult>
    {
        private readonly Func<Promise<TThisResult>> _Func;
        internal InfernoTaskPromiseReturnNoArgument(Func<Promise<TThisResult>> func, 
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
                _Func().Run(Success, Fail);
            }
            catch (Exception ex)
            {
                Fail(ex);
                return;
            }
        }
    }
}