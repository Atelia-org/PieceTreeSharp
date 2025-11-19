using System;
using System.Collections.Generic;

namespace PieceTree.TextBuffer.Diff
{
    public class DiffComputer
    {
        public static DiffChange[] ComputeDiff(string original, string modified)
        {
            return ComputeDiff(original.ToCharArray(), modified.ToCharArray());
        }

        public static DiffChange[] ComputeDiff<T>(IList<T> original, IList<T> modified, IEqualityComparer<T>? comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;
            return new LcsDiff<T>(original, modified, comparer).ComputeDiff();
        }
    }

    internal class LcsDiff<T>
    {
        private readonly IList<T> _original;
        private readonly IList<T> _modified;
        private readonly IEqualityComparer<T> _comparer;

        public LcsDiff(IList<T> original, IList<T> modified, IEqualityComparer<T> comparer)
        {
            _original = original;
            _modified = modified;
            _comparer = comparer;
        }

        public DiffChange[] ComputeDiff()
        {
            return ComputeDiffRecursive(0, _original.Count - 1, 0, _modified.Count - 1);
        }

        private DiffChange[] ComputeDiffRecursive(int originalStart, int originalEnd, int modifiedStart, int modifiedEnd)
        {
            while (originalStart <= originalEnd && modifiedStart <= modifiedEnd && _comparer.Equals(_original[originalStart], _modified[modifiedStart]))
            {
                originalStart++;
                modifiedStart++;
            }

            while (originalEnd >= originalStart && modifiedEnd >= modifiedStart && _comparer.Equals(_original[originalEnd], _modified[modifiedEnd]))
            {
                originalEnd--;
                modifiedEnd--;
            }

            if (originalStart > originalEnd || modifiedStart > modifiedEnd)
            {
                if (modifiedStart <= modifiedEnd)
                {
                    return new[] { new DiffChange(originalStart, 0, modifiedStart, modifiedEnd - modifiedStart + 1) };
                }
                else if (originalStart <= originalEnd)
                {
                    return new[] { new DiffChange(originalStart, originalEnd - originalStart + 1, modifiedStart, 0) };
                }
                else
                {
                    return Array.Empty<DiffChange>();
                }
            }

            var (midOriginal, midModified) = FindMiddleSnake(originalStart, originalEnd, modifiedStart, modifiedEnd);

            var leftChanges = ComputeDiffRecursive(originalStart, midOriginal, modifiedStart, midModified);
            var rightChanges = ComputeDiffRecursive(midOriginal + 1, originalEnd, midModified + 1, modifiedEnd);

            return ConcatenateChanges(leftChanges, rightChanges);
        }

        private (int, int) FindMiddleSnake(int originalStart, int originalEnd, int modifiedStart, int modifiedEnd)
        {
            int n = originalEnd - originalStart + 1;
            int m = modifiedEnd - modifiedStart + 1;
            int delta = n - m;
            int max = n + m;
            
            int[] vf = new int[2 * max + 1];
            int[] vb = new int[2 * max + 1];
            
            for (int i = 0; i < vf.Length; i++) vf[i] = -1;
            for (int i = 0; i < vb.Length; i++) vb[i] = int.MaxValue;
            
            vf[max + 1] = 0;
            vb[max + delta + 1] = n + 1;

            bool deltaIsEven = (delta % 2 == 0);
            
            for (int d = 0; d <= (max + 1) / 2; d++)
            {
                // Forward
                for (int k = -d; k <= d; k += 2)
                {
                    int kIndex = max + k;
                    int x;
                    if (k == -d || (k != d && vf[kIndex - 1] < vf[kIndex + 1]))
                    {
                        x = vf[kIndex + 1];
                    }
                    else
                    {
                        x = vf[kIndex - 1] + 1;
                    }
                    
                    int y = x - k;
                    
                    while (x < n && y < m && _comparer.Equals(_original[originalStart + x], _modified[modifiedStart + y]))
                    {
                        x++;
                        y++;
                    }
                    
                    vf[kIndex] = x;
                    
                    if (!deltaIsEven && k >= delta - (d - 1) && k <= delta + (d - 1))
                    {
                        if (vf[kIndex] >= vb[kIndex])
                        {
                            return (originalStart + vf[kIndex] - 1, modifiedStart + (vf[kIndex] - k) - 1);
                        }
                    }
                }
                
                // Backward
                for (int k = -d; k <= d; k += 2)
                {
                    int kActual = k + delta;
                    int kIndex = max + kActual;
                    
                    int x;
                    if (k == -d || (k != d && vb[kIndex - 1] >= vb[kIndex + 1]))
                    {
                        x = vb[kIndex + 1] - 1;
                    }
                    else
                    {
                        x = vb[kIndex - 1];
                    }
                    
                    int y = x - kActual;
                    
                    while (x > 0 && y > 0 && _comparer.Equals(_original[originalStart + x - 1], _modified[modifiedStart + y - 1]))
                    {
                        x--;
                        y--;
                    }
                    
                    vb[kIndex] = x;
                    
                    if (deltaIsEven && kActual >= -d && kActual <= d)
                    {
                        if (vb[kIndex] <= vf[kIndex])
                        {
                             return (originalStart + vb[kIndex] - 1, modifiedStart + (vb[kIndex] - kActual) - 1);
                        }
                    }
                }
            }
            
            // Should not happen
            return (originalStart, modifiedStart);
        }

        private DiffChange[] ConcatenateChanges(DiffChange[] left, DiffChange[] right)
        {
            if (left.Length == 0) return right;
            if (right.Length == 0) return left;
            
            var lastLeft = left[left.Length - 1];
            var firstRight = right[0];
            
            if (lastLeft.OriginalEnd == firstRight.OriginalStart && lastLeft.ModifiedEnd == firstRight.ModifiedStart)
            {
                var merged = new DiffChange(
                    lastLeft.OriginalStart,
                    lastLeft.OriginalLength + firstRight.OriginalLength,
                    lastLeft.ModifiedStart,
                    lastLeft.ModifiedLength + firstRight.ModifiedLength
                );
                
                var result = new DiffChange[left.Length + right.Length - 1];
                Array.Copy(left, 0, result, 0, left.Length - 1);
                result[left.Length - 1] = merged;
                Array.Copy(right, 1, result, left.Length, right.Length - 1);
                return result;
            }
            
            var result2 = new DiffChange[left.Length + right.Length];
            Array.Copy(left, 0, result2, 0, left.Length);
            Array.Copy(right, 0, result2, left.Length, right.Length);
            return result2;
        }
    }
}
