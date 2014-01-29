using System.Collections.Generic;

namespace rhul
{
    public class Game
    {
        private int[] currentPos;
        private int ones = 0, score, loop;
        // stores all previous positions, including the starting position, so the algorithm can detect a repeated position
        private List<int[]> prevPositions = new List<int[]>();

        public Game(StartPos startPos)
        {
            this.currentPos = startPos.position;
            this.ones = startPos.ones;
            this.prevPositions.Add(this.currentPos);
        }

        public void Play()
        {
            bool repeatedPosition = false;
            // performs a move on the current position until a repeated position occurs
            for (this.score = 0; !repeatedPosition; this.score++)
            {
                // perform a move on the current position
                this.PerformMove();

                int moveIndex = this.prevPositions.Count - 1;
                // loop backwards from the last position
                for (; !repeatedPosition && moveIndex >= 0; moveIndex--)
                {
                    int[] prevPos = this.prevPositions[moveIndex];

                    // only compare positions with equal length to the current move
                    if (prevPos.Length == this.currentPos.Length)
                    {
                        // compare the previous position with the current
                        bool matches = true;
                        for (int i = 0; matches && i < this.currentPos.Length; i++)
                            if (prevPos[i] != this.currentPos[i])
                                matches = false;

                        // there is a repeated position if they match
                        repeatedPosition = matches;
                    }
                }

                if (repeatedPosition)
                    this.loop = this.score - moveIndex;
                else
                    this.prevPositions.Add(this.currentPos);
            }
        }

        /*
         * Performs a move on the current position in the game. The algorithm works as follows:
         * 
         * 1) Subtracts one coin from each stack in the current position
         * 2) Creates a new stack from the coins subtracted, which is just the input position's size.
         * 3) Inserts the new stack at the correct location for the new position to be in descending order.
        */
        private void PerformMove()
        {
            // use the number of ones in the current position to calculate the exact size for the new position array
            int[] newPos = new int[this.currentPos.Length - this.ones + 1];
            int newPosIndex = 0;
            // reset the number of ones, as its next value will be determined by the new position created by this method
            this.ones = 0;

            if (this.currentPos.Length == 1)
                this.ones++;

            bool added = false;
            // add the stacks to the new position by subtracting one from each stack and insert the new stack at the correct location to maintain descending order
            for (int stackIndex = 0; stackIndex < this.currentPos.Length; stackIndex++)
            {
                int updatedStack = this.currentPos[stackIndex] - 1;
                if (updatedStack > 0)
                {
                    if (updatedStack == 1)
                        this.ones++;

                    // add the new stack, which is the length of the new position
                    if (!added && updatedStack <= this.currentPos.Length)
                    {
                        newPos[newPosIndex++] = this.currentPos.Length;
                        added = true;
                    }

                    newPos[newPosIndex++] = updatedStack;
                }
            }

            // if the new stack was too small to be addded above, it must go on the end
            if (!added)
                newPos[newPosIndex] = this.currentPos.Length;

            this.currentPos = newPos;
        }

        public int Score
        {
            get { return this.score; }
        }

        public int Loop
        {
            get { return this.loop; }
        }
    }
}
