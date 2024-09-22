using static System.Runtime.InteropServices.JavaScript.JSType;

namespace InfernoDispatcher.Tasks
{
    public class InactiveInfernoTaskNoResult :
        InfernoTaskNoResult, IInactiveInfernoTask
    {
        public InactiveInfernoTaskNoResult(Action callback, params InfernoTask[] froms)
            : base(callback, froms)
        {

        }
    }
}