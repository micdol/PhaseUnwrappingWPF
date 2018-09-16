using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhaseUnwrapping
{
    class Goldstein : BaseUnwrapper
    {
        #region Flags Definitions

        public static readonly byte POSITIVE_RESIDUE = 0x01;
        public static readonly byte NEGATIVE_RESIDUE = 0x02;
        public static readonly byte VISITED = 0x04;
        public static readonly byte ACTIVE = 0x08;
        public static readonly byte BRANCH_CUT = 0x10;
        public static readonly byte BORDER = 0x20;
        public static readonly byte UNWRAPPED = 0x40;
        public static readonly byte POSTPONED = 0x80;
        public static readonly byte RESIDUE = (byte)(POSITIVE_RESIDUE | NEGATIVE_RESIDUE);
        public static readonly byte AVOID = (byte)(BORDER | BRANCH_CUT);

        #endregion

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
        /// Residues, positive are marked with 1.0 negative with -1.0
        /// </summary>
        public double[,] Residues
        {
            get
            {
                var res = new double[mRows, mCols];

                for (int row = 0; row < mRows - 1; row++)
                {
                    for (int col = 0; col < mCols - 1; col++)
                    {
                        if (mFlags[row, col] == POSITIVE_RESIDUE) res[row, col] = 1.0;
                        else if (mFlags[row, col] == NEGATIVE_RESIDUE) res[row, col] = -1.0;
                    }
                }

                return res;
            }
        }

        /// <summary>
        /// Branch cuts 
        /// </summary>
        public double[,] BranchCuts
        {
            get
            {
                var res = new double[mRows, mCols];

                for (int row = 0; row < mRows - 1; row++)
                {
                    for (int col = 0; col < mCols - 1; col++)
                    {
                        if (mFlags[row, col] == BRANCH_CUT) res[row, col] = 1.0;
                    }
                }

                return res;
            }
        }

        public Goldstein(double[,] wrappedPhase) : base(wrappedPhase)
        {

        }

        /// <summary>
        /// Unwraps whole image
        /// </summary>
        public override void Unwrap()
        {


        }

        /// <summary>
        /// Computes residues 
        /// </summary>
        /// <returns></returns>
        public int ComputeResidues()
        {
            int numPositive = 0;
            int numNegative = 0;

            double integral = 0.0;
            for (int row = 0; row < mRows - 1; row++)
            {
                for (int col = 0; col < mCols - 1; col++)
                {
                    // Clear residue (either positive or negative) for this pixel
                    mFlags[row, col] &= (byte)~RESIDUE;

                    if (((mFlags[row, col] & AVOID) |
                        (mFlags[row, col + 1] & AVOID) |
                        (mFlags[row + 1, col] & AVOID) |
                        (mFlags[row + 1, col + 1] & AVOID)) != 0)
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

                    // If the sum ~!= 0 mark as residue
                    if (integral > EPS)
                    {
                        Debug.WriteLine("Positive residue @ [{0}, {1}] = {2}", row, col, integral);
                        mFlags[row, col] = POSITIVE_RESIDUE;
                        numPositive++;
                    }
                    else if (integral < -EPS)
                    {
                        Debug.WriteLine("Negative residue @ [{0}, {1}] = {2}", row, col, integral);
                        mFlags[row, col] = NEGATIVE_RESIDUE;
                        numNegative++;
                    }
                }
            }

            Debug.WriteLine("Number of residues: {0}, positive: {1}, negative: {2}", numPositive + numNegative, numPositive, numNegative);

            return numPositive + numNegative;
        }


        /// <summary>
        /// Computes branch cuts, it is required to have residues up-to-date, <seealso cref="ComputeResidues"/>
        /// </summary>
        public void ComputeBranchCuts()
        {

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
                    if ((mFlags[row, col] & POSITIVE_RESIDUE) != 0)
                    {
                        // Negative residue in the next col
                        if (col < mCols - 1 && (mFlags[row, col + 1] & NEGATIVE_RESIDUE) != 0)
                        {
                            balanceRow = row;
                            balanceCol = col + 1;
                        }
                        // Negative residue in the next row
                        else if (row < mRows - 1 && (mFlags[row + 1, col] & NEGATIVE_RESIDUE) != 0)
                        {
                            balanceRow = row + 1;
                            balanceCol = col;
                        }
                    }
                    // "Stepped on" negative residue
                    if ((mFlags[row, col] & NEGATIVE_RESIDUE) != 0)
                    {
                        // Positive residue in the next column
                        if (col < mCols - 1 && (mFlags[row, col + 1] & POSITIVE_RESIDUE) != 0)
                        {
                            balanceRow = row;
                            balanceCol = col + 1;
                        }
                        // Positive residue in the next row
                        else if (row < mRows - 1 && (mFlags[row + 1, col] & POSITIVE_RESIDUE) != 0)
                        {
                            balanceRow = row + 1;
                            balanceCol = col;
                        }
                    }

                    // Balancing residue was found
                    if (balanceCol > 0 && balanceRow > 0)
                    {
                        numDipoles++;

                        Debug.WriteLine("Balancing dipole #{4} between [{0}, {1}] and [{2}, {3}]", row, col, balanceRow, balanceCol, numDipoles);

                        // Place cut...
                        PlaceBranchCut(row, col, balanceRow, balanceCol);

                        // ...and remove resiude flag
                        mFlags[row, col] &= (byte)~RESIDUE;
                        mFlags[balanceRow, balanceCol] &= (byte)~RESIDUE;
                    }

                }
            }

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
            // Residue location is in the upper left corner so we leave residue as it is and
            // move to the pixel next to it

            // Move in rows
            if (dstRow > srcRow && srcRow > 0) srcRow++;
            else if (dstRow < srcRow && dstRow > 0) dstRow++;

            // Move in cols
            if (dstCol > srcCol && srcCol > 0) srcCol++;
            else if (dstCol < srcCol && dstCol > 0) dstCol++;

            // It's the same pixel, update flag and return
            if (srcRow == dstRow && srcCol == dstCol)
            {
                mFlags[srcRow, srcCol] |= BRANCH_CUT;
                return;
            }

            // Not the same pixel - need to walk from A to B

            // Distances to walk in X (col), Y (row) direction
            int dRows = Math.Abs(srcRow - dstRow);
            int dCols = Math.Abs(srcCol - dstCol);

            // Treat walking as going on a line described as y = a x + b
            // Either step left/right or up/down by one

            // TODO analyze
            if (dRows > dCols)
            {
                int step = srcRow < dstRow ? 1 : -1;
                double a = (dstCol - srcCol) / (double)(dstRow - srcRow);
                for (int row = srcRow; row != dstRow + step; row += step)
                {
                    int col = (int)(srcCol + (row - srcRow) * a + 0.5);
                    mFlags[row, col] |= BRANCH_CUT;
                }

            }
            else
            {
                int step = srcCol < dstCol ? 1 : -1;
                double a = (dstRow - srcRow) / (double)(dstCol - srcCol);
                for (int col = srcCol; col != dstCol + step; col += step)
                {
                    int row = (int)(srcRow + (col - srcCol) * a + 0.5);
                    mFlags[row, col] |= BRANCH_CUT;
                }
            }
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
                        mFlags[row, col] |= BORDER;
                    }
                    else if (col == 0 || col == mCols - 1)
                    {
                        mFlags[row, col] |= BORDER;
                    }
                });
            });
        }
    }
}
