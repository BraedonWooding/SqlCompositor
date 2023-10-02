namespace SqlCompositor.Core.Model
{
    public class TrackingInfo
    {
        public TrackingInfo(string memberName, int? lineNumber)
        {
            MemberName = memberName;
            LineNumber = lineNumber;
        }

        public string MemberName { get; }
        public int? LineNumber { get; }
    }
}