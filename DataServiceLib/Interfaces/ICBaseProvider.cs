using CoreLib.Models;
using System.Data;

namespace DataServiceLib.Interfaces
{
    public interface ICBaseProvider
    {
        void CloseConnection();
        bool ExecuteSP(string spName, IDbDataParameter[] parameters, string connectionString);
        (DataRow Row, CResponseMessage Response) GetDataRowAndResponseFromSP(string spName, IDbDataParameter[] parameters, string connectionString);
        DataRow GetDataRowFromSP(string spName, IDbDataParameter[] parameters, string connectionString);
        (DataSet Data, CResponseMessage Response) GetDatasetAndResponseFromSP(string spName, IDbDataParameter[] parameters, string connectionString);
        DataSet GetDatasetFromSP(string spName, IDbDataParameter[] parameters, string connectionString);
        DataTable GetDataTableFromSP(string spName, IDbDataParameter[] parameters, string connectionString);
        CResponseMessage GetResponseFromExecutedSP(string spName, IDbDataParameter[] parameters, string connectionString);
        bool OpenConnection(string connectionString);
    }
}