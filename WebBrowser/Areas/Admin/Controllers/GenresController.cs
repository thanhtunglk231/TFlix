using CoreLib.Dtos.Genres;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class GenresController : Controller
    {
        private readonly IGenresService _genresService;

        public GenresController(IGenresService genresService)
        {
            _genresService = genresService;
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET: /Admin/Genres/GetAll
        public async Task<IActionResult> GetAll()
        {
            Console.WriteLine("[Admin/GenresController] -> GetAll() ENTER");
            var result = await _genresService.get_all();
            Console.WriteLine("[Admin/GenresController] <- GetAll() EXIT: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }

        // POST: /Admin/Genres/Add
        public async Task<IActionResult> Add([FromBody] AddGenreDto dto)
        {
            Console.WriteLine("[Admin/GenresController] -> Add() ENTER payload: " + JsonConvert.SerializeObject(dto));
            var result = await _genresService.add_Genre(dto);
            Console.WriteLine("[Admin/GenresController] <- Add() EXIT: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }

        // POST: /Admin/Genres/Update
        public async Task<IActionResult> Update([FromBody] UpdateGenreDto dto)
        {
            Console.WriteLine("[Admin/GenresController] -> Update() ENTER payload: " + JsonConvert.SerializeObject(dto));
            var result = await _genresService.update_Genre(dto);
            Console.WriteLine("[Admin/GenresController] <- Update() EXIT: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }

        // POST: /Admin/Genres/Delete?id=123
        public async Task<IActionResult> Delete([FromQuery] decimal id)
        {
            Console.WriteLine("[Admin/GenresController] -> Delete() ENTER, id=" + id);
            if (id <= 0) return BadRequest(new { message = "Invalid id." });

            var result = await _genresService.delete_Genre(id);
            Console.WriteLine("[Admin/GenresController] <- Delete() EXIT: " + JsonConvert.SerializeObject(result));
            return Ok(result);
        }
    }
}
