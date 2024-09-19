namespace InfernoDispatcher.Tasks
{
    public class InfernoTaskNoResultWithArgument<TArgument> : InfernoTaskNoResultBase
    {
        private Action<TArgument> _Callback;
        internal InfernoTaskNoResultWithArgument(Action<TArgument> callback, params InfernoTask[] froms) : base(froms)
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