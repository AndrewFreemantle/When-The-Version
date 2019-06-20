namespace WhenTheVersion
{
    public class RevisionInfo
    {
        public RevisionInfo(int revisionNumber, int nextRevisionNumber, string errorIfAny = null)
        {
            RevisionNumber = revisionNumber;
            NextRevisionNumber = nextRevisionNumber;
            ErrorIfAny = errorIfAny;
        }

        public int RevisionNumber { get; }
        public int NextRevisionNumber { get; }
        public string ErrorIfAny { get; set; }
        public bool Succeed => string.IsNullOrWhiteSpace(ErrorIfAny);

        public override string ToString() => Succeed ? $"RevisionNumber: {RevisionNumber}, NextRevisionNumber: {NextRevisionNumber}" : ErrorIfAny;
    }
}
