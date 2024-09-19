using static System.Runtime.InteropServices.JavaScript.JSType;

namespace InfernoDispatcher.Tasks
{
    public class InfernoTaskWithResult<TThisResult> : InfernoTaskWithResultBase<TThisResult>
    {
        private readonly Func<TThisResult> _Callback;
        internal InfernoTaskWithResult(Func<TThisResult> callback, params InfernoTask[] froms)
            : base(froms)
        {
            _Callback = callback;
        }
        public override void Run(object[]? arguments)
        {
            CheckNotAlreadyCompleted();
            try
            {

                var result = _Callback();
                Success(result);
            }
            catch (Exception ex)
            {
                Fail(ex);
                return;
            }
        }
    }
}