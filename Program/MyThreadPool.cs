using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Program
{
    public class MyThreadPool
    {
        private List<Thread> _threads;

        public MyThreadPool()
        {
            _threads = new List<Thread>();
        }

        public void StartThread(ParameterizedThreadStart method, int parameter)
        {
            var newThread = new Thread(method);
            _threads.Add(newThread);
            newThread.Start(parameter);
        }
    }
}
