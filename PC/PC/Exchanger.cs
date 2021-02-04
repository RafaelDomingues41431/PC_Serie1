using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PC
{
    public class Exchanger<T>
    {
        LinkedList<T> failedExchange;
        static T elem1;
        static T elem2;

        Object exchangeSignal;

        public Exchanger(){
            failedExchange = new LinkedList<T>();
            exchangeSignal = new object();
        }

        public bool Exchange(T mine, int timeout, out T yours)
        {
            lock (exchangeSignal)
            {
                foreach(T elem in failedExchange)
                {
                    if (elem.Equals(mine)) {
                        yours = default(T);
                        return false;
                    }
                }

                if(elem1 == null)
                {
                    elem1 = mine;
                    try {
                        if (Monitor.Wait(exchangeSignal, timeout))
                        {
                            yours = elem2;
                            elem1 = default(T);
                            elem2 = default(T);
                            return true;
                        }
                        else
                        {
                            yours = default(T);
                            return false;
                        }
                    }
                    catch (ThreadInterruptedException)
                    {
                        throw new ThreadInterruptedException();
                    }
                }
                else
                {
                    elem2 = mine;
                    yours = elem1;
                    Monitor.Pulse(exchangeSignal);
                    return true;
                }
            }
        }
    }






    public class ExchangerTest
    {
        public static void TestExchange2Threads(Exchanger<string> exchanger) {
            Thread thread1 = new Thread(()=>
            {
                string yours;
                exchanger.Exchange("thread1 message", Timeout.Infinite, out yours);
                Console.WriteLine("Thread1 received: " + yours);
            });

            Thread thread2 = new Thread(() =>
            {
                string yours;
                exchanger.Exchange("thread2 message", Timeout.Infinite, out yours);
                Console.WriteLine("Thread2 received: " + yours);
            });

            thread1.Start();
            thread2.Start();
            thread1.Join();
            thread2.Join();
        }

        public static void TestExchange(Exchanger<string> exchanger)
        {
            int nThreads = 6;
            Thread[] threads = new Thread[6];
            for (int i = 0; i < nThreads; i++)
            {
                int threadId = i;
                threads[i] = new Thread(()=>
                {
                    string yours;
                    exchanger.Exchange(threadId + " message", Timeout.Infinite, out yours);
                    Console.WriteLine(threadId + " received: " + yours);
                });
                threads[i].Start();
            }

            for (int i = 0; i < nThreads; i++)
                threads[i].Join();
        }

        public static void Main()
        {
            Exchanger<string> exchanger = new Exchanger<string>();
            TestExchange(exchanger);
            Console.ReadKey();
        }
    }
}
