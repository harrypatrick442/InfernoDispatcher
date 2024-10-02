namespace InfernoDispatcher.Tasks
{
    public class InfernoTaskDoneState
    {
        public Exception? Exception { get;}
        public bool Canceled { get; }
        public object[]? Result { get; }
        public InfernoTaskDoneState(Exception? exception, bool cancelled, object[]? result) {
            Exception = exception;
            Canceled = cancelled;
            Result = result;
        }
    }
}