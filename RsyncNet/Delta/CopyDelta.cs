namespace RsyncNet.Delta
{
    public class CopyDelta : IDelta
    {
        #region IDelta Properties, indexers, events and operators

        public int Length { get; set; }
        public long Offset { get; set; }

        #endregion
    }
}