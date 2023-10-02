namespace SqlCompositor.Core.Model
{
    public sealed class SqlParameters
    {
        public List<object> Parameters { get; }

        public SqlParameters(List<object>? parameters = null)
        {
            Parameters = parameters ?? new();
        }

        public string Value(object param)
        {
            // try to re-use parameters if we have an exact equality
            var index = Parameters.IndexOf(param);
            if (index == -1)
            {
                index = Parameters.Count;
                Parameters.Add(param);
            }

            return $"@p{index}";
        }

        //public IEnumerable<SqlParameter> ToSqlParameters()
        //{
        //    return Parameters.Select((p, i) => new SqlParameter($"@p{i}", p ?? DBNull.Value));
        //}
    }
}