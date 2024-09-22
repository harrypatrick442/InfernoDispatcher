using static System.Runtime.InteropServices.JavaScript.JSType;

namespace InfernoDispatcher.Tasks
{
    public class InactiveInfernoTaskWithResultArgument <TArgument, TThisResult> : InfernoTaskWithResultArgument<TArgument, TThisResult>, IInactiveInfernoTask
    {
        public InactiveInfernoTaskWithResultArgument(Func<TArgument, TThisResult> callback, params InfernoTask[] froms)
            : base(callback, froms)
        {

        }
    }
}