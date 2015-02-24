using System;
using Cirrious.CrossCore;

namespace FodyProblem.Core.Utilities
{
    public static class AsyncErrorHandler
    {
        public static void HandleException(Exception exception)
        {
            Mvx.Trace(exception.StackTrace);
        }
    }
}