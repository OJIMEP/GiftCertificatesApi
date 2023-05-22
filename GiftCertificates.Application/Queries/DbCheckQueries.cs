namespace GiftCertificates.Application.Queries
{
    public static class DbCheckQueries
    {
        public const string DatabaseBalancingReplicaFull = @"select datediff(ms, last_commit_time, getdate())
			from [master].[sys].[dm_hadr_database_replica_states]";

        public const string DatabaseBalancingMain = @"select top (1) _IDRRef from dbo._Reference112";

        public const string DatabaseBalancingReplicaTables = @"Select TOP(1) _IDRRef FROM dbo._Reference99";
    }
}
