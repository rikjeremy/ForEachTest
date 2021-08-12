using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ForEachTest
{
    class Program
    {
        static void Main(string[] args)
        {


            int maxThreads = 8;
            List<int> testSet = Enumerable.Range(1, 100).ToList();
            int testVal = testSet.Sum();
            Stopwatch stopwatch;

            using (FileStream fs = new FileStream(@"c:\test\paralleltest.txt", FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                using (StreamWriter writer = new StreamWriter(fs))
                {


                    // control loop
                    for (int x = 0; x <= 2000; x += 100)
                    {

                        Console.WriteLine($"Latency = {x}/2000ms");

                        int sum = 0;


                        if (x <= 300) //Single thread scales linearly... just putting it in for a couple to demonstrate
                        {

                            stopwatch = Stopwatch.StartNew();
                            // single thread
                            foreach (int i in testSet)
                            {
                                Interlocked.Add(ref sum, i);
                                Thread.Sleep(x);
                            }
                            stopwatch.Stop();

                            writer.WriteLine($"SINGLE THREAD ({x}ms) - {(sum == testVal ? "PASSED" : "FAILED")} - {sum} - {testVal} - {stopwatch.ElapsedMilliseconds:#,##0}");

                            sum = 0;

                        }
                        else
                        {
                            writer.WriteLine($"SINGLE THREAD ({x}ms) - SKIPPED");

                        }

                        stopwatch = Stopwatch.StartNew();
                        // parallel f-e
                        Parallel.ForEach(testSet, new ParallelOptions() { MaxDegreeOfParallelism = maxThreads }, i =>
                        {
                            Interlocked.Add(ref sum, i);
                            Thread.Sleep(x);
                        });
                        stopwatch.Stop();

                        writer.WriteLine($"PARALLEL F-E ({x}ms) - {(sum == testVal ? "PASSED" : "FAILED")} - {sum} - {testVal} - {stopwatch.ElapsedMilliseconds:#,##0}");

                        sum = 0;

                        SemaphoreSlim semaphore = new SemaphoreSlim(maxThreads);
                        stopwatch = Stopwatch.StartNew();
                        int refCt = 0;
                        // semaphore wrk threads
                        foreach (int i in testSet)
                        {
                            Debug.WriteLine(i);
                            semaphore.Wait();
                            Interlocked.Increment(ref refCt);
                            new Thread(() => Wrk(ref refCt, ref sum, i, x, semaphore)).Start();
                        }

                        while (refCt != 0)
                        {
                            Thread.Sleep(10);
                        }

                        stopwatch.Stop();

                        writer.WriteLine($"SEMAPHORE THREADS ({x}ms) - {(sum == testVal ? "PASSED" : "FAILED")} - {sum} - {testVal} - {stopwatch.ElapsedMilliseconds:#,##0}");

                        sum = 0;

                        semaphore = new SemaphoreSlim(maxThreads);
                        stopwatch = Stopwatch.StartNew();
                        refCt = 0;
                        // semaphore wrk tasks
                        foreach (int i in testSet)
                        {
                            Debug.WriteLine(i);
                            semaphore.Wait();
                            Interlocked.Increment(ref refCt);
                            Task.Run(() => Wrk(ref refCt, ref sum, i, x, semaphore));
                        }

                        while (refCt != 0)
                        {
                            Thread.Sleep(10);
                        }

                        stopwatch.Stop();

                        writer.WriteLine($"SEMAPHORE TASKS ({x}ms) - {(sum == testVal ? "PASSED" : "FAILED")} - {sum} - {testVal} - {stopwatch.ElapsedMilliseconds:#,##0}");

                        sum = 0;

                        stopwatch = Stopwatch.StartNew();
                        //PLINQ
                        testSet.AsParallel().WithDegreeOfParallelism(maxThreads).ForAll(i =>
                        {
                            Interlocked.Add(ref sum, i);
                            Thread.Sleep(x);
                        });
                        stopwatch.Stop();

                        writer.WriteLine($"PLINQ ({x}ms) - {(sum == testVal ? "PASSED" : "FAILED")} - {sum} - {testVal} - {stopwatch.ElapsedMilliseconds:#,##0}");

                        writer.Flush();

                    }

                    writer.WriteLine("FINISHED");


                }
            }

            Console.WriteLine("FINISHED - press any key to close...");
            Console.ReadKey();

        }

        private static void Wrk(ref int refCt, ref int sum, int i, int slp, SemaphoreSlim semaphore)
        {
            Interlocked.Add(ref sum, i);
            Thread.Sleep(slp);
            Interlocked.Decrement(ref refCt);
            semaphore.Release();
        }
    }
}
