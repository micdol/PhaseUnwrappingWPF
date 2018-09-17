using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PhaseUnwrapping
{
    public class Goldstein : BaseUnwrapper
    {
        /// <remarks>
        /// Flag definitions (and some extension methods for them) are provided in <see cref="BitFlags"/> static class
        /// </remarks>

        private static readonly bool DEBUG_OUTPUT_ON = false;

        /// <summary>
        /// Due to possible small numerical errors of floating point operations
        /// this specifies tolerance of two values which suppose to be equal
        /// </summary>
        protected static readonly double EPS = 1e-3;

        protected byte[,] mFlags;

        /// <summary>
        /// Wrapped phase
        /// </summary>
        public override double[,] Wrapped
        {
            get => base.Wrapped;
            set
            {
                base.Wrapped = value;

                // Re-init flags as well
                InitFlags();
            }
        }

        /// <summary>
        /// List of residues described as [row, col, charge (-1=negative, 1=positive)]
        /// </summary>
        public List<(int row, int col, sbyte charge)> Residues
        {
            // Return copy 
            get => new List<(int, int, sbyte)>(mResidues);
        }
        private List<(int row, int col, sbyte charge)> mResidues = new List<(int, int, sbyte)>();

        /// <summary>
        /// Branch cuts 
        /// </summary>
        public List<(int row, int col)> BranchCuts
        {
            // Return copy 
            get => new List<(int, int)>(mBranchCuts);
        }
        private List<(int row, int col)> mBranchCuts = new List<(int, int)>();

        public Goldstein(double[,] wrappedPhase) : base(wrappedPhase)
        {

        }

        /// <summary>
        /// Unwraps whole image
        /// </summary>
        public override void Unwrap()
        {
            // "New" avoid code, should avoid unwrapped pixels as well 
            // Unwrapping will be done by flood-fill Itoh-like method
            // First everything around branch cuts will be unwrapped and in the 
            // last step branch cuts will be unwrapped. 
            byte SKIP = (byte)(BitFlags.AVOID | BitFlags.UNWRAPPED);


        }

        /// <summary>
        /// Computes residues 
        /// </summary>
        /// <returns></returns>
        public int ComputeResidues()
        {
            // Residues will be recomputed, clear old ones
            mResidues.Clear();

            int numPositive = 0;
            int numNegative = 0;

            double integral = 0.0;

            for (int row = 0; row < mRows - 1; row++)
            {
                for (int col = 0; col < mCols - 1; col++)
                {
                    // Clear residue (either positive or negative) for this pixel
                    mFlags[row, col] &= (byte)(~BitFlags.RESIDUE);

                    if (mFlags[row, col].IsAvoid() ||
                        mFlags[row, col + 1].IsAvoid() ||
                        mFlags[row + 1, col].IsAvoid() ||
                        mFlags[row + 1, col + 1].IsAvoid())
                    {
                        continue;
                    }

                    // Sum of wrapped phase differences (~integal) around suspected residue (top left corner of rect)
                    //                     -----------------------------
                    // MARK RESIDUE HERE --|-> row, col |  row, col+1  |
                    //                     -------------X---------------
                    //                     | row+1, col | row+1, col+1 |
                    //                     -----------------------------
                    // But "real" position of the residue is "X", between the pixels
                    integral =
                        Utilities.Gradient(mWrapped[row, col + 1], mWrapped[row, col]) +
                        Utilities.Gradient(mWrapped[row, col], mWrapped[row + 1, col]) +
                        Utilities.Gradient(mWrapped[row + 1, col], mWrapped[row + 1, col + 1]) +
                        Utilities.Gradient(mWrapped[row + 1, col + 1], mWrapped[row, col + 1]);

                    // Summing (integration) around closed path is residue free if the result is equal 0!

                    // If the sum != ~0 mark as residue
                    if (integral > EPS)
                    {
                        Debug.WriteLineIf(DEBUG_OUTPUT_ON, string.Format("Positive residue @ [{0}, {1}] = {2}", row, col, integral));

                        mFlags[row, col] |= BitFlags.POSITIVE_RESIDUE;
                        mResidues.Add((row, col, 1));
                        numPositive++;
                    }
                    else if (integral < -EPS)
                    {
                        Debug.WriteLineIf(DEBUG_OUTPUT_ON, string.Format("Negative residue @ [{0}, {1}] = {2}", row, col, integral));

                        mFlags[row, col] |= BitFlags.NEGATIVE_RESIDUE;
                        mResidues.Add((row, col, -1));
                        numNegative++;
                    }
                }
            }

            Debug.WriteLineIf(DEBUG_OUTPUT_ON, string.Format("Number of residues: {0}, positive: {1}, negative: {2}", mResidues.Count, numPositive, numNegative));

            return mResidues.Count;
        }

        /// <summary>
        /// Computes branch cuts, it is required to have residues up-to-date, <seealso cref="ComputeResidues"/>
        /// </summary>
        public void ComputeBranchCuts()
        {
            mBranchCuts.Clear();

            int maxBoxSize = 1000000000;
            List<(int row, int col)> active = new List<(int row, int col)>();

            // Foreach unbalanced residue
            foreach (var residue in mResidues)
            {
                int row = residue.row;
                int col = residue.col;
                int charge = residue.charge;

                ref var pixel = ref mFlags[row, col];
                pixel |= BitFlags.ACTIVE;
                pixel |= BitFlags.VISITED;

                active.Add((row, col));

                // "Box" search for another residue around current resiude
                for (int n = 3; n < maxBoxSize; n += 2)
                {
                    // Half of the box (for iterating around)
                    int boxSize = n / 2;

                    // Foreach active pixel
                    for (int a = 0; a < active.Count; a++)
                    {
                        var boxCenter = active[a];

                        // Foreach pixel in box
                        for (int boxRow = boxCenter.row - boxSize;
                            boxRow < boxCenter.row + boxSize;
                            boxRow++)
                        {
                            for (int boxCol = boxCenter.col - boxSize;
                                boxCol < boxCenter.col + boxSize;
                                boxCol++)
                            {
                                // Out of bounds
                                if (boxRow < 0 || boxRow >= mRows || boxCol < 0 || boxCol >= mCols)
                                {
                                    continue;
                                }

                                ref byte boxPixel = ref mFlags[boxRow, boxCol];

                                // Box pixel = border pixel -> just place cut to the border
                                // it will "ground" the charge of the residues just like lightning rod 
                                if (boxPixel.IsBorder())
                                {
                                    charge = 0;
                                    PlaceBranchCut(row, col, boxRow, boxCol);
                                }
                                // Pixel which is inactive residue
                                else if (boxPixel.IsResidue() && !boxPixel.IsActive())
                                {
                                    // Not visited therefore not balanced
                                    if (!boxPixel.IsVisited())
                                    {
                                        charge += boxPixel.IsPositiveResidue() ? 1 : -1;
                                        boxPixel |= BitFlags.VISITED;
                                    }

                                    boxPixel |= BitFlags.ACTIVE;
                                    PlaceBranchCut(row, col, boxRow, boxCol);
                                }

                                // Charge is balanced - continue search
                                if (charge == 0)
                                {
                                    // goto suppouse to be evil but here its a nice way to break all those billions of loops above
                                    goto ContinueSearch;
                                }

                            }
                        }
                    }
                }

                // Box search finished but is the charge balanced?
                // If it isn't balanced place cut to the nearest border to balance it
                if (charge != 0)
                {

                }

                ContinueSearch:
                while (active.Count != 0)
                {
                    var px = active[0];
                    mFlags[px.row, px.col] &= (byte)(~BitFlags.ACTIVE);
                    active.RemoveAt(0);
                }

            }
        }

        /// <summary>
        /// Balances dipole residues. These are such that adjacent pixels are residues of opposite charge.
        /// During balancing such residues are removed and are replaced with branch cuts.
        /// </summary>
        /// <returns></returns>
        public int BalanceDipoles()
        {
            int numDipoles = 0;

            for (int row = 0; row < mRows; row++)
            {
                for (int col = 0; col < mCols; col++)
                {
                    int balanceRow = -1;
                    int balanceCol = -1;
                    // "Stepped on" positive residue
                    if (mFlags[row, col].IsPositiveResidue())
                    {
                        // Negative residue in the next col
                        if (col < mCols - 1 && mFlags[row, col + 1].IsNegativeResidue())
                        {
                            balanceRow = row;
                            balanceCol = col + 1;
                        }
                        // Negative residue in the next row
                        else if (row < mRows - 1 && mFlags[row + 1, col].IsNegativeResidue())
                        {
                            balanceRow = row + 1;
                            balanceCol = col;
                        }
                    }
                    // "Stepped on" negative residue
                    else if (mFlags[row, col].IsNegativeResidue())
                    {
                        // Positive residue in the next column
                        if (col < mCols - 1 && mFlags[row, col + 1].IsPositiveResidue())
                        {
                            balanceRow = row;
                            balanceCol = col + 1;
                        }
                        // Positive residue in the next row
                        else if (row < mRows - 1 && mFlags[row + 1, col].IsPositiveResidue())
                        {
                            balanceRow = row + 1;
                            balanceCol = col;
                        }
                    }

                    // Balancing residue was found
                    if (balanceCol > 0 && balanceRow > 0)
                    {
                        numDipoles++;

                        Debug.WriteLineIf(DEBUG_OUTPUT_ON, string.Format("Balancing dipole #{4} between [{0}, {1}] and [{2}, {3}]", row, col, balanceRow, balanceCol, numDipoles));

                        // Place cut...
                        PlaceBranchCut(row, col, balanceRow, balanceCol);

                        // ...and remove resiude & flag
                        mFlags[row, col] &= (byte)(~BitFlags.RESIDUE);
                        mFlags[balanceRow, balanceCol] &= (byte)(~BitFlags.RESIDUE);
                        int removed = mResidues.RemoveAll(x => (x.row == row && x.col == col) || (x.row == balanceRow && x.col == balanceCol));
                    }
                }
            }

            Debug.WriteLineIf(DEBUG_OUTPUT_ON, string.Format("Removed {0} residues, number of branch cuts generated: {1}", numDipoles * 2, BranchCuts.Count));

            return numDipoles;
        }

        /// <summary>
        /// Places a branch cut between resiude at A [<paramref name="srcRow"/>, <paramref name="srcCol"/>] 
        /// and B [<paramref name="dstRow"/>, <paramref name="dstCol"/>]. Updates flags by setting bit 
        /// <see cref="BRANCH_CUT"/> in <see cref="mFlags"/> for pixels which lay on the line between 
        /// those two points.
        /// </summary>
        /// <param name="srcRow"></param>
        /// <param name="srcCol"></param>
        /// <param name="dstRow"></param>
        /// <param name="dstCol"></param>
        private void PlaceBranchCut(int srcRow, int srcCol, int dstRow, int dstCol)
        {
            Debug.WriteLineIf(DEBUG_OUTPUT_ON, string.Format("Placing branch cut between [{0}, {1}] and [{2}, {3}] through pixels:", srcRow, srcCol, dstRow, dstCol));

            // Residue location in flags array is in the upper left corner of the four pixels surrounding it
            // however real postion is in the center of these pixels, while placing cut this need to be taken
            // into account as only real residues need to be connected -> therfore the inital move in cols/rows.
            // Easier seen when drawn :) 

            // Move in rows
            if (dstRow > srcRow && srcRow > 0) srcRow++;
            else if (dstRow < srcRow && dstRow > 0) dstRow++;

            // Move in cols
            if (dstCol > srcCol && srcCol > 0) srcCol++;
            else if (dstCol < srcCol && dstCol > 0) dstCol++;

            // It's the same pixel, update flag and return
            if (srcRow == dstRow && srcCol == dstCol)
            {
                Debug.WriteLineIf(DEBUG_OUTPUT_ON, string.Format("[{0}, {1}]", srcRow, srcCol));

                mFlags[srcRow, srcCol] |= BitFlags.BRANCH_CUT;
                mBranchCuts.Add((srcRow, srcCol));
                return;
            }

            // Not the same pixel - need to walk from A to B

            // Distances to walk in X (col), Y (row) direction
            int dRows = Math.Abs(srcRow - dstRow);
            int dCols = Math.Abs(srcCol - dstCol);

            // Treat walking as going on a line described as y = a x + b
            // Either step left/right or up/down by one

            // TODO Kind of like Bresenham line algo? note: bresenhm cant draw vertical lines
            // TODO OpenCV has LineIterator
            // TODO further analyze because now its just sort of mindlessly re-written 
            if (dRows > dCols)
            {
                int step = srcRow < dstRow ? 1 : -1;
                double a = (dstCol - srcCol) / (double)(dstRow - srcRow);
                for (int row = srcRow; row != dstRow + step; row += step)
                {
                    int col = (int)(srcCol + (row - srcRow) * a + 0.5);
                    mFlags[row, col] |= BitFlags.BRANCH_CUT;
                    mBranchCuts.Add((row, col));

                    Debug.WriteLineIf(DEBUG_OUTPUT_ON, string.Format("[{0}, {1}] ", row, col));
                }
            }
            else
            {
                int step = srcCol < dstCol ? 1 : -1;
                double a = (dstRow - srcRow) / (double)(dstCol - srcCol);
                for (int col = srcCol; col != dstCol + step; col += step)
                {
                    int row = (int)(srcRow + (col - srcCol) * a + 0.5);
                    mFlags[row, col] |= BitFlags.BRANCH_CUT;
                    mBranchCuts.Add((row, col));

                    Debug.WriteLineIf(DEBUG_OUTPUT_ON, string.Format("[{0}, {1}] ", row, col));
                }
            }

            Debug.WriteLineIf(DEBUG_OUTPUT_ON, "");
        }

        /// <summary>
        /// Initializes flags, marking border pixels
        /// </summary>
        private void InitFlags()
        {
            mFlags = new byte[mRows, mCols];

            // Mark border pixels
            Parallel.For(0, mRows, row =>
            {
                Parallel.For(0, mCols, col =>
                {
                    if (row == 0 || row == mRows - 1)
                    {
                        mFlags[row, col] |= BitFlags.BORDER;
                    }
                    else if (col == 0 || col == mCols - 1)
                    {
                        mFlags[row, col] |= BitFlags.BORDER;
                    }
                });
            });
        }
    }
}
