using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SharedStateConcurrencyExample
{
    class Program
    {
        private static readonly object _listLock = new object();
        private static readonly Stack<int> _multiThreadedNumbers = new Stack<int>();

        static void Main(string[] args)
        {
            Console.WriteLine("In this example we are going to use a thread pool to insert numbers into an array");
            Console.WriteLine("We will not be using thread-safe collections, so the full horror of multi-threaded code can be witnessed");
            _multiThreadedNumbers.Push(0);
            foreach (var i in Enumerable.Range(0,50))
            {
                ThreadPool.QueueUserWorkItem(AdderFunction, i);
            }
            
            Console.WriteLine("Press any key to read output");
            Console.ReadKey();
            foreach (var multiThreadedNumber in _multiThreadedNumbers)
            {
                Console.WriteLine(multiThreadedNumber);
            }
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        static void AdderFunction(object iteration)
        {
            Console.WriteLine("Concurrent Iteration: {0}", iteration);
            lock (_listLock)
            {
                var previousNumber = _multiThreadedNumbers.Peek();
                Console.WriteLine("Adding {0}", previousNumber + 1);
                _multiThreadedNumbers.Push(previousNumber+1);
                Thread.Sleep(100);
            }
        }
    }
}
