namespace Opserver.Data
{
    public partial class PollNode
    {
        public bool Equals(PollNode other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.GetType() == GetType() && string.Equals(UniqueKey, other.UniqueKey);
        }

        public override int GetHashCode() => UniqueKey?.GetHashCode() ?? 0;

        public static bool operator ==(PollNode left, PollNode right) => Equals(left, right);

        public static bool operator !=(PollNode left, PollNode right) => !Equals(left, right);

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((PollNode)obj);
        }
    }
}
