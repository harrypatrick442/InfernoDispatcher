namespace InfernoDispatcher.Tasks
{
    public class InfernoTaskWhateverArgument<TArgument> : InfernoTaskNoResultBase
    {
        private Action<Exception, TArgument> _Callback;
        internal InfernoTaskWhateverArgument(Action<Exception, TArgument> callback, params InfernoTask[] froms)
            : base(froms)
        {
            _Callback = callback;
        }
        public override void Run(object[]? arguments)
        {
            CheckNotAlreadyCompleted();
            try
            {

                _Callback((Exception)arguments![0], (TArgument)arguments![1]);
                Success();
            }
            catch (Exception ex)
            {
                Fail(ex);
                return;
            }
        }
    }
}