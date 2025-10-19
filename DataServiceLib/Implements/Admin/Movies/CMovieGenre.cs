using CoreLib.Dtos.MovieAsset;
using CoreLib.Dtos.MovieGenre;
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

namespace DataServiceLib.Implements.Admin.Movies
{
    public class CMovieGenre : ICMovieGenre
    {

        private readonly ICBaseProvider _baseProvider;
        private readonly string _connectionString;

        public CMovieGenre(ICBaseProvider baseProvider, IConfiguration configuration)
        {
            _baseProvider = baseProvider;
            _connectionString = configuration.GetConnectionString("OracleDb");
        }

        // ---------- GET ALL ----------
        //public async Task<CResponseMessage> sp_get_all()
        //{
        //    try
        //    {
        //        var o_cursor = new OracleParameter("o_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };
        //        var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
        //        var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };

        //        var ds = _baseProvider.GetDatasetFromSP("sp_movie_genre_get_all",
        //            new[] { o_cursor, o_code, o_message }, _connectionString);

        //        return new CResponseMessage
        //        {
        //            Data = ds,
        //            code = o_code.Value?.ToString() ?? "500",
        //            message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
        //            Success = (o_code.Value?.ToString() == "200")
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        return new CResponseMessage { Success = false, code = "500", message = "Lỗi server: " + ex.Message };
        //    }
        //}

        // ---------- GET BY PK (movie_id + genre_id) ----------
        public async Task<CResponseMessage> sp_get_by_pk(decimal movieId, decimal genreId)
        {
            try
            {
                var p_movie_id = new OracleParameter("p_movie_id", OracleDbType.Decimal, movieId, ParameterDirection.Input);
                var p_genre_id = new OracleParameter("p_genre_id", OracleDbType.Decimal, genreId, ParameterDirection.Input);

                var o_cursor = new OracleParameter("o_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };
                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };

                var ds = _baseProvider.GetDatasetFromSP("sp_movie_genre_get_by_pk",
                    new[] { p_movie_id, p_genre_id, o_cursor, o_code, o_message }, _connectionString);

                return new CResponseMessage
                {
                    Data = ds,
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = o_code.Value?.ToString() == "200"
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage { Success = false, code = "500", message = "Lỗi server: " + ex.Message };
            }
        }

        // ---------- GET BY MOVIE (lấy danh sách genre của 1 movie) ----------
        public async Task<CResponseMessage> sp_get_by_movie(decimal movieId)
        {
            try
            {
                var p_movie_id = new OracleParameter("p_movie_id", OracleDbType.Decimal, movieId, ParameterDirection.Input);
                var o_cursor = new OracleParameter("o_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };
                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };

                var ds = _baseProvider.GetDatasetFromSP("sp_movie_genre_get_by_movie",
                    new[] { p_movie_id, o_cursor, o_code, o_message }, _connectionString);

                return new CResponseMessage
                {
                    Data = ds,
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = o_code.Value?.ToString() == "200"
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage { Success = false, code = "500", message = "Lỗi server: " + ex.Message };
            }
        }

        // ---------- GET BY GENRE (lấy danh sách movie thuộc 1 genre) ----------
        public async Task<CResponseMessage> sp_get_by_genre(decimal genreId)
        {
            try
            {
                var p_genre_id = new OracleParameter("p_genre_id", OracleDbType.Decimal, genreId, ParameterDirection.Input);
                var o_cursor = new OracleParameter("o_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };
                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };

                var ds = _baseProvider.GetDatasetFromSP("sp_movie_genre_get_by_genre",
                    new[] { p_genre_id, o_cursor, o_code, o_message }, _connectionString);

                return new CResponseMessage
                {
                    Data = ds,
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = o_code.Value?.ToString() == "200"
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage { Success = false, code = "500", message = "Lỗi server: " + ex.Message };
            }
        }

        // ---------- ADD ----------
        public async Task<CResponseMessage> sp_add(AddMovieGenreDto addMovieAssetDto)
        {
            try
            {
                var p_movie_id = new OracleParameter("p_movie_id", OracleDbType.Decimal, addMovieAssetDto.MovieId, ParameterDirection.Input);
                var p_genre_id = new OracleParameter("p_genre_id", OracleDbType.Decimal, addMovieAssetDto.GenreId, ParameterDirection.Input);

                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };

                var ds = _baseProvider.GetDatasetFromSP("sp_movie_genre_add",
                    new[] { p_movie_id, p_genre_id, o_code, o_message }, _connectionString);

                return new CResponseMessage
                {
                    Data = ds,
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = o_code.Value?.ToString() == "200"
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage { Success = false, code = "500", message = "Lỗi server: " + ex.Message };
            }
        }

        // ---------- UPDATE (đổi từ oldGenreId sang newGenreId cho 1 movie) ----------
        public async Task<CResponseMessage> sp_update(UpdateMovieGenreDto updateMovieGenreDto)
        {
            try
            {
                var p_movie_id = new OracleParameter("p_movie_id", OracleDbType.Decimal, updateMovieGenreDto.MovieId, ParameterDirection.Input);
                var p_old_genre_id = new OracleParameter("p_old_genre_id", OracleDbType.Decimal, updateMovieGenreDto.OldGenreId, ParameterDirection.Input);
                var p_new_genre_id = new OracleParameter("p_new_genre_id", OracleDbType.Decimal, updateMovieGenreDto.NewGenreId, ParameterDirection.Input);

                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };

                var ds = _baseProvider.GetDatasetFromSP("sp_movie_genre_update",
                    new[] { p_movie_id, p_old_genre_id, p_new_genre_id, o_code, o_message }, _connectionString);

                return new CResponseMessage
                {
                    Data = ds,
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = o_code.Value?.ToString() == "200"
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage { Success = false, code = "500", message = "Lỗi server: " + ex.Message };
            }
        }

        // ---------- DELETE ----------
        public async Task<CResponseMessage> sp_delete(DeleteMovieGenreDto deleteMovieGenreDto)
        {
            try
            {
                var p_movie_id = new OracleParameter("p_movie_id", OracleDbType.Decimal, deleteMovieGenreDto.MovieId, ParameterDirection.Input);
                var p_genre_id = new OracleParameter("p_genre_id", OracleDbType.Decimal, deleteMovieGenreDto.GenreId, ParameterDirection.Input);

                var o_code = new OracleParameter("o_code", OracleDbType.Varchar2, 10) { Direction = ParameterDirection.Output };
                var o_message = new OracleParameter("o_message", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };

                var ds = _baseProvider.GetDatasetFromSP("sp_movie_genre_delete",
                    new[] { p_movie_id, p_genre_id, o_code, o_message }, _connectionString);

                return new CResponseMessage
                {
                    Data = ds,
                    code = o_code.Value?.ToString() ?? "500",
                    message = o_message.Value?.ToString() ?? "Không lấy được phản hồi",
                    Success = o_code.Value?.ToString() == "200"
                };
            }
            catch (Exception ex)
            {
                return new CResponseMessage { Success = false, code = "500", message = "Lỗi server: " + ex.Message };
            }
        }
    }
}
