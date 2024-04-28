using ApiHoteleria.Dtos;
using ApiHoteleria.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Net;

namespace ApiHoteleria.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HotelController : ControllerBase
    {
        [Authorize]
        [HttpPost]
        [Route("create")]
        public IActionResult Create([FromBody] Hotel hotel, [FromServices] MySqlConnection connection)
        {
            try
            {
                IActionResult response = Unauthorized();

                // valida si vienen los campos necesarios para la creacion del hotel
                if (hotel.Name== null || hotel.Address==null || hotel.Phone== null || hotel.Email==null)
                {
                    return StatusCode((int)HttpStatusCode.Forbidden,"Incomplete request");

                }

                // validacion para verificar si el email del hotel que se envia ya existe en la base de datos

                var existingHotel = connection.Query<string>("SELECT Email FROM hotel WHERE Email" +
                    "= @email", new { hotel.Email }).FirstOrDefault();

                // si ya existe, retorna un error
                if (existingHotel != null)
                {
                    return StatusCode((int)HttpStatusCode.NotFound, "Hotel email already exists!");
                }

                connection.Execute("INSERT INTO hotel(Name, Address, Phone, Email) VALUES(@name, @address, @phone, @email)", new { hotel.Name, hotel.Address, hotel.Phone, hotel.Email });

                response = Ok("Hotel created successfully!");

                return response;
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, "An error has ocurred: " + e.Message);
            }

        }

        [Authorize]
        [HttpPut]
        [Route("update")]
        public IActionResult Update([FromBody] Hotel hotel, [FromServices] MySqlConnection connection)
        {
            try
            {
                IActionResult response = Unauthorized();

                // valida si vienen los campos necesarios para la creacion del hotel
                if (hotel.Name == null || hotel.Address == null || hotel.Phone == null ||
                    hotel.Email == null || String.IsNullOrEmpty(hotel.Hotel_ID.ToString()))
                {
                    return StatusCode((int)HttpStatusCode.Forbidden, "Incomplete request");

                }

                // validacion para saber si el hotel existe

                var findHotel = connection.Query<string>("SELECT Hotel_ID FROM hotel WHERE Hotel_ID" +
          " = @hotel_id", new { hotel.Hotel_ID }).FirstOrDefault();


                // si no existe, retorna un error
                if (findHotel == null)
                {
                    return StatusCode((int)HttpStatusCode.NotFound, "Hotel does not exist!");
                }

                System.Diagnostics.Debug.WriteLine(findHotel.ToString());

                // validacion para verificar si el email del hotel que se envia ya existe en la base de datos

                var existingHotel = connection.Query<string>("SELECT Email FROM hotel WHERE Email" +
                    "= @email AND Hotel_ID <> @hotel_id", new { hotel.Email, hotel.Hotel_ID }).FirstOrDefault();

                // si ya existe, retorna un error
                if (existingHotel != null)
                {
                    return StatusCode((int)HttpStatusCode.NotFound, "Hotel email already exists!");
                }

                connection.Execute("UPDATE hotel SET Name = @name, Address = @address, Phone = @phone, Email = @email " +
                    "WHERE Hotel_ID = @hotel_id", new { hotel.Name, hotel.Address, hotel.Phone, hotel.Email, hotel.Hotel_ID });

                response = Ok("Hotel updated successfully!");

                return response;
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, "An error has ocurred: " + e.Message);
            }

        }

        [Authorize]
        [HttpGet]   
        [Route("get")]
        public IActionResult Get([FromServices] MySqlConnection connection)
        {
            try
            {
                IActionResult response = Unauthorized();

                var hotels = connection.Query<Hotel>("SELECT * FROM hotel" ).ToList();

                response = Ok(hotels);

                return response;
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, "An error has ocurred: " + e.Message);
            }

        }

    }
}
