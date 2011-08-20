using System;
using System.Collections.Generic;

namespace OstrichNet.Util
{
    /// <summary>
    /// Helps to do retry logic.  A typical use would be:
    /// 
    /// Retry.AtMost(3)
    /// .Try(() => 
    /// {
    ///   DoSomethingRetryable();
    /// });
    /// </summary>
    public class Retry
    {
        private readonly int atMost;

        /// <summary>
        /// Prevents a default instance of the <see cref="Retry"/> class from being created.
        /// </summary>
        /// <param name="atMost">At most.</param>
        private Retry(int atMost)
        {
            if (atMost < 0)
                throw new ArgumentException("Must be greater than 0");
            this.atMost = atMost;
        }

        /// <summary>
        /// Creates a retry instance that will only allow the number of retries specified.
        /// </summary>
        /// <param name="times">The times.</param>
        /// <returns></returns>
        public static Retry AtMost(int times)
        {
            return new Retry(times);
        }

        /// <summary>
        /// Tries the specified action for until the limit is reached
        /// </summary>
        /// <param name="action">The action.</param>
        public void Try(Action action)
        {
            Try(() => ExecuteActionAsFunc(action));
        }

        /// <summary>
        /// Tries the specified action until the limit is reached.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        public TResult Try<TResult>(Func<TResult> action)
        {
            var exceptions = new List<Exception>(atMost);
            int count = 0;
            while (true)
            {
                try
                {
                    count++;
                    return action();
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                    if (count >= atMost)
                        throw new AggregateException(exceptions);
                }
            }
        }

        private static object ExecuteActionAsFunc(Action action)
        {
            action();
            return null;
        }
    }
}
