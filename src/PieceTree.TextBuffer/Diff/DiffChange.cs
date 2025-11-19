namespace PieceTree.TextBuffer.Diff
{
    public struct DiffChange
    {
        public int OriginalStart;
        public int OriginalLength;
        public int ModifiedStart;
        public int ModifiedLength;

        public int OriginalEnd => OriginalStart + OriginalLength;
        public int ModifiedEnd => ModifiedStart + ModifiedLength;

        public DiffChange(int originalStart, int originalLength, int modifiedStart, int modifiedLength)
        {
            OriginalStart = originalStart;
            OriginalLength = originalLength;
            ModifiedStart = modifiedStart;
            ModifiedLength = modifiedLength;
        }
    }
}
