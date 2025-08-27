using CoreLib.Models;
using DataServiceLib.Interfaces;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataServiceLib.Implements
{
    public class CBaseProvider : ICBaseProvider
    {

        private OracleConnection _connection;
        private OracleCommand _command;
        private OracleDataAdapter _adapter;


        public CBaseProvider()
        {

        }

        public bool OpenConnection(string connectionString)
        {
            try
            {
                if (_connection == null || _connection.State != ConnectionState.Open)
                {
                    _connection = new OracleConnection(connectionString);
                    _connection.Open();
                    Console.WriteLine("Opened Oracle connection.");
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex + "Failed to open Oracle connection.");

                return false;
            }
        }

        public void CloseConnection()
        {
            try
            {
                if (_connection?.State == ConnectionState.Open)
                {
                    _connection.Close();
                    _connection.Dispose();
                    _command?.Dispose();
                    _connection = null;
                    Console.WriteLine("Closed Oracle connection.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex + "Failed to close Oracle connection.");
                //WriteToFile(ex);
            }
        }

        // 1. Trả về DataSet
        public DataSet GetDatasetFromSP(string spName, IDbDataParameter[] parameters, string connectionString)
        {
            var dataset = new DataSet();
            if (!OpenConnection(connectionString))
                return dataset;

            try
            {
                _command = new OracleCommand(spName, _connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                if (parameters != null)
                {
                    foreach (var param in parameters)
                        _command.Parameters.Add(param);
                }

                _adapter = new OracleDataAdapter(_command);
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
                _adapter?.Dispose();
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
            return dt.Rows.Count > 0 ? dt.Rows[0] : dt.NewRow();
        }


        public bool ExecuteSP(string spName, IDbDataParameter[] parameters, string connectionString)
        {
            bool success = false;
            if (!OpenConnection(connectionString))
                return false;

            try
            {
                _command = new OracleCommand(spName, _connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                if (parameters != null)
                {
                    foreach (var param in parameters)
                        _command.Parameters.Add(param);
                }

                success = _command.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                _command?.Dispose();
                CloseConnection();
            }

            return success;
        }
        public (DataRow Row, CResponseMessage Response) GetDataRowAndResponseFromSP(string spName, IDbDataParameter[] parameters, string connectionString)
        {
            var code = new OracleParameter("o_code", OracleDbType.Varchar2, 10)
            {
                Direction = ParameterDirection.Output
            };
            var message = new OracleParameter("o_message", OracleDbType.Varchar2, 200)
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
                message = message.Value?.ToString()
            };

            return (row, response);
        }


        public CResponseMessage GetResponseFromExecutedSP(string spName, IDbDataParameter[] parameters, string connectionString)
        {
            var code = new OracleParameter("o_code", OracleDbType.Varchar2)
            {
                Size = 10,
                Direction = ParameterDirection.Output,
                Value = DBNull.Value
            };

            var message = new OracleParameter("o_message", OracleDbType.Varchar2)
            {
                Size = 200,
                Direction = ParameterDirection.Output,
                Value = DBNull.Value
            };

            var paramList = parameters?.ToList() ?? new List<IDbDataParameter>();
            paramList.Add(code);
            paramList.Add(message);

            ExecuteSP(spName, paramList.ToArray(), connectionString);

            return new CResponseMessage
            {
                code = code.Value?.ToString(),
                message = message.Value?.ToString()
            };
        }
        public (DataSet Data, CResponseMessage Response) GetDatasetAndResponseFromSP(string spName, IDbDataParameter[] parameters, string connectionString)
        {
            var code = new OracleParameter("o_code", OracleDbType.Varchar2, 10)
            {
                Direction = ParameterDirection.Output,
                Value = DBNull.Value
            };

            var message = new OracleParameter("o_message", OracleDbType.Varchar2, 200)
            {
                Direction = ParameterDirection.Output,
                Value = DBNull.Value
            };

            var paramList = parameters?.ToList() ?? new List<IDbDataParameter>();
            paramList.Add(code);
            paramList.Add(message);

            var dataset = GetDatasetFromSP(spName, paramList.ToArray(), connectionString);

            var response = new CResponseMessage
            {
                code = code.Value?.ToString(),
                message = message.Value?.ToString()
            };

            return (dataset, response);
        }
    }
}
