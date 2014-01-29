
namespace rhul
{
    /*
     * Represents a starting position.
     * The number of ones in the position is stored because it can be used to calculate the size of arrays when playing the game,
     * avoiding slower and less efficient dynamic structures.
     */
    public class StartPos
    {
        public int[] position;
        public int ones;
    }
}
