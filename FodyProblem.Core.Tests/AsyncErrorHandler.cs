using System;
using Cirrious.CrossCore;

namespace FodyProblem.Core.Tests
{
    public static class AsyncErrorHandler
    {
        public static void HandleException(Exception exception)
        {
            Mvx.Trace(exception.StackTrace);
        }
    }
}