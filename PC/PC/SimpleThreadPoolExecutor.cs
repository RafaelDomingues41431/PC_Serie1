using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PC
{
    public class Runnable
    {

        public delegate void Command();
        public Command command;

        public Runnable()
        {

        }

        public Runnable(Command command)
        {
            this.command = command;
        }

        public void Run()
        {
            command.Invoke();
        }
    }

    public class Work
    {
        public Object workSignal;
        public Runnable command;
    }

    public class ThreadMapping
    {
        public Thread thread;
        public Runnable command;
        public Object threadSignal;
    }

    public class SimpleThreadPoolExecutor
    {
        long keepAliveTime;
        int maxPoolSize;
        bool shuttingDown;
        Thread terminationThread;

        LinkedList<ThreadMapping> standBy;
        LinkedList<ThreadMapping> working;
        LinkedList<Work> workQueue;

        Object executeSignal;
        Object listLock;
        Object terminationLock;

        public SimpleThreadPoolExecutor(int maxPoolSize, long keepAliveTime)
        {
            this.maxPoolSize = maxPoolSize;
            this.keepAliveTime = keepAliveTime;
            this.shuttingDown = false;
            standBy = new LinkedList<ThreadMapping>();
            working = new LinkedList<ThreadMapping>();
            workQueue = new LinkedList<Work>();
            listLock = new object();
            executeSignal = new object();
            terminationLock = new object();
        }

        public bool Execute(Runnable command, long timeout) {
            if (!Delegate(command))
            {
                Work work = new Work();
                work.command = command;
                work.workSignal = new object();
                lock (work.workSignal)
                {
                    try
                    {
                        lock (listLock)
                        {
                            workQueue.AddFirst(work);
                        }
                        if (Monitor.Wait(work.workSignal, (int)timeout))
                        {
                            if (shuttingDown)
                                throw new RejectedExecutionException();
                                return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    catch (ThreadInterruptedException)
                    {
                        throw new ThreadInterruptedException();
                    }
                }
            }
            else
                return true;
                   
        }

        public bool Delegate(Runnable command)
        {
            lock (listLock)
            {
                if (standBy.Count > 0)
                {
                    ThreadMapping mapping = standBy.Last();
                    standBy.Remove(mapping);
                    working.AddLast(mapping);
                    mapping.command = command;
                    lock (mapping.threadSignal)
                    {
                        Monitor.Pulse(mapping.threadSignal);
                    }
                    return true;
                }

                if (working.Count < maxPoolSize)
                {
                    ThreadMapping mapping = new ThreadMapping();
                    mapping.command = command;
                    mapping.threadSignal = new object();
                    mapping.thread = new Thread(() =>
                    {
                    while (true)
                    {
                            if (mapping.command != null)
                            {
                                mapping.command.Run();
                                mapping.command = null;
                                if (shuttingDown)
                                    return;
                                lock (listLock)
                                {
                                    if (workQueue.Count > 0)
                                    {
                                        Work work = workQueue.Last();
                                        workQueue.RemoveLast();
                                        mapping.command = work.command;
                                        lock (work.workSignal)
                                        {
                                            Monitor.Pulse(work.workSignal);
                                        }
                                        continue;
                                    }
                                    else
                                    {
                                        working.Remove(mapping);
                                        standBy.AddFirst(mapping);
                                    }
                                }
                            }
                            else
                            {
                                lock (mapping.threadSignal)
                                {
                                    try
                                    {
                                        if (Monitor.Wait(mapping.threadSignal, (int)keepAliveTime))
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            lock (listLock)
                                            {
                                                standBy.Remove(mapping);
                                            }
                                            return;
                                        }
                                    }
                                    catch (ThreadInterruptedException)
                                    {
                                        throw new ThreadInterruptedException();
                                    }
                                }
                            }
                        }
                    });
                    working.AddLast(mapping);
                    mapping.thread.Start();
                    return true;
                }
                return false;
            }
        }

        public void Shutdown()
        {
            shuttingDown = true;
            terminationThread = new Thread(()=>
            {
                lock (listLock)
                {
                    foreach (ThreadMapping threadMapping in standBy)
                    {
                        threadMapping.thread.Abort();
                    }
                }

                foreach (ThreadMapping threadMapping in working)
                {
                    threadMapping.thread.Join();
                }

                lock (terminationLock)
                {
                    Monitor.Pulse(terminationLock);
                }
            });
            terminationThread.Start();
        }

        public bool AwaitTermination(long timeout)
        {
            if (shuttingDown == true) {
                lock (terminationLock)
                {
                    Monitor.Wait(terminationLock, (int)timeout);
                }
                terminationThread.Join();
                return true;
            }else
                return false;
            
        }
    }

    [Serializable]
    internal class RejectedExecutionException : Exception
    {
        public RejectedExecutionException()
        {
        }

        public RejectedExecutionException(string message) : base(message)
        {
        }

        public RejectedExecutionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected RejectedExecutionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }






   /* public class Test
    {
        public static void Main(string[] args)
        {
            Test1();
        }

        public static void Test1()
        {
            SimpleThreadPoolExecutor executor = new SimpleThreadPoolExecutor(2, 10000);

            Runnable command = new Runnable();

            if (!executor.Execute(command, 0))
                Console.WriteLine("execution 1 failed!");
            if (!executor.Execute(command, 0))
                Console.WriteLine("execution 2 failed!");
            if (!executor.Execute(command, 0))
                Console.WriteLine("execution 3 failed!");
            if (!executor.Execute(command, Timeout.Infinite))
                Console.WriteLine("execution 4 failed!");

            executor.Shutdown();
            executor.AwaitTermination(Timeout.Infinite);
            Console.WriteLine("finished!");
            Console.ReadKey();
        }
    }*/
}
