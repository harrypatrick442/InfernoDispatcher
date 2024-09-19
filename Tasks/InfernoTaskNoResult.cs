namespace InfernoDispatcher.Tasks
{
    public class InfernoTaskNoResult : InfernoTaskNoResultBase
    {
        private Action _Callback;
        internal InfernoTaskNoResult(Action callback, params InfernoTask[] froms) : base(froms)
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