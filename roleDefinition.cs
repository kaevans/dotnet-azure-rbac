namespace graph_pim_dotnet
{
    public class RoleDefintion
    {
        public Properties properties { get; set; }
    }

    public class Properties
    {
        public string roleDefinitionId { get; set; }
        public string principalId { get; set; }
    }
}