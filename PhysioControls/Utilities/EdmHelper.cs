using System.Data.EntityClient;

namespace PhysioControls.Utilities
{
    public static class EdmHelper
    {
        #region Methods

        public static EntityConnection CreateSqlCeConnection(string fileName)
        {
            var sqlCeConnectionString = string.Format("Data Source={0}", fileName);
            var builder = new EntityConnectionStringBuilder
            {
                // regarding how to specify the metadata
                // http://stackoverflow.com/questions/8971698/entity-framework-unable-to-load-the-specified-metadata-resource
                Metadata = "res://*/",
                Provider = "System.Data.SqlServerCe.4.0",
                ProviderConnectionString = sqlCeConnectionString
            };
            var edmConnectionString = builder.ToString();
            var edmConnection = new EntityConnection(edmConnectionString);
            return edmConnection;
        }

        #endregion
    }
}
