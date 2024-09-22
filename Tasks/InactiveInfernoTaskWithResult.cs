using static System.Runtime.InteropServices.JavaScript.JSType;

namespace InfernoDispatcher.Tasks
{
    public class InactiveInfernoTaskWithResult <TThisResult> : InfernoTaskWithResult<TThisResult>, IInactiveInfernoTask
    {
        public InactiveInfernoTaskWithResult(Func<TThisResult>? callback, params InfernoTask[] froms)
            : base(callback, froms)
        {

        }
    }
    public class InactiveInfernoTaskWithResult
    {
        public static InactiveInfernoTaskWithResult<TResult> NewSuccess<TResult>(TResult result)
        {
            var task = new InactiveInfernoTaskWithResult<TResult>(null);
            task.Success(result);
            return task;
        }
    }
}