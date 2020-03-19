using ReactiveUI871;

namespace Runner
{
    class App
    {

        static void Main(string[] args)
        {
            var test = new MessageBusTest();
            test.MessageBusSequentialCallTest();
        }
    }
}
