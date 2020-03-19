using System;
using System.Collections.Generic;
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
        private class TaskNumberThreadId
        {
            public TaskNumberThreadId(int taskNumber, int threadId)
            {
                TaskNumber = taskNumber;
                ThreadId = threadId;
            }

            public int TaskNumber { get; }
            public int ThreadId { get; }

            public override bool Equals(object obj)
            {
                return obj is TaskNumberThreadId id &&
                       TaskNumber == id.TaskNumber &&
                       ThreadId == id.ThreadId;
            }

            public override string ToString()
            {
                return string.Format("TaskNumber: {0}, ThreadId: {1}", TaskNumber, ThreadId);
            }
        }

        [TestMethod]
        public void MessageBusSequentialCallTest()
        {
            var expectListA = new List<TaskNumberThreadId>();
            var actualListA = new List<TaskNumberThreadId>();
            var expectListB = new List<TaskNumberThreadId>();
            var actualListB = new List<TaskNumberThreadId>();

            var syncer = new object();

            // Make Thread A
            Dispatcher threadA_dispacher = null;
            Task.Run(() =>
            {
                Debug.WriteLine(string.Format("Thread-A ID: {0}", Thread.CurrentThread.ManagedThreadId));
                threadA_dispacher = Dispatcher.CurrentDispatcher;
                Dispatcher.Run();
            });

            // Get Thread A Scheduler
            DispatcherScheduler threadA_scheduler = null;
            while (threadA_dispacher == null)
            {
                Thread.Sleep(10);
            }
            threadA_dispacher.Invoke(() =>
            {
                threadA_scheduler = DispatcherScheduler.Current;
            });

            // Make Thread B
            Dispatcher threadB_dispacher = null;
            Task.Run(() =>
            {
                Debug.WriteLine(string.Format("Thread-B ID: {0}", Thread.CurrentThread.ManagedThreadId));
                threadB_dispacher = Dispatcher.CurrentDispatcher;
                Dispatcher.Run();
            });

            // Get Thread B Scheduler
            DispatcherScheduler threadB_scheduler = null;
            while (threadB_dispacher == null)
            {
                Thread.Sleep(10);
            }
            threadB_dispacher.Invoke(() =>
            {
                threadB_scheduler = DispatcherScheduler.Current;
            });

            // Run with anonimous thread
            Task.Run(() =>
            {
                Debug.WriteLine(string.Format("Run Thread ID: {0}", Thread.CurrentThread.ManagedThreadId));

                // Create MessageBus instance and regist scheduler
                var messageBus = new MessageBus();
                messageBus.RegisterScheduler<int>(threadA_scheduler, "Thread-A");
                messageBus.RegisterScheduler<int>(threadB_scheduler, "Thread-B");

                // Regist process to Listener
                messageBus.Listen<int>("Thread-A").Subscribe(i =>
                {
                    Debug.WriteLine(string.Format("OUT:TaskNumber:{0}, ThreadId:{1} Thread-A", i, Thread.CurrentThread.ManagedThreadId));
                    actualListA.Add(new TaskNumberThreadId(i, Thread.CurrentThread.ManagedThreadId));
                });
                messageBus.Listen<int>("Thread-B").Subscribe(i =>
                {
                    Debug.WriteLine(string.Format("OUT:TaskNumber:{0}, ThreadId:{1} Thread-B", i, Thread.CurrentThread.ManagedThreadId));
                    actualListB.Add(new TaskNumberThreadId(i, Thread.CurrentThread.ManagedThreadId));
                });

                // Call SendMessage with parallel threads.
                Parallel.ForEach(new int[] { 1, 2, 3, 4, 5 }, i =>
                {
                    lock (syncer)
                    {
                        Debug.WriteLine(string.Format("IN:TaskNumber:{0}, ThreadId:{1}", i, Thread.CurrentThread.ManagedThreadId));
                        messageBus.SendMessage(i, "Thread-A");
                        expectListA.Add(new TaskNumberThreadId(i, threadA_scheduler.Dispatcher.Thread.ManagedThreadId));
                        messageBus.SendMessage(i, "Thread-B");
                        expectListB.Add(new TaskNumberThreadId(i, threadB_scheduler.Dispatcher.Thread.ManagedThreadId));
                    }
                });

            }).Wait();

            expectListA.ForEach(expect => Assert.AreEqual(expect, actualListA[expectListA.IndexOf(expect)], "Thread-A"));
            expectListB.ForEach(expect => Assert.AreEqual(expect, actualListB[expectListB.IndexOf(expect)], "Thread-B"));
        }
    }
}
