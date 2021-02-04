using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PC
{
    public class Completion
    {
        Object lck;

        public Completion()
        {
            lck = new object();
        }

        public void Complete()
        {
            lock (lck)
            {
                Monitor.Pulse(lck);
            }
        }

        public Boolean WaitForCompletion(long millisTimeout)
        {
            lock (lck)
            {
                try
                {
                    if (Monitor.Wait(lck, (int)millisTimeout))
                        return true;
                    else
                        return false;
                }
                catch (ThreadInterruptedException)
                {
                    throw new ThreadInterruptedException();
                }
            }
        }

        public void CompleteAll()
        {
            lock (lck)
            {
                Monitor.PulseAll(lck);
            }
        }
    }







    public class TestCompletion
    {
        public static void Test1() //test Complete
        {
            int nThreads = 4;
            Completion completion = new Completion();
            Thread[] threads = new Thread[nThreads];

            for (int i = 0; i < nThreads; i++)
            {
                int threadId = i;
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        Console.WriteLine("Test1:Thread " + threadId + " started");
                        if (completion.WaitForCompletion(Timeout.Infinite))
                            Console.WriteLine("Test1:Thread " + threadId + " WaitForCompletion exited successfully.");
                        else
                            Console.WriteLine("Test1:Thread " + threadId + " WaitForCompletion timed out.");
                    }
                    catch (ThreadInterruptedException)
                    {
                        Console.WriteLine("Test1:Thread " + threadId + " interrupted.");
                    }
                });
                threads[i].Start();
            }

            Thread releaseThread = new Thread(()=>{
                for (int i = 0; i < nThreads; i++)
                {
                    while (threads[i].ThreadState == ThreadState.Running)
                        Thread.Sleep(1000);

                    while (threads[i].ThreadState == ThreadState.WaitSleepJoin)
                    {
                        Thread.Sleep(1000);
                        completion.Complete();
                        Console.WriteLine("Complete called.");
                    }
                }
            });

            releaseThread.Start();

            for(int i = 0; i < nThreads; i++){
                if(threads[i] != null)
                    threads[i].Join();
            }

            releaseThread.Join();

        }

        public static void Test2() //test CompleteAll
        {
            int nThreads = 10;
            Completion completion = new Completion();
            Thread[] threads = new Thread[10];
            for(int i = 0; i < nThreads - 1; i++)
            {
                int threadId = i;
                threads[i] = new Thread(() =>
                  {
                      try
                      {
                          Console.WriteLine("Test2:Thread "+threadId+" started");
                          if (completion.WaitForCompletion(Timeout.Infinite))
                              Console.WriteLine("Test2:Thread "+threadId+" WaitForCompletion exited successfully.");
                          else
                              Console.WriteLine("Test2:Thread "+threadId+" WaitForCompletion timed out.");
                      }
                      catch(ThreadInterruptedException) {
                          Console.WriteLine("Test2:Thread "+threadId+" interrupted.");
                      }
                 });
                threads[i].Start();
            }

            Thread releaseThread = new Thread(() =>
            {
                for (int i = 0; i < nThreads; i++)
                {
                    if (threads[i] == null)
                        continue;
                    while (threads[i].ThreadState == ThreadState.Running)
                        Thread.Sleep(1000);

                    while (threads[i].ThreadState == ThreadState.WaitSleepJoin)
                    {
                        Thread.Sleep(1000);
                        completion.CompleteAll();
                        Console.WriteLine("Test2:CompleteAll called.");
                    }
                    
                }
            });
            releaseThread.Start();

            for(int i = 0; i<nThreads; i++)
            {
                if(threads[i]!=null)
                    threads[i].Join();
            }

            releaseThread.Join();

        }

        public static void Test3() //test timeout
        {
            Completion completion = new Completion();
            Thread thread = new Thread(() =>
            {
                try
                {
                    Console.WriteLine("Test3:Thread started");
                    if (completion.WaitForCompletion(10000))
                        Console.WriteLine("Test3:Thread WaitForCompletion exited successfully.");
                    else
                        Console.WriteLine("Test3:Thread WaitForCompletion timed out.");
                }
                catch (ThreadInterruptedException)
                {
                    Console.WriteLine("Test3:Thread interrupted.");
                }
            });
            thread.Start();
            thread.Join();
        }

        /*public static void Main(string[] args)
        {
            Test2();
            Console.ReadKey();
        }*/
    }
}


