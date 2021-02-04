using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PC
{
    public class ExpirableLazy<T> where T : class
    {

        Func<T> provider;
        TimeSpan timeToLive;
        bool processingProvider;
        int startTime;
        int threadsWaiting;
        bool failed;
        private T value;

        public ExpirableLazy(Func<T> provider, TimeSpan timeToLive){
            this.provider = provider;
            this.timeToLive = timeToLive;
            startTime = 0;
            processingProvider=false;
            failed = false;
            threadsWaiting = 0;
        }

        public T Value
        {
            get
            {
                if (value != null && ((Environment.TickCount - startTime) < timeToLive.TotalMilliseconds))
                    return value;
                else
                {
                    lock (this)
                    {
                        if (processingProvider)
                        {
                            try
                            {
                                ++threadsWaiting;
                                Monitor.Wait(this);
                                --threadsWaiting;
                                if (!failed && value != null && ((Environment.TickCount - startTime) < timeToLive.TotalMilliseconds))
                                {
                                    if (threadsWaiting > 0)
                                        Monitor.Pulse(this);
                                    return Value;
                                }
                            }
                            catch (ThreadInterruptedException)
                            {
                                --threadsWaiting;
                                throw new ThreadInterruptedException();
                            }
                        }
                        else
                        {
                            processingProvider = true;
                        }
                    }
                    try
                    {
                        this.value = provider();
                        lock (this) {
                            startTime = Environment.TickCount;
                            failed = false;
                            processingProvider = false;
                            if (threadsWaiting > 0)
                                Monitor.Pulse(this);
                            return value;
                        }
                    }
                    catch (Exception e)
                    {
                        lock (this)
                        {
                            failed = true;
                            Monitor.Pulse(this);
                            throw e;
                        }
                    }
                }
            }
        }
    }




    class TestObj
    {
        public int value;
        public TestObj(int value) {
            this.value = value;
        }
    }

    class ExpirableLazyTest
    {

        public static void TestExpirableLazy()
        {
            Func<TestObj> func = new Func<TestObj>(() =>
            {
                
                Thread.Sleep(2000);
                return new TestObj(Environment.TickCount);
            });
            
            ExpirableLazy<TestObj> lazy = new ExpirableLazy<TestObj>(func,new TimeSpan(0,0,3));

            int nThreads = 2;
            Thread[] threads = new Thread[nThreads];

            for(int i= 0; i < nThreads; i++)
            {
                int threadId = i;
                threads[i] = new Thread(() =>
                {
                    TestObj testObj = lazy.Value;
                    Console.WriteLine("Thread " + threadId +" returned value " + testObj.value);
                });

                threads[i].Start();
            }

            Thread thread1 = new Thread(()=>
            {
                Thread.Sleep(4000);
                TestObj testObj = lazy.Value;
                Console.WriteLine("Thread " + "Thread1" + " returned value " + testObj.value);
            });

            Thread thread2 = new Thread(() =>
            {
                TestObj testObj = lazy.Value;
                Console.WriteLine("Thread " + "thread2" + " returned value " + testObj.value);
            });

            
            

            for (int i = 0; i < nThreads; ++i)
                threads[i].Join();

            thread1.Start();
            thread1.Join();
            thread2.Start();
            thread2.Join();

            Console.ReadKey();
        }

        /*public static void Main()
        {
            TestExpirableLazy(); ;
        }*/
    }   
}
