namespace EvenireDB
{
    //TODO: consider add versioning on events file        

    public enum Direction
    {
        Forward = 0,
        Backward = 1,
    }

    public enum StreamPosition : long
    {
        Start = 0,
        End = long.MaxValue
    }
}