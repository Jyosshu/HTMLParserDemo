using System.Collections.Generic;

namespace ClassLibrary
{
    public interface IDataAccess
    {
        List<Distributor> GetDistributorsFromDb();
    }
}