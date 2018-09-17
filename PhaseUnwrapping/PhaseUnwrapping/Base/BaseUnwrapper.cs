using System.Threading.Tasks;

namespace PhaseUnwrapping
{
    /// <summary>
    /// Base class for each phase unwrapping method/algorithm
    /// </summary>
    public abstract class BaseUnwrapper
    {
        #region Fields 

        /// <summary>
        /// Height in pixels, number of rows 
        /// </summary>
        protected int mRows;

        /// <summary>
        /// Width in pixels, number of columns
        /// </summary>
        protected int mCols;

        #endregion

        #region Properties 

        /// <summary>
        /// Unwrapped phase 
        /// </summary>
        public double[,] Unwrapped
        {
            // Return copy
            get => mUnwrapped.Clone() as double[,];
        }
        protected double[,] mUnwrapped;

        /// <summary>
        /// Wrapped phase
        /// </summary>
        public virtual double[,] Wrapped
        {
            // Return copy
            get => mWrapped.Clone() as double[,];
            set
            {
                // Deep copy of passed data
                mWrapped = value.Clone() as double[,];

                // Update size
                mRows = mWrapped.GetLength(0);
                mCols = mWrapped.GetLength(1);

                // Reset unwrapped as well
                mUnwrapped = new double[mRows, mCols];

                // Seed unwrapped value with the upper left pixel of wrapped phase
                mUnwrapped[0, 0] = mWrapped[0, 0];
            }
        }
        protected double[,] mWrapped;

        #endregion

        /// <summary>
        /// Initializes unwrapper instance with wrapped phase data
        /// </summary>
        /// <param name="wrappedPhase"></param>
        public BaseUnwrapper(double[,] wrappedPhase) => Wrapped = wrappedPhase;

        /// <summary>
        /// Unwraps whole image
        /// </summary>
        public abstract void Unwrap();
    }
}
