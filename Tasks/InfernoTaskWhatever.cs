namespace InfernoDispatcher.Tasks
{
    public class InfernoTaskWhatever: InfernoTaskNoResultBase
    {
        private Action<InfernoTaskDoneState> _Callback;
        internal InfernoTaskWhatever(Action<InfernoTaskDoneState> callback, params InfernoTask[] froms)
            : base(froms)
        {
            _Callback = callback;
        }
        public override void Run(object[]? arguments)
        {
            CheckNotAlreadyCompleted();
            try
            {

                _Callback((InfernoTaskDoneState)arguments![0]);
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