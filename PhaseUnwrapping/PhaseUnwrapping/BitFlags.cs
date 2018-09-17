namespace PhaseUnwrapping
{
    /// <summary>
    /// Static class providing bit flags definition and helper extension methods for 
    /// setting and checkin bit flag code
    /// </summary>
    public static class BitFlags
    {
        /// Flag definitions, each flag should be corresponding to single distinct bit
        #region Flag Definitions 

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

        #region Extension Methods

        /// <summary>
        /// Sets <see cref="POSITIVE_RESIDUE"/> bit on <paramref name="b"/>
        /// </summary>
        /// <param name="b"></param>
        public static void MarkPositiveResidue(this byte b) => b |= POSITIVE_RESIDUE;

        /// <summary>
        /// Checks if <paramref name="b"/> has <see cref="POSITIVE_RESIDUE"/> bit set
        /// </summary>
        /// <param name="b"></param>
        public static bool IsPositiveResidue(this byte b) => (b & POSITIVE_RESIDUE) != 0;

        /// <summary>
        /// Sets <see cref="NEGATIVE_RESIDUE"/> bit on <paramref name="b"/>
        /// </summary>
        /// <param name="b"></param>
        public static void MarkNegativeResidue(this byte b) => b |= NEGATIVE_RESIDUE;

        /// <summary>
        /// Checks if <paramref name="b"/> has <see cref="NEGATIVE_RESIDUE"/> bit set
        /// </summary>
        /// <param name="b"></param>
        public static bool IsNegativeResidue(this byte b) => (b & NEGATIVE_RESIDUE) != 0;

        /// <summary>
        /// Checks if <paramref name="b"/> has either <see cref="POSITIVE_RESIDUE"/> or <see cref="NEGATIVE_RESIDUE"/> bit set
        /// </summary>
        /// <param name="b"></param>
        public static bool IsResidue(this byte b) => (b & RESIDUE) != 0;

        /// <summary>
        /// Clears bits <see cref="POSITIVE_RESIDUE"/> and <see cref="NEGATIVE_RESIDUE"/> on <paramref name="b"/>
        /// </summary>
        /// <param name="b"></param>
        public static void ClearResidue(this byte b) => b &= (byte)(~RESIDUE);

        /// <summary>
        /// Sets <see cref="VISITED"/> bit on <paramref name="b"/>
        /// </summary>
        /// <param name="b"></param>
        public static void MarkVisited(this byte b) => b |= VISITED;

        /// <summary>
        /// Checks if <paramref name="b"/> has <see cref="VISITED"/> bit set
        /// </summary>
        /// <param name="b"></param>
        public static bool IsVisited(this byte b) => (b & VISITED) != 0;

        /// <summary>
        /// Sets <see cref="ACTIVE"/> bit on <paramref name="b"/>
        /// </summary>
        /// <param name="b"></param>
        public static void MarkActive(this byte b) => b |= ACTIVE;

        /// <summary>
        /// Clears bit <see cref="ACTIVE"/> on <paramref name="b"/>
        /// </summary>
        /// <param name="b"></param>
        public static void ClearActive(this byte b) => b &= (byte)(~ACTIVE);

        /// <summary>
        /// Checks if <paramref name="b"/> has <see cref="ACTIVE"/> bit set
        /// </summary>
        /// <param name="b"></param>
        public static bool IsActive(this byte b) => (b & ACTIVE) != 0;

        /// <summary>
        /// Sets <see cref="BRANCH_CUT"/> bit on <paramref name="b"/>
        /// </summary>
        /// <param name="b"></param>
        public static void MarkBranchCut(this byte b) => b |= BRANCH_CUT;

        /// <summary>
        /// Checks if <paramref name="b"/> has <see cref="BRANCH_CUT"/> bit set
        /// </summary>
        /// <param name="b"></param>
        public static bool IsBranchCut(this byte b) => (b & BRANCH_CUT) != 0;

        /// <summary>
        /// Sets <see cref="BORDER"/> bit on <paramref name="b"/>
        /// </summary>
        /// <param name="b"></param>
        public static void MarkBorder(this byte b) => b |= BORDER;

        /// <summary>
        /// Checks if <paramref name="b"/> has <see cref="BORDER"/> bit set
        /// </summary>
        /// <param name="b"></param>
        public static bool IsBorder(this byte b) => (b & BORDER) != 0;

        /// <summary>
        /// Sets <see cref="UNWRAPPED"/> bit on <paramref name="b"/>
        /// </summary>
        /// <param name="b"></param>
        public static void MarkUnwrapped(this byte b) => b |= UNWRAPPED;

        /// <summary>
        /// Checks if <paramref name="b"/> has <see cref="POSTPONED"/> bit set
        /// </summary>
        /// <param name="b"></param>
        public static void MarkPostponed(this byte b) => b |= POSTPONED;

        /// <summary>
        /// Clears bit <see cref="POSTPONED"/> on <paramref name="b"/>
        /// </summary>
        /// <param name="b"></param>
        public static void ClearPostponed(this byte b) => b &= (byte)(~POSTPONED);

        /// <summary>
        /// Checks if <paramref name="b"/> has <see cref="AVOID"/> bit set
        /// </summary>
        /// <param name="b"></param>
        public static bool IsAvoid(this byte b) => (b & AVOID) != 0;

        /// <summary>
        /// Checks if <paramref name="b"/> has any of bits from <paramref name="code"/> set
        /// </summary>
        /// <param name="b"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        /// <remarks>
        /// code is of int type to avoid irritating (byte) cast if combination of flags is desired, eg.: 1 | 2 | 4 
        /// </remarks>
        public static bool Is(this byte b, int code) => (b & code) != 0;

        /// <summary>
        /// Sets bits from <paramref name="code"/> on b. Only 8 lowest bits from <paramref name="code"/> are taken into account
        /// </summary>
        /// <param name="b"></param>
        /// <param name="code"></param>
        /// <remarks>
        /// code is of int type to avoid irritating (byte) cast if combination of flags is desired, eg.: 1 | 2 | 4 
        /// </remarks>
        public static void Mark(this byte b, int code) => b |= (byte)code;

        #endregion


    }
}
