using CoreLib.Dtos;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using RestSharp;
using System.Data;
using System.Data.SqlClient;

namespace DataServiceLib.Implements
{
    public class CFilm : ICFilm
    {
        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public CFilm(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("SqlServer");
        }

        public CResponseMessage Get_Film_Detail(GetFilmDetail filmId)
        {
            try
            {
                var p_id = new System.Data.SqlClient.SqlParameter("@p_id", SqlDbType.Int)
                {
                    Value = filmId.id,
                    Direction = ParameterDirection.Input
                };

                var p_kind = new System.Data.SqlClient.SqlParameter("@p_kind", SqlDbType.NVarChar, 20)
                {
                    Value = string.IsNullOrWhiteSpace(filmId.genre) ? DBNull.Value : filmId.genre,
                    Direction = ParameterDirection.Input
                };

                var p_code = new System.Data.SqlClient.SqlParameter("@p_code", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };

                var p_message = new System.Data.SqlClient.SqlParameter("@p_message", SqlDbType.NVarChar, 4000)
                {
                    Direction = ParameterDirection.Output
                };

                var parameters = new IDbDataParameter[]
                {
                    p_id, p_kind, p_code, p_message
                };

                var dataset = _baseProvider.GetDatasetFromSP("SP_GET_CONTENT_BY_ID", parameters, _connectionString);

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