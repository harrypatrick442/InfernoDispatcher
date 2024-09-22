using static System.Runtime.InteropServices.JavaScript.JSType;

namespace InfernoDispatcher.Tasks
{
    public class InactiveInfernoTaskWithResult <TThisResult> : InfernoTaskWithResult<TThisResult>, IInactiveInfernoTask
    {
        public InactiveInfernoTaskWithResult(Func<TThisResult> callback, params InfernoTask[] froms)
            : base(callback, froms)
        {

        }
    }
}