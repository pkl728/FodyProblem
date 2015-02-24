using System;
using Cirrious.CrossCore;

namespace FodyProblem.iOS
{
    public static class AsyncErrorHandler
    {
        public static void HandleException(Exception exception)
        {
            Mvx.Trace(exception.StackTrace);
        }
    }
}