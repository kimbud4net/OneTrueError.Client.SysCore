using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneTrueError.Client.SysCore
{

    /// <summary>
    /// SysCore Retryer
    /// </summary>
    public class ExecuteRetryer
    {
        public int RetryTimes { get; set; }
        public int WaitSeconds { get; set; }

        private int CurrentRetry { get; set; }

        public ExecuteRetryer(int retryTimes, int waitSeconds)
        {
            this.RetryTimes = retryTimes;
            this.WaitSeconds = waitSeconds;
        }

        public ExecuteRetryer() : this(5, 200)
        {
        }

        public void Execute(Action action)
        {
            CurrentRetry = 0;
            RetryExecute(action);
        }

        private void RetryExecute(Action action)
        {
            try
            {
                action();
            }
            catch
            {
                if (CurrentRetry >= RetryTimes)
                    throw;
                System.Threading.Thread.Sleep(WaitSeconds);
                Debug.WriteLine("retry:{0}", CurrentRetry);
                CurrentRetry += 1;
                RetryExecute(action);
            }
        }
    }
}
