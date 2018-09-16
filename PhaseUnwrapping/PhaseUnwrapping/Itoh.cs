using System.Diagnostics;
using System.Threading.Tasks;

namespace PhaseUnwrapping
{
    public class Itoh : BaseUnwrapper
    {
        // Itoh algorithm is basically summing wrapped phase values
        // 1.   For row/col compute [wrapped] phase difference:
        //      foreach(pixel in row/col)
        //          diff[i] = mWrapped[i+1] - mWrapped[i]
        // 2.   Wrap the results of 1.
        //      foreach(value in diff)
        //          wrappedDiff[i] = Wrap(value)
        // 3.   Init first unwrapped phase value with for instance first wrapped pixel
        //      unwrapped[0] = wrapped[0]
        // 4.   Unwrap by summing wrapped phase diffs
        //      foreach(value in wrappedDiff)
        //          result[i] = result[i-1] + value

        /// <summary>
        /// Initializes Itoh unwrapper with wrapped phase data
        /// </summary>
        /// <param name="wrappedPhase"></param>
        public Itoh(double[,] wrappedPhase) : base(wrappedPhase) { }

        /// <summary>
        /// Unwraps whole image
        /// </summary>
        public override void Unwrap()
        {
            // First we unwrap first column then each row. Each unwrapped first column row value
            // will be a initial phase value for each of the rows
            UnwrapColumn(0);
            Parallel.For(0, mRows, row => UnwrapRow(row));

            // This can be also "reverted" - first row then columns basing on first row phase results
            // TODO Maybe test how this influence results depending on the nature of deformation in input image (namely direction of phase gradient)?

            //UnwrapRow(0);
            //Parallel.For(0, mCols, col => UnwrapColumn(col));
        }

        /// <summary>
        /// Unwraps single row
        /// </summary>
        /// <param name="row"></param>
        /// <param name="init">initial value, by default NaN meaning that theoretically init is already set in mUnwrapped array</param>
        private void UnwrapRow(int row, double init = double.NaN)
        {
            // Init if init value provided
            if (!double.IsNaN(init))
            {
                mUnwrapped[row, 0] = init;
            }

            for (int col = 1; col < mCols; col++)
            {
                double diff = mWrapped[row, col] - mWrapped[row, col - 1];
                double wrappedDiff = Utilities.Wrap(diff);
                mUnwrapped[row, col] = mUnwrapped[row, col - 1] + wrappedDiff;
            }
        }

        /// <summary>
        /// Unwraps single column
        /// </summary>
        /// <param name="col"></param>
        /// <param name="init">initial value, by default NaN meaning that theoretically init is already set in mUnwrapped array</param>
        private void UnwrapColumn(int col, double init = double.NaN)
        {
            // Init if init value provided
            if (!double.IsNaN(init))
            {
                mUnwrapped[0, col] = init;
            }

            for (int row = 1; row < mRows; row++)
            {
                double diff = mWrapped[row - 1, col] - mWrapped[row, col];
                double wrappedDiff = Utilities.Wrap(diff);
                mUnwrapped[row, col] = mUnwrapped[row - 1, col] + wrappedDiff;
            }
        }
    }
}
