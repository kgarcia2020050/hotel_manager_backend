using ApiHoteleria.Dtos;
using ApiHoteleria.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Collections;
using System.Net;
using System.Net.NetworkInformation;

namespace ApiHoteleria.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class RoomsController : ControllerBase
    {
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

                if (id == 0)
                {
                    var types = connection.Query<Rooms>("SELECT * FROM room").ToList();
                    data = types;

                }
                else
                {
                    var type = connection.Query<Rooms>("SELECT * FROM room WHERE Room_ID = @id", new { id }).ToList();
                    data = type;
                }


                if (data.Count == 0)
                {
                    message = "No Rooms found";
                    statuscode = (int)HttpStatusCode.NotFound;
                    response = StatusCode((int)HttpStatusCode.NotFound, new { statuscode, message, data });
                    return response;
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


                if (room.Hotel_ID == 0 || room.Type_ID == 0 || room.Room_Number == null || room.Status == null)
                {
                    message = "Please fill all fields";
                    statusCode = (int)HttpStatusCode.PreconditionFailed;
                    response = StatusCode((int)HttpStatusCode.PreconditionFailed, new { statusCode, message });
                    return response;
                }


                var existingHotel = connection.Query<int>("SELECT * FROM hotel WHERE Hotel_ID = @id", new { id = room.Hotel_ID }).FirstOrDefault();

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


                connection.Execute("INSERT INTO room(Hotel_ID, Type_ID, Room_Number, Status) VALUES (@hotel_ID, @type_ID, @room_Number, @status)",
                    new { hotel_ID = room.Hotel_ID,  room.Type_ID,  room.Room_Number,  room.Status });


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



                var existingHotel = connection.Query<int>("SELECT * FROM hotel WHERE Hotel_ID = @id", new { id = room.Hotel_ID }).FirstOrDefault();

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



                connection.Execute("UPDATE room SET Hotel_ID = @hotel_ID, Type_ID = @type_ID, Room_Number = @room_Number , Status = @status WHERE Room_ID = @id",
                    new {  room.Hotel_ID,  room.Type_ID,  room.Room_Number, room.Status, id = room.Room_ID });

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
