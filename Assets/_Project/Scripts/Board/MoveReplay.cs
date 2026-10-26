using System.Collections.Generic;

namespace Flowers.Board
{
    /// <summary>
    /// Applies a cascade step's falls (or a shuffle's moves) to anything laid out like the board, e.g. the
    /// view's flower objects. All moves happen at once: a flower's destination can be another moving
    /// flower's start, so moving them one by one would overwrite flowers that haven't moved yet.
    /// </summary>
    public static class MoveReplay
    {
        /// <summary>
        /// Moves every item from move.From to move.To at the same time. Cells that are only left become
        /// default (null). buffer must be the same size as cells; its contents are overwritten.
        /// </summary>
        public static void Apply<T>(T[,] cells, IReadOnlyList<FlowerMove> moves, T[,] buffer)
        {
            for (int i = 0; i < moves.Count; i++)
            {
                var from = moves[i].From;
                buffer[from.X, from.Y] = cells[from.X, from.Y];
            }
            for (int i = 0; i < moves.Count; i++)
            {
                var from = moves[i].From;
                cells[from.X, from.Y] = default;
            }
            for (int i = 0; i < moves.Count; i++)
            {
                var m = moves[i];
                cells[m.To.X, m.To.Y] = buffer[m.From.X, m.From.Y];
            }
        }
    }
}
