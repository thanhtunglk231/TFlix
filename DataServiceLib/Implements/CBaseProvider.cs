using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DataServiceLib.Implements
{
    public class CBaseProvider : ICBaseProvider
    {
        private SqlConnection _connection;
        private SqlCommand _command;
        private SqlDataAdapter _adapter;

        public CBaseProvider()
        {
        }

        public bool OpenConnection(string connectionString)
        {
            try
            {
                if (_connection == null || _connection.State != ConnectionState.Open)
                {
                    _connection = new SqlConnection(connectionString);
                    _connection.Open();
                    Console.WriteLine("Opened SQL Server connection.");
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex + " Failed to open SQL Server connection.");
                return false;
            }
        }

        public void CloseConnection()
        {
            try
            {
                if (_connection != null)
                {
                    if (_connection.State == ConnectionState.Open)
                    {
                        _connection.Close();
                    }

                    _connection.Dispose();
                    _connection = null;
                }

                _command?.Dispose();
                _command = null;

                _adapter?.Dispose();
                _adapter = null;

                Console.WriteLine("Closed SQL Server connection.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex + " Failed to close SQL Server connection.");
            }
        }

        public DataSet GetDatasetFromSP(string spName, IDbDataParameter[] parameters, string connectionString)
        {
            var dataset = new DataSet();

            if (!OpenConnection(connectionString))
                return dataset;

            try
            {
                _command = new SqlCommand(spName, _connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        if (param is SqlParameter sqlParam)
                            _command.Parameters.Add(sqlParam);
                    }
                }

                _adapter = new SqlDataAdapter(_command);
                _adapter.Fill(dataset);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                dataset = new DataSet();
            }
            finally
            {
                _command?.Dispose();
                _command = null;

                _adapter?.Dispose();
                _adapter = null;

                CloseConnection();
            }

            return dataset;
        }

        public DataTable GetDataTableFromSP(string spName, IDbDataParameter[] parameters, string connectionString)
        {
            var ds = GetDatasetFromSP(spName, parameters, connectionString);
            return ds.Tables.Count > 0 ? ds.Tables[0] : new DataTable();
        }

        public DataRow GetDataRowFromSP(string spName, IDbDataParameter[] parameters, string connectionString)
        {
            var dt = GetDataTableFromSP(spName, parameters, connectionString);
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public bool ExecuteSP(string spName, IDbDataParameter[] parameters, string connectionString)
        {
            bool success = false;

            if (!OpenConnection(connectionString))
                return false;

            try
            {
                _command = new SqlCommand(spName, _connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        if (param is SqlParameter sqlParam)
                            _command.Parameters.Add(sqlParam);
                    }
                }

                _command.ExecuteNonQuery();
                success = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                _command?.Dispose();
                _command = null;

                CloseConnection();
            }

            return success;
        }

        public (DataRow Row, CResponseMessage Response) GetDataRowAndResponseFromSP(string spName, IDbDataParameter[] parameters, string connectionString)
        {
            var code = new SqlParameter("@o_code", SqlDbType.VarChar, 10)
            {
                Direction = ParameterDirection.Output
            };

            var message = new SqlParameter("@o_message", SqlDbType.NVarChar, 200)
            {
                Direction = ParameterDirection.Output
            };

            var paramList = parameters?.ToList() ?? new List<IDbDataParameter>();
            paramList.Add(code);
            paramList.Add(message);

            var dt = GetDataTableFromSP(spName, paramList.ToArray(), connectionString);
            var row = dt.Rows.Count > 0 ? dt.Rows[0] : null;

            var response = new CResponseMessage
            {
                code = code.Value?.ToString(),
                message = message.Value?.ToString(),
                Success = code.Value?.ToString() == "1"
            };

            return (row, response);
        }

        public CResponseMessage GetResponseFromExecutedSP(string spName, IDbDataParameter[] parameters, string connectionString)
        {
            var code = new SqlParameter("@o_code", SqlDbType.VarChar, 10)
            {
                Direction = ParameterDirection.Output
            };

            var message = new SqlParameter("@o_message", SqlDbType.NVarChar, 200)
            {
                Direction = ParameterDirection.Output
            };

            var paramList = parameters?.ToList() ?? new List<IDbDataParameter>();
            paramList.Add(code);
            paramList.Add(message);

            ExecuteSP(spName, paramList.ToArray(), connectionString);

            return new CResponseMessage
            {
                code = code.Value?.ToString(),
                message = message.Value?.ToString(),
                Success = code.Value?.ToString() == "1"
            };
        }

        public (DataSet Data, CResponseMessage Response) GetDatasetAndResponseFromSP(string spName, IDbDataParameter[] parameters, string connectionString)
        {
            var code = new SqlParameter("@o_code", SqlDbType.VarChar, 10)
            {
                Direction = ParameterDirection.Output
            };

            var message = new SqlParameter("@o_message", SqlDbType.NVarChar, 200)
            {
                Direction = ParameterDirection.Output
            };

            var paramList = parameters?.ToList() ?? new List<IDbDataParameter>();
            paramList.Add(code);
            paramList.Add(message);

            var dataset = GetDatasetFromSP(spName, paramList.ToArray(), connectionString);

            var response = new CResponseMessage
            {
                code = code.Value?.ToString(),
                message = message.Value?.ToString(),
                Success = code.Value?.ToString() == "1"
            };

            return (dataset, response);
        }
    }
}