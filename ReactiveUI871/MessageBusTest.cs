using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReactiveUI;

namespace ReactiveUI871
{
    [TestClass]
    public class MessageBusTest
    {
        [TestMethod]
        public void MessageBusSequentialCallTest()
        {
            var syncer = new object();
            var disoacher = Dispatcher.CurrentDispatcher;

            Task.Run(() =>
            {
                Debug.WriteLine(string.Format("ManagerThreadID: {0}", Thread.CurrentThread.ManagedThreadId));
                disoacher = Dispatcher.CurrentDispatcher;
                Dispatcher.Run();
            });

            Task.Run(() =>
            {
                //var mainScheduler = CurrentThreadScheduler.Instance;
                Debug.WriteLine(string.Format("MainThreadID: {0}", Thread.CurrentThread.ManagedThreadId));

                var messageBus = new MessageBus();
                //messageBus.RegisterScheduler<int>(CurrentThreadScheduler.Instance);
                messageBus.Listen<int>().Subscribe(i =>
                {
                    disoacher.Invoke(() => 
                    {
                        Debug.WriteLine(string.Format("OUT:TaskNumber:{0}, ThreadId:{1}", i, Thread.CurrentThread.ManagedThreadId));
                    });
                });

                Parallel.ForEach(new int[] { 1, 2, 3, 4, 5 }, i =>
                {
                    lock (syncer)
                    {
                        Debug.WriteLine(string.Format("IN:TaskNumber:{0}, ThreadId:{1}", i, Thread.CurrentThread.ManagedThreadId));
                        Task.Run(() => messageBus.SendMessage(i));
                    }
                });

            }).Wait();
        }
    }
}
