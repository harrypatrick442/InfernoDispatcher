﻿namespace InfernoDispatcher
{
    public class ThreadSafeTaskWrapperVoidPromiseReturn<TArgument> : ThreadSafeTaskWrapperNoResultBase
    {
        private readonly Func<TArgument, PromiseVoid> _Func;
        internal ThreadSafeTaskWrapperVoidPromiseReturn(Func<TArgument, PromiseVoid> func, 
            params ThreadSafeTaskWrapper[] froms)
            : base(froms)
        {
            _Func = func;
        }
        public override void Run(object[]? arguments)
        {
            CheckNotAlreadyCompleted();
            try
            {
                _Func((TArgument)arguments![0]).Run(Success, Fail);
            }
            catch (Exception ex)
            {
                Fail(ex);
                return;
            }
        }
    }
}