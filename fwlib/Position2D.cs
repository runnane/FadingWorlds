namespace fwlib
{
    public class Position2D
    {
        public Position2D(int x_column, int y_row)
        {
            X = x_column;
            Y = y_row;
        }

        public int X { get; set; }
        public int Y { get; set; }

        public bool IsInvalid
        {
            get { return (X < 0 || Y < 0); }
        }
    }
}
