using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;

namespace Server.Classes
{
    public class DBContext: DbContext
    {
        public DbSet<Client> Clients { get; set; }
        /*Server=10.0.201.112;Database=base1_ISP_22_4_12;User=ISP_22_4_12;Pwd=7m4tIyDMeybp_;Trusted_Connection=false;*/
        public DBContext() : base(@"Server=10.0.201.112;Database=base1_ISP_22_4_12;User=ISP_22_4_12;Pwd=7m4tIyDMeybp_;Trusted_Connection=false;")
        {
            try
            {
                Database.SetInitializer(new MigrateDatabaseToLatestVersion<DBContext, Configuration>());
            }
            catch (System.Data.SqlClient.SqlException sqlEx)
            {
                Console.WriteLine(sqlEx.Message);
            }

        }

        internal sealed class Configuration : DbMigrationsConfiguration<DBContext>
        {
            public Configuration()
            {
                AutomaticMigrationsEnabled = true;
                AutomaticMigrationDataLossAllowed = true;
            }
        }

    }
}
