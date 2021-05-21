using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ClassLibrary
{
    public class DataAccess : IDataAccess
    {
        private readonly IConfiguration _config;
        private readonly ILogger<DataAccess> _log;
        private readonly string _connectionStringName = "SQLCONNSTR_AMdev";

        public DataAccess(IConfiguration config, ILogger<DataAccess> log)
        {
            _config = config;
            _log = log;
        }

        public List<Distributor> GetDistributorsFromDb()
        {
            try
            {
                string connstr = _config.GetConnectionString(_connectionStringName);
                using (IDbConnection connection = new SqlConnection(connstr))
                {
                    var results = connection.Query<Distributor>("SELECT * FROM Distributors ORDER BY DistributorId").ToList();
                    return results;
                }
            }
            catch (SqlException se)
            {
                _log.LogError(se.Message, se);
                return null;
            }
        }
    }
}
