using ApiHoteleria.Dtos;
using ApiHoteleria.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using MySqlConnector;
using System.Collections;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.NetworkInformation;

namespace ApiHoteleria.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class RoomsController : ControllerBase
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
        [HttpGet]
        [Route("Get")]

        public IActionResult Get([FromQuery] int id, [FromServices] MySqlConnection connection)
        {
            string message = "Data fetched successfully";
            int statuscode = (int)HttpStatusCode.OK;

            try
            {


                IActionResult response = Unauthorized();

                List<Rooms> data = null;

                var authorization = Request.Headers[HeaderNames.Authorization];

                string clientId = getClientIdFromToken(authorization.ToString().Replace("Bearer ", ""));

                if (clientId == null)
                {
                    statuscode = (int)HttpStatusCode.Forbidden;
                    message = "Invalid token";
                    return StatusCode((int)HttpStatusCode.Forbidden, new { statuscode, message });
                }

                var user = connection.Query<Users>("SELECT * FROM user WHERE User_ID = @user_id", new { user_id = clientId }).FirstOrDefault();

                if (user == null)
                {
                    statuscode = (int)HttpStatusCode.NotFound;
                    message = "User not found";
                    return StatusCode((int)HttpStatusCode.NotFound, new { statuscode, message });
                }


                int hotelId = user.hotel_id;

                if (id == 0)
                {

                    if(hotelId == 0)
                    {
                        var types = connection.Query<Rooms>("SELECT r.*, rt.Capacity, rt.Price_Per_Night FROM room r INNER JOIN room_type rt " +
        "ON rt.Type_ID = r.Type_ID").ToList();
                        data = types;
                    }
                    else
                    {
                        var types = connection.Query<Rooms>("SELECT r.*, rt.Capacity, rt.Price_Per_Night FROM room r INNER JOIN room_type rt " +
            "ON rt.Type_ID = r.Type_ID WHERE r.Hotel_ID = @id",new { id = hotelId }).ToList();
                        data = types;
                    }
                }
                else
                {
                    var type = connection.Query<Rooms>("SELECT r.*, rt.Capacity, rt.Price_Per_Night FROM room r INNER JOIN room_type rt " +
                        "ON rt.Type_ID = r.Type_ID WHERE r.Room_ID = @id", new { id }).ToList();
                    data = type;
                }

                response = Ok(new { statuscode, message, data });
                return response;

            }
            catch (Exception ex)
            {
                message = ex.Message;
                return StatusCode(500, message);
            }

        }

        [Authorize]
        [HttpPost]
        [Route("Create")]
        public IActionResult Create([FromBody] RoomsDto room, [FromServices] MySqlConnection connection)
        {
            string message = "Room created successfully!";
            int statusCode = (int)HttpStatusCode.OK;
            try
            {
                IActionResult response = Unauthorized();


                if (room.Type_ID == 0 || room.Room_Number == null || room.Status == null)
                {
                    message = "Please fill all fields";
                    statusCode = (int)HttpStatusCode.PreconditionFailed;
                    response = StatusCode((int)HttpStatusCode.PreconditionFailed, new { statusCode, message });
                    return response;
                }

                var authorization = Request.Headers[HeaderNames.Authorization];

                string clientId = getClientIdFromToken(authorization.ToString().Replace("Bearer ", ""));

                if (clientId == null)
                {
                    statusCode = (int)HttpStatusCode.Forbidden;
                    message = "Invalid token";
                    return StatusCode((int)HttpStatusCode.Forbidden, new { statusCode, message });
                }

                var user = connection.Query<Users>("SELECT * FROM user WHERE User_ID = @user_id", new { user_id = clientId }).FirstOrDefault();

                if (user == null)
                {
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = "User not found";
                    return StatusCode((int)HttpStatusCode.NotFound, new { statusCode, message });
                }

                var existingHotel = connection.Query<int>("SELECT * FROM hotel WHERE Hotel_ID = @id", new { id = user.hotel_id}).FirstOrDefault();

                if (existingHotel == 0)
                {
                    message = "Hotel not found";
                    statusCode = (int)HttpStatusCode.NotFound;
                    response = StatusCode((int)HttpStatusCode.NotFound, new { statusCode, message });
                    return response;
                }

                var existingType = connection.Query<int>("SELECT * FROM room_type WHERE Type_ID = @id", new { id = room.Type_ID }).FirstOrDefault();

                if (existingType == 0)
                {
                    message = "Room type not found";
                    statusCode = (int)HttpStatusCode.NotFound;
                    response = StatusCode((int)HttpStatusCode.NotFound, new { statusCode, message });
                    return response;
                }

                var existingRoom = connection.Query<int>("SELECT * FROM room WHERE Room_Number = @room_number", new { room_number = room.Room_Number }).FirstOrDefault();

                if (existingRoom != 0)
                {
                    message = "Room already exists";
                    statusCode = (int)HttpStatusCode.PreconditionFailed;
                    response = StatusCode((int)HttpStatusCode.PreconditionFailed, new { statusCode, message });
                    return response;
                }

                connection.Execute("INSERT INTO room(Hotel_ID, Type_ID, Room_Number, Status) VALUES (@hotel_ID, @type_ID, @room_Number, @status)",
                    new { hotel_ID = user.hotel_id,  room.Type_ID,  room.Room_Number,  room.Status });


                response = Ok(new { statusCode, message });

                return response;

            }
            catch (Exception e)
            {
                message = "An error has ocurred: " + e.Message;
                statusCode = (int)HttpStatusCode.InternalServerError;
                return StatusCode((int)HttpStatusCode.InternalServerError, new { statusCode, message });

            }
        }

        [Authorize]
        [HttpPut]
        [Route("Update")]
        public IActionResult update([FromBody] RoomsDto room, [FromServices] MySqlConnection connection)
        {
            string message = "Room updated successfully!";
            int statusCode = (int)HttpStatusCode.OK;
            try
            {
                IActionResult response = Unauthorized();

                if (room.Room_ID == 0)
                {
                    message = "Please provide a Room ID";
                    statusCode = (int)HttpStatusCode.PreconditionFailed;
                    response = StatusCode((int)HttpStatusCode.PreconditionFailed, new { statusCode, message });
                }

                var roomsFound = connection.Query<Rooms>("SELECT * FROM room WHERE Room_ID = @id", new { id = room.Room_ID }).ToList();

                if (roomsFound.Count == 0)
                {
                    message = "Room not found";
                    statusCode = (int)HttpStatusCode.NotFound;
                    response = StatusCode((int)HttpStatusCode.NotFound, new { statusCode, message });
                    return response;
                }

                var existingType = connection.Query<int>("SELECT * FROM room_type WHERE Type_ID = @id", new { id = room.Type_ID }).FirstOrDefault();

                if (existingType == 0)
                {
                    message = "Room type not found";
                    statusCode = (int)HttpStatusCode.NotFound;
                    response = StatusCode((int)HttpStatusCode.NotFound, new { statusCode, message });
                    return response;
                }

                var existingRoom = connection.Query<int>("SELECT * FROM room WHERE Room_Number = @room_number " +
                    "and Room_ID <> @id", new { room_number = room.Room_Number, id = room.Room_ID }).FirstOrDefault();

                if (existingRoom != 0)
                {
                    message = "Room already exists";
                    statusCode = (int)HttpStatusCode.PreconditionFailed;
                    response = StatusCode((int)HttpStatusCode.PreconditionFailed, new { statusCode, message });
                    return response;
                }

                connection.Execute("UPDATE room SET Type_ID = @type_ID, Room_Number = @room_Number , Status = @status WHERE Room_ID = @id",
                    new {  room.Type_ID,  room.Room_Number, room.Status, id = room.Room_ID });

                response = Ok(new { statusCode, message });


                return response;

            }
            catch (Exception e)
            {
                statusCode = (int)HttpStatusCode.InternalServerError;
                message = "An error has ocurred: " + e.Message;
                return StatusCode((int)HttpStatusCode.InternalServerError, new { statusCode, message });

            }
        }

        [Authorize]
        [HttpDelete]
        [Route("Delete")]
        public IActionResult delete([FromBody] int id, [FromServices] MySqlConnection connection)
        {
            string message = "Room deleted successfully!";
            int statusCode = (int)HttpStatusCode.OK;
            try
            {
                IActionResult response = Unauthorized();
                var room = connection.Query<Rooms>("select * from reservation_detail rd inner join " +
                    "reservation r on r.Reservation_ID = rd.Reservation_ID where rd.Room_ID = @id " +
                    "and r.Status <> 'Pendiente' and r.Status <> 'Confirmada'", new { id }).ToList();

                if (room.Count > 0)
                {
                    message = "Rom is being used by a room";
                    statusCode = (int)HttpStatusCode.PreconditionFailed;
                    response = StatusCode((int)HttpStatusCode.PreconditionFailed, new { statusCode, message });
                }

                connection.Execute("DELETE FROM room WHERE Room_ID = @id", new { id });

                response = Ok(new { statusCode, message });

                return response;
            }
            catch (Exception e)
            {
                statusCode = (int)HttpStatusCode.InternalServerError;
                message = "An error has ocurred: " + e.Message;
                return StatusCode((int)HttpStatusCode.InternalServerError, new { statusCode, message });
            }
        }

    }
}
