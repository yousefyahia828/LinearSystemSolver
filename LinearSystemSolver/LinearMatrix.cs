// ReSharper disable CompareOfFloatsByEqualityOperator
namespace LinearSystemSolver;

internal class LinearMatrix
{
    private readonly double[,] _matrix;
    private readonly int _rows;
    private readonly int _cols;
    private int _timeOut;


    public event Action<string> OnStepCompleted;

    private LinearMatrix(double[,] matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        _rows = matrix.GetLength(0);
        _cols = matrix.GetLength(1);
        var count = _rows * _cols;

        _matrix = new double[_rows, _cols];

        Array.Copy(matrix, _matrix, count);
    }

    public static LinearMatrix Create(double[,] matrix)
    {
        return new LinearMatrix(matrix);
    }

    // Iterative solution
    private async Task ToRowEchelon()
    {
        int leadingCol = 0;

        for (int r = 0; r < _rows && leadingCol < _cols;)
        {
            // Try to find a row can contain a leading  
            int leadingRow = -1;
            for (int rr = r; rr < _rows; rr++)
            {
                if (_matrix[rr, leadingCol] != 0)
                {
                    leadingRow = rr;
                    break;
                }
            }

            if (leadingRow == -1)
            {
                await Task.Delay(_timeOut);
                OnStepCompleted?.Invoke($"Column {leadingCol + 1} can not contain a leading element\r\n");
                leadingCol++;
                continue;
            }

            if (leadingRow != r)
            {
                SwapRows(r, leadingRow);
                await Task.Delay(_timeOut);
                OnStepCompleted?.Invoke($"Swapping R{r + 1} with R{leadingRow + 1}\r\n");
            }


            // Normalize leading row to get leading  = 1
            if (_matrix[r, leadingCol] != 1)
            {
                double leadingValue = 1 / _matrix[r, leadingCol];

                for (int cc = leadingCol; cc < _cols; cc++)
                {
                    _matrix[r, cc] *= leadingValue;
                }

                await Task.Delay(_timeOut);
                OnStepCompleted?.Invoke($"Normalizing R{r + 1}\r\n");
                OnStepCompleted?.Invoke($"R{r + 1} = R{r + 1} * {Fix(leadingValue):0.##}\r\n\n");
            }
            // leading is ok


            if (r + 1 >= _rows)
                break;

            await Task.Delay(_timeOut);
            OnStepCompleted?.Invoke("Converting elements under the leading to zero\r\n");
            // Convert all elements below the leading to zero
            for (int rr = r + 1; rr < _rows; rr++)
            {
                double factor = _matrix[rr, leadingCol];

                if (factor == 0) continue;

                for (int cc = leadingCol; cc < _cols; cc++)
                {
                    _matrix[rr, cc] -= factor * _matrix[r, cc];
                }

                await Task.Delay(_timeOut);
                OnStepCompleted?.Invoke($"R{rr + 1} = R{rr + 1} - {Fix(factor):0.##}R{r + 1}\r\n");
            }

            r++;
            leadingCol++;

            OnStepCompleted?.Invoke("==============\r\n");
        }

        await Task.Delay(_timeOut);
        OnStepCompleted?.Invoke("\nMatrix has been converted to REF\r\n");
    }

    public async Task ToReducedRowEchelon()
    {
        if (!IsRowEchelon())
            await ToRowEchelon();

        await Task.Delay(_timeOut);
        OnStepCompleted?.Invoke("==============\r\n");
        OnStepCompleted?.Invoke("Converting to RREF\r\n");

        for (int r = 1; r < _rows; r++)
        {
            int leadingCol = -1;

            for (int c = 0; c < _cols; c++)
            {
                if (_matrix[r, c] == 1)
                {
                    leadingCol = c;
                    break;
                }
            }

            if (leadingCol == -1)
            {
                OnStepCompleted?.Invoke($"There is no leading element at R{r + 1}\r\n");
                continue;
            }

            for (int rr = r - 1; rr >= 0; rr--)
            {
                double factor = _matrix[rr, leadingCol];

                for (int c = leadingCol; c < _cols; c++)
                {
                    _matrix[rr, c] -= factor * _matrix[r, c];
                }

                await Task.Delay(_timeOut);
                OnStepCompleted?.Invoke($"R{rr + 1} = R{rr + 1} - {Fix(factor):0.##}R{r + 1}\r\n");
            }
        }
    }

    private bool IsRowEchelon()
    {
        int lastLeading = -1;

        for (int r = 0; r < _rows; r++)
        {
            int leadingCol = -1;

            for (int c = 0; c < _cols; c++)
            {
                if (_matrix[r, c] != 0)
                {
                    leadingCol = c;
                    break;
                }
            }

            if (leadingCol == -1) // zero row
            {
                for (int rr = r + 1; rr < _rows; rr++)
                {
                    for (int cc = 0; cc < _cols; cc++)
                    {
                        if (_matrix[rr, cc] != 0)
                        {
                            return false; // Non-zero row comes after zero row
                        }
                    }
                }

                // if you are here all the next rows are zero rows .
                // So , no need to check any other thing
                return true;
            }

            // Ensure that the leading is one
            if (_matrix[r, leadingCol] != 1)
                return false;

            // Ensure that the current leading element
            // is positioned at the right of the leading element at the above row
            if (leadingCol <= lastLeading)
                return false;

            lastLeading = leadingCol;
        }

        return true;
    }

    private bool IsReducedRowEchelon()
    {
        if (!IsRowEchelon())
            return false;

        for (int r = 0; r < _rows; r++)
        {
            int leadingCol = -1;

            for (int c = 0; c < _cols; c++)
            {
                if (_matrix[r, c] == 1)
                {
                    leadingCol = c;
                    break;
                }
            }

            if (leadingCol == -1)
                continue;

            for (int rr = r - 1; rr >= 0; rr--)
            {
                if (_matrix[rr, leadingCol] != 0)
                    return false;
            }
        }

        return true;
    }

    private void SwapRows(int row1, int row2)
    {
        for (int c = 0; c < _cols; c++)
        {
            // swap using tuple deconstruction
            (_matrix[row1, c], _matrix[row2, c]) = (_matrix[row2, c], _matrix[row1, c]);
        }
    }

    // Converts negative zero (-0) to a normal zero (0) so it is not displayed as "-0"
    private static double Fix(double value)
    {
        return Math.Round(value, 2) + 0.0;
    }

    public async Task SolveWithSteps(bool fastMood)
    {
        _timeOut = fastMood ? 0 : 300;

        OnStepCompleted?.Invoke("Solution Process started\r\n");

        if (!IsReducedRowEchelon())
        {
            await ToReducedRowEchelon();
            OnStepCompleted?.Invoke("\nMatrix has been converted to RREF\r\n");
        }

        await FireResult();
    }

    private async Task FireResult()
    {
        OnStepCompleted?.Invoke("\n==============\r\n");

        bool hasSolution = true;

        for (int r = 0; r < _rows; r++)
        {
            bool allZero = true;
            for (int c = 0; c < _cols - 1; c++)
            {
                if (_matrix[r, c] != 0)
                {
                    allZero = false;
                    break;
                }
            }

            if (allZero && _matrix[r, _cols - 1] != 0)
            {
                MessageBox.Show("Inconsistent system detected.\nNo solution exists.", "Invalid Operation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                OnStepCompleted?.Invoke("Inconsistent system detected.\r\n");
                OnStepCompleted?.Invoke("No solution exists.\r\n");
                hasSolution = false;
                break;
            }

            if (allZero && _matrix[r, _cols - 1] == 0)
            {
                MessageBox.Show("Infinite solutions", "Solution Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                OnStepCompleted?.Invoke("Infinite solutions\r\n");
                return;
            }
        }

        if (hasSolution)
        {
            for (int r = 0; r < _rows; r++)
            {
                OnStepCompleted?.Invoke($"X{r + 1} : {Fix(_matrix[r, _cols - 1]):0.##}\r\n");
                await Task.Delay(_timeOut);
            }

            OnStepCompleted?.Invoke("\n==============\r\n");
        }

        // Replace any negative zero in the matrix with a normal zero before previewing it
        for (int r = 0; r < _rows; r++)
        {
            for (int c = 0; c < _cols; c++)
            {
                if (_matrix[r, c] == 0)
                    _matrix[r, c] = 0.0;
            }
        }

        var form = new PreviewForm(_matrix);
        form.Show();
    }
}