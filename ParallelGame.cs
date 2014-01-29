using System;
using System.Collections.Generic;
using System.Threading;

namespace rhul
{
    /*
     * The program was only using 25% of processing power on my PC, so to use the other cores I made it multithreaded.
     * It runs about 2.5 times faster than singlethreaded on my quad core PC (about 500ms with the Console.WriteLine()'s commented)
     */
    public class ParallelGame : IDisposable
    {        
        private List<StartPos> startingPositions;   
        private volatile bool runThreads = true;
        private int nextItemIndex = 0;
        private object nextItemLock = new object(), scoreLock = new object();

        // these allow threads to wait for an event to occur
        private readonly ManualResetEvent startEvent = new ManualResetEvent(false);
        private readonly ManualResetEvent[] finishedEvents, runningEvents;

        // marked as 'volatile' because multiple threads will be reading these for comparisons and setting them, so a thread could get a stale cached value without this
        private volatile int highestScore, highestLoop;

        // these are only written to by the multiple threads. all writes are volatile in C#, so no need for the 'volatile' keyword (learnt this from here: http://igoro.com/archive/volatile-keyword-in-c-memory-model-explained/)
        private int highestLoopOccurrences = 0, highestScoreOccurrences = 0;
        private int[] highestScorePosition, highestLoopPosition;

        public ParallelGame(int threadCount)
        {
            this.finishedEvents = new ManualResetEvent[threadCount];
            this.runningEvents = new ManualResetEvent[threadCount];
            for (int threadId = 0; threadId < threadCount; threadId++)
            {
                this.finishedEvents[threadId] = new ManualResetEvent(false);
                this.runningEvents[threadId] = new ManualResetEvent(false);
                new Thread(this.Worker).Start(threadId);
            }
        }

        public void Play(List<StartPos> positions)
        {
            this.nextItemIndex = 0;
            this.highestLoop = 0;
            this.highestScore = 0;
            this.highestLoopOccurrences = 0;
            this.highestScoreOccurrences = 0;
            this.highestScorePosition = new int[0];
            this.highestLoopPosition = new int[0];
            this.startingPositions = positions;

            // signal the worker threads to start processing the positions
            this.startEvent.Set();

            // wait until all threads have started and are no longer waiting for startEvent
            WaitHandle.WaitAll(this.runningEvents);

            // now that they are all started, the starting event can be reset to prevent excessive looping in Worker()'s "while(this.run)"
            this.startEvent.Reset();

            // wait for all worker threads to finish processing the starting positions
            WaitHandle.WaitAll(this.finishedEvents);

            // reset the events
            for (int i = 0; i < this.finishedEvents.Length; i++)
            {
                this.finishedEvents[i].Reset();
                this.runningEvents[i].Reset();
            }
        }

        /*
         * Gets the next starting position from the list using locking to prevent a race condition
         */
        private StartPos GetNextItem()
        {
            StartPos item = null;

            // only allow one thread at a time to do this to prevent a possible race condition
            lock (this.nextItemLock)
            {
                if (this.nextItemIndex < this.startingPositions.Count)
                    item = this.startingPositions[this.nextItemIndex++];
            }

            return item;
        }

        /*
         * This method is run by the worker threads.
         */
        private void Worker(object objThreadId)
        {
            // the parameter passed in the Thread.Start() method to start the current thread is the threadId
            int threadId = (int)objThreadId;
            while (this.runThreads)
            {
                // causes the current thread to wait for this event to be signaled
                this.startEvent.WaitOne();

                // signals that the current thread has started
                this.runningEvents[threadId].Set();

                StartPos item = null;
                while ((item = this.GetNextItem()) != null)
                {
                    Game game = new Game(item);
                    game.Play();

                    // multiple threads could collide here without a lock
                    lock (this.scoreLock)
                    {
                        if (game.Score > this.highestScore)
                        {
                            this.highestScorePosition = item.position;
                            this.highestScore = game.Score;
                            this.highestScoreOccurrences = 1;
                        }
                        else if (game.Score == this.highestScore)
                            this.highestScoreOccurrences++;

                        if (game.Loop > this.highestLoop)
                        {
                            this.highestLoopPosition = item.position;
                            this.highestLoop = game.Loop;
                            this.highestLoopOccurrences = 1;
                        }
                        else if (game.Loop == this.highestLoop)
                            this.highestLoopOccurrences++;
                    }
                }

                // signals that the current thread has got null when calling GetNextItem(), so is finished
                this.finishedEvents[threadId].Set();
            }
        }

        public int HighestScore
        {
            get { return this.highestScore; }
        }

        public int HighestLoop
        {
            get { return this.highestLoop; }
        }

        public int[] HighestScorePosition
        {
            get { return this.highestScorePosition; }
        }

        public int[] HighestLoopPosition
        {
            get { return this.highestLoopPosition; }
        }

        public int HighestScoreOccurrences
        {
            get { return this.highestScoreOccurrences; }
        }

        public int HighestLoopOccurrences
        {
            get { return this.highestLoopOccurrences; }
        }

        #region IDisposable Members

        /*
         * This class implements the IDisposeable interface so the using(){} statement can be used to call this method to terminate the threads.
         */
        public void Dispose()
        {
            // worker threads will be waiting for the startEvent, so need to set it to release them
            this.startEvent.Set();

            // terminates the while(this.runThreads) loop on each worker thread
            this.runThreads = false;
        }

        #endregion
    }
}
