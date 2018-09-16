using System;

namespace PhaseUnwrapping
{
    public static class Utilities
    {
        /// <summary>
        /// Wraps phase value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double Wrap(double value)
        {
            return Math.Atan2(Math.Sin(value), Math.Cos(value));
        }

        /// <summary>
        /// Computes gradient of wrapped phase between provided pixels/values.
        /// This function expects wrapped phase to run from -PI to PI
        /// </summary>
        /// <param name="wrappedPhase1"></param>
        /// <param name="wrappedPhase2"></param>
        /// <returns></returns>
        public static double Gradient(double wrappedPhase1, double wrappedPhase2)
        {
            double result = wrappedPhase1 - wrappedPhase2;
            if (result > Math.PI / 2)
            {
                result -= Math.PI;
            }
            else if (result < -Math.PI / 2)
            {
                result += Math.PI;
            }
            return result;
        }
    }
}
