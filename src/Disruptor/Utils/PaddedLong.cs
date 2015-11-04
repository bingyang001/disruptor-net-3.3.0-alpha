namespace Disruptor.Utils
{
    /// <summary>
    /// Cache line padded long variable to be used when false sharing maybe an issue.
    /// </summary>
    //public class PaddedLong : MutableLong
    //{        
    //    public volatile long p1 = 7L;
    //    public volatile long p2 = 7L;
    //    public volatile long p3 = 7L;
    //    public volatile long p4 = 7L;
    //    public volatile long p5 = 7L;
    //    public volatile long p6 = 7L;
    //    /// <summary>
    //    /// Default constructor
    //    /// </summary>
    //    public PaddedLong()
    //    {
    //    }



    //    /// <summary>
    //    /// Construct with an initial value.
    //    /// </summary>
    //    /// <param name="initialValue">initialValue for construction</param>
    //    public PaddedLong(long initialValue)
    //        : base(initialValue)
    //    {

    //    }

    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    /// <returns></returns>
    //    public long sumPaddingToPreventOptimisation()
    //    {
    //        return p1 + p2 + p3 + p4 + p5 + p6;
    //    }
    //}
}
