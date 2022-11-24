namespace AgdaLibraryLookup.Collections
{
    public interface ITraverseConstrainer<T>
    {
        /// <summary>
        /// Checks if the value being examined satisfies some constraints and updates inner state. 
        /// </summary>
        /// <param name="val"> The value being examined. </param>
        /// <returns> <see langword="true"/> if the value satisfies object constraints; <see langword="false"/> otherwise. </returns>
        public bool Examine(T val);
    }

    public sealed class NoopTraverseConstrainer<T> : ITraverseConstrainer<T>
    {
        public bool Examine(T _) => true; 
    }
}
