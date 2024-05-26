using ApiHoteleria.Dtos;
using ApiHoteleria.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using MySqlConnector;
using System.IdentityModel.Tokens.Jwt;
using System.Net;

namespace ApiHoteleria.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HotelController : ControllerBase
    {

        private string getClientIdFromToken(string token)
        {

            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(token);
            var tokenS = jsonToken as JwtSecurityToken;
            var claims = tokenS.Claims.Select(claim => (claim.Type, claim.Value)).ToList();
            string userId = "";
            for (int i = 0; i < claims.Count; i++)
            {
                if (claims[i].Type == "sub")
                {
                    userId = claims[i].Value;
                }
            }
            System.Diagnostics.Debug.WriteLine("EL ID DEL TOKEN ES " + userId);
            return userId;
        }

        [Authorize]
        [HttpPost]
        [Route("create")]
        public IActionResult Create([FromBody] Hotel hotel, [FromServices] MySqlConnection connection)
        {
           string message = "Hotel created successfully!";
            int statusCode = (int)HttpStatusCode.OK;
            try
            {

                IActionResult response = Unauthorized();


                var authorization = Request.Headers[HeaderNames.Authorization];

                string clientId = getClientIdFromToken(authorization.ToString().Replace("Bearer ", ""));

                if (clientId == null)
                {
                    statusCode = (int)HttpStatusCode.Forbidden;
                    message = "Invalid token";
                    return StatusCode((int)HttpStatusCode.Forbidden, new { statusCode, message });
                }

                var user = connection.Query<Users>("SELECT * FROM user WHERE User_ID = @user_id AND Role = 'Super Admin'", new { user_id = clientId }).FirstOrDefault();

                if (user == null)
                {
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = "User not found";
                    return StatusCode((int)HttpStatusCode.NotFound, new { statusCode, message });
                }

                // valida si vienen los campos necesarios para la creacion del hotel
                if (hotel.Name== null || hotel.Address==null || hotel.Phone== null || hotel.Email==null)
                {
                    statusCode = (int)HttpStatusCode.Forbidden;
                    message = "Incomplete request";
                    return StatusCode((int)HttpStatusCode.Forbidden,new {statusCode, message});

                }

                // validacion para verificar si el email del hotel que se envia ya existe en la base de datos

                var existingHotel = connection.Query<string>("SELECT Email FROM hotel WHERE Email" +
                    "= @email", new { hotel.Email }).FirstOrDefault();

                // si ya existe, retorna un error
                if (existingHotel != null)
                {
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = "Hotel email already exists!";
                    return StatusCode((int)HttpStatusCode.NotFound,new {statusCode, message});
                }

                connection.Execute("INSERT INTO hotel(Name, Address, Phone, Email) VALUES(@name, @address, @phone, @email)", new { hotel.Name, hotel.Address, hotel.Phone, hotel.Email });

                response = Ok(new {statusCode, message});

                return response;
            }
            catch (Exception e)
            {

                message = "An error has ocurred: " + e.Message;
                statusCode = (int)HttpStatusCode.InternalServerError;
                return StatusCode((int)HttpStatusCode.InternalServerError, new {statusCode, message});
            }

        }

        [Authorize]
        [HttpPut]
        [Route("update")]
        public IActionResult Update([FromBody] Hotel hotel, [FromServices] MySqlConnection connection)
        {
            string message = "Hotel updated successfully!";
            int statusCode = (int)HttpStatusCode.OK;
            try
            {
                IActionResult response = Unauthorized();

                var authorization = Request.Headers[HeaderNames.Authorization];

                string clientId = getClientIdFromToken(authorization.ToString().Replace("Bearer ", ""));

                if (clientId == null)
                {
                    statusCode = (int)HttpStatusCode.Forbidden;
                    message = "Invalid token";
                    return StatusCode((int)HttpStatusCode.Forbidden, new { statusCode, message });
                }

                var user = connection.Query<Users>("SELECT * FROM user WHERE User_ID = @user_id AND Role = 'Super Admin'", new { user_id = clientId }).FirstOrDefault();

                if (user == null)
                {
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = "User not found";
                    return StatusCode((int)HttpStatusCode.NotFound, new { statusCode, message });
                }


                // valida si vienen los campos necesarios para la creacion del hotel
                if (hotel.Name == null || hotel.Address == null || hotel.Phone == null ||
                    hotel.Email == null || String.IsNullOrEmpty(hotel.Hotel_ID.ToString()))
                {
                    statusCode = (int)HttpStatusCode.Forbidden;
                    message = "Incomplete request";
                    return StatusCode((int)HttpStatusCode.Forbidden, new {statusCode, message});

                }

                // validacion para saber si el hotel existe

                var findHotel = connection.Query<string>("SELECT Hotel_ID FROM hotel WHERE Hotel_ID" +
          " = @hotel_id", new { hotel.Hotel_ID }).FirstOrDefault();


                // si no existe, retorna un error
                if (findHotel == null)
                {
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = "Hotel does not exist!";
                    return StatusCode((int)HttpStatusCode.NotFound,new {statusCode, message});
                }

                System.Diagnostics.Debug.WriteLine(findHotel.ToString());

                // validacion para verificar si el email del hotel que se envia ya existe en la base de datos

                var existingHotel = connection.Query<string>("SELECT Email FROM hotel WHERE Email" +
                    "= @email AND Hotel_ID <> @hotel_id", new { hotel.Email, hotel.Hotel_ID }).FirstOrDefault();

                // si ya existe, retorna un error
                if (existingHotel != null)
                {
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = "Hotel email already exists!";
                    return StatusCode((int)HttpStatusCode.NotFound, new {statusCode, message});
                }

                connection.Execute("UPDATE hotel SET Name = @name, Address = @address, Phone = @phone, Email = @email " +
                    "WHERE Hotel_ID = @hotel_id", new { hotel.Name, hotel.Address, hotel.Phone, hotel.Email, hotel.Hotel_ID });

                response = Ok(new {statusCode, message});

                return response;
            }
            catch (Exception e)
            {
                message = "An error has ocurred: " + e.Message;
                statusCode = (int)HttpStatusCode.InternalServerError;
                return StatusCode((int)HttpStatusCode.InternalServerError,new {statusCode, message});
            }

        }

        [Authorize]
        [HttpGet]   
        [Route("get")]
        public IActionResult Get([FromServices] MySqlConnection connection)
        {
            string message = "Hotels retrieved successfully!";
            int statusCode = (int)HttpStatusCode.OK;
            try
            {

                IActionResult response = Unauthorized();

                var hotels = connection.Query<Hotel>("SELECT * FROM hotel" ).ToList();

                response = Ok(new {statusCode, message, hotels});

                return response;
            }
            catch (Exception e)
            {
                message = "An error has ocurred: " + e.Message;
                statusCode = (int)HttpStatusCode.InternalServerError;
                return StatusCode((int)HttpStatusCode.InternalServerError,new {statusCode, message});
            }

        }

    }
}
