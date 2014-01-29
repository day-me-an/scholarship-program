using System;
using System.Collections.Generic;
using System.Linq;

namespace rhul
{
    class Program
    {
        const int CoinsToCalculate = 36;

        // stores all starting positions for each number of coins calculated by GenerateStartingPositons(). the algorithm requires all previous coin positions to function.
        static readonly List<StartPos>[] positionCache = new List<StartPos>[CoinsToCalculate - 2];

        static void Main(string[] args)
        {
            //VerifyStartingPositions(CoinsToCalculate);
            PlayGames(CoinsToCalculate);
        }

        /*
         * Plays the game on every combination for each number of coins.
         * Outputs the highest score, loop and the combination they occur at for each number of coins.
         */
        static void PlayGames(int maxCoins)
        {
            DateTime start = DateTime.Now;

            // use the number of processors/cores as the number of threads to use for the algorithm
            using (ParallelGame pgame = new ParallelGame(Environment.ProcessorCount))
            {
                Console.WriteLine(Environment.ProcessorCount);
                for (int coins = 1; coins <= maxCoins; coins++)
                {
                    List<StartPos> startingPositions = GenerateStartingPositions(coins);
                    pgame.Play(startingPositions);

                    // the question said output *any* position for highest loop/score, so it only shows one (which can vary due to the multiple threads).
                    // comment these out for more accurate algorithm execution time
                    Console.WriteLine("Coins={0}", coins);
                    Console.WriteLine(" Highest Score is {0} (shared by {1}/{2} positions) and first found at [{3}]",
                        pgame.HighestScore,
                        pgame.HighestScoreOccurrences,
                        startingPositions.Count,
                        String.Join(",", Array.ConvertAll<int, string>(pgame.HighestScorePosition, Convert.ToString)));
                    Console.WriteLine(" Highest Loop is {0} (shared by {1}/{2} positions) and first found at [{3}]",
                        pgame.HighestLoop,
                        pgame.HighestLoopOccurrences,
                        startingPositions.Count,
                        String.Join(",", Array.ConvertAll<int, string>(pgame.HighestLoopPosition, Convert.ToString)));
                    Console.WriteLine();
                }
            }

            Console.WriteLine("execution time {0}ms", (DateTime.Now - start).TotalMilliseconds);
            Console.ReadKey();
        }

        /*
         * Generates all the possible distinct starting positions in descending order for the inputted number of coins.
         * By "distinct" and "descending order" I mean when calculating the positions for 6 coins: [3,2,1] would be an acceptable combination and the following would not: [1,2,3], [2,1,3], [2,3,1], [1,3,2] and [3,1,2].
         * 
         * 1) The algoritm splits the inputted number of coins into pairs of stacks named 'left' and 'right', which all sum to the inputted number of coins.
         * 2) This process initially starts by placing all but one of the coins in the left stack, and the remaining coin in the right stack.
         * 3) The algorithm repeatedly moves a coin from the left to the right stack, on each repetition a new pair is formed and the following is done:
         *      i)  add it to the 'positions' list.
         *      ii) look up the left stack of the new pair in the position cache (otherwise a lot of recursion would be needed). with each position found in the cache:
         *          I) replace the current pair's left stack with the cached positions
         *          II) add to the positions list.
         * 4) (3) is repeated until moving another coin would cause the right stack to be greater than the left.
         * 5) Return the positions list
         * 
         * Limitation: all previous coin combinations are required to be in the cache, so the algorithm can't calculate '36' without having previously calculated 2-35.
         * 
         * Future improvement: to calculate higher coin numbers it could use the hard disk instead of RAM to store the cached combinations.
         * 
         * Example input/output: 
         * input=2 output=[[2], [1,1]]
         * input=3 output=[[3], [2,1], [1,1,1]]
         * input=4 output=[[4], [3,1], [2,1,1], [1,1,1,1], [2,2]]
         */
        static List<StartPos> GenerateStartingPositions(int coins)
        {
            // stores the positions generated and returned by this algorithm.
            // ideally this should be an array with size calculated mathematically, but i couldn't work out how
            List<StartPos> positions = new List<StartPos>();

            // add an initial stack containing all the coins
            StartPos initPos = new StartPos();
            initPos.position = new int[] { coins };
            initPos.ones = (coins == 1) ? 1 : 0;
            positions.Add(initPos);

            // only coins greater than one can be split into sub positions
            if (coins > 1)
            {
                // loop through pairs of stacks which sum to the number of coins
                for (int stackLeft = coins - 1, stackRight = 1;
                    stackLeft >= stackRight;
                    stackLeft--, stackRight++)
                {
                    // add each pair to the `positions` list
                    StartPos pairPos = new StartPos();
                    pairPos.position = new int[] { stackLeft, stackRight };
                    if (stackLeft == 1)
                        pairPos.ones++;
                    if (stackRight == 1)
                        pairPos.ones++;
                    positions.Add(pairPos);

                    // there are different ways of expressing `leftStack` if it's greater than one
                    if (stackLeft > 1)
                    {
                        // look up ways of arranging the `leftStack` coins from the position cache
                        List<StartPos> cachedPositions = positionCache[stackLeft - 2];

                        // index starts from 1 to skip the initial position containing all coins, as it would cause duplicates
                        for (int i = 1; i < cachedPositions.Count; i++)
                        {
                            StartPos cachedPos = cachedPositions[i];
                            // only use positions that will maintain the descending order
                            if (stackRight <= cachedPos.position[cachedPos.position.Length - 1])
                            {
                                StartPos newPos = new StartPos();
                                newPos.position = new int[cachedPos.position.Length + 1];
                                newPos.ones = cachedPos.ones;
                                if (stackRight == 1)
                                    newPos.ones++;

                                // combine `stackRight` and the cached position to form a new position, which has a sum equal to `coins`
                                Array.Copy(cachedPos.position, newPos.position, cachedPos.position.Length);
                                newPos.position[newPos.position.Length - 1] = stackRight;
                                positions.Add(newPos);
                            }
                        }
                    }
                }

                // only add the positions for this number of coins to the position cache if the next number of coins will be calculated
                if (coins < CoinsToCalculate)
                    positionCache[coins - 2] = positions;
            }

            return positions;
        }

        /*
         * A quick debugging method to test if the GenerateStartingPositions() algorithm is calculating all possible combinations.
         * It creates 100,000 random combinations for each number of coins and checks if each combination is present in the list returned by GenerateStartingPositons() for that number of coins. 
         */
        static bool VerifyStartingPositions(int maxCoins)
        {
            Random rand = new Random();
            bool verified = true;

            for (int coins = 2; coins <= maxCoins; coins++)
            {
                List<StartPos> algoCombs = GenerateStartingPositions(coins);

                // verify each algo position's sum eqals this number of coins
                if (!algoCombs.TrueForAll((StartPos algoComb) => algoComb.position.Sum() == coins))
                {
                    // a breakpoint would be here
                    Console.WriteLine("algo comb for {0} has incorrect sum", coins);
                    verified = false;
                }

                List<int> randComb = new List<int>();

                for (int i = 0; i < 100000; i++)
                {
                    int total = 0;
                    do
                    {
                        int randc = rand.Next(1, coins - total);
                        total += randc;
                        randComb.Add(randc);
                    } while (total < coins);

                    // not efficient, but this is only a quick test
                    randComb.Sort();
                    randComb.Reverse();

                    bool found = false;
                    foreach (StartPos algoComb in algoCombs)
                    {
                        bool allMatch = true;
                        if (algoComb.position.Length == randComb.Count)
                        {
                            for (int j = 0; allMatch && j < algoComb.position.Length; j++)
                                if (algoComb.position[j] != randComb[j])
                                    allMatch = false;
                        }
                        else
                            allMatch = false;

                        if (allMatch)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        // a breakpoint would be here
                        Console.WriteLine("comb for {0} was not found", coins);
                        verified = false;
                    }

                    randComb.Clear();
                }
            }

            return verified;
        }
    }
}