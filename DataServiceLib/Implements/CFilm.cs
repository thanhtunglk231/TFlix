using CoreLib.Dtos;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataServiceLib.Implements
{
    public class CFilm : ICFilm
    {
        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public CFilm(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("OracleDb");
        }

        public CResponseMessage Get_Film_Detail(GetFilmDetail filmId)
        {
            try
            {
                // --- INPUTS ---
                var p_id = new OracleParameter("p_id", OracleDbType.Int32)
                {
                    Value = filmId.id, // truyền ID phim cần lấy
                    Direction = ParameterDirection.Input
                };

                var p_kind = new OracleParameter("p_kind", OracleDbType.Varchar2, 20)
                {
                    Value = filmId.genre ?? (object)DBNull.Value, // "MOVIE", "SERIES", "EPISODE" hoặc null
                    Direction = ParameterDirection.Input
                };

                // --- OUTPUTS ---
                var p_result = new OracleParameter("p_result", OracleDbType.RefCursor)
                {
                    Direction = ParameterDirection.Output
                };

                var p_code = new OracleParameter("p_code", OracleDbType.Int32)
                {
                    Direction = ParameterDirection.Output
                };

                var p_message = new OracleParameter("p_message", OracleDbType.NVarchar2, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                // Gộp tất cả tham số
                var parameters = new OracleParameter[]
                {
        p_id, p_kind, p_result, p_code, p_message
                };

                // Gọi stored procedure
                var dataset = _baseProvider.GetDatasetFromSP("SP_GET_CONTENT_BY_ID", parameters, _connectionString);

                // Lấy kết quả phản hồi
                return new CResponseMessage
                {
                    Data = dataset,
                    code = p_code.Value?.ToString() ?? "-1",
                    message = p_message.Value?.ToString() ?? "Không có phản hồi từ SP",
                    Success = p_code.Value?.ToString() == "1"
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage
                {
                    Success = false,
                    code = "500",
                    message = "Lỗi server: " + ex.Message
                };
            }

        }



    }
}
