using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace ReactiveUI871
{
    [TestClass]
    public class WhenAnyValueTest
    {
        public class Model: ReactiveObject
        {
            [Reactive]
            public object EvnetReceiver { get; set; }

            public List<int> ReceivedEvent { get; }

            public Model()
            {
                ReceivedEvent = new List<int>();

                this.WhenAnyValue(_ => _.EvnetReceiver)
                    .Where(x => x is int)
                    .Select(x => (int)x)
                    .Subscribe(x =>
                    {
                        ReceivedEvent.Add(x);
                        Debug.WriteLine(string.Format("Receive value:{0}", x));
                    });
            }
        }

        [TestMethod]
        public void MyTestMethod()
        {
            var sut = new Model();

            new int[] { 1, 2, 3, 4, 5 }.ToList().ForEach(x =>
                {
                    Debug.WriteLine(string.Format("Send value:{0}", x));
                    sut.EvnetReceiver = x;
                });

            Assert.AreEqual(5, sut.ReceivedEvent.Count);
        }
    }
}
