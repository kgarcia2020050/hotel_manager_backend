using ApiHoteleria.Dtos;
using ApiHoteleria.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Collections;
using System.Net;

namespace ApiHoteleria.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class Types : ControllerBase
    {

        [Authorize]
        [HttpGet]
        [Route("")]
        public IActionResult getTypes([FromQuery] int id, [FromServices] MySqlConnection connection)
        {
            string message = "Types fetched successfully!";
            int statusCode = (int)HttpStatusCode.OK;
            try
            {
                IActionResult response = Unauthorized();

                List<RoomTypes> data = null;

                if (id == 0)
                {
                    var types = connection.Query<RoomTypes>("SELECT * FROM room_type").ToList();
                    data = types;

                }
                else
                {
                    var type = connection.Query<RoomTypes>("SELECT * FROM room_type WHERE Type_ID = @id", new { id }).ToList();
                    data = type;
                }


                if (data.Count == 0)
                {
                    message = "No types found";
                    statusCode = (int)HttpStatusCode.NotFound;
                    response = StatusCode((int)HttpStatusCode.NotFound, new { statusCode, message, data });
                    return response;
                }

                response = StatusCode((int)HttpStatusCode.OK, new { statusCode, message, data });
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
        [HttpPost]
        [Route("")]
        public IActionResult create([FromBody] TypesDto type, [FromServices] MySqlConnection connection)
        {
            string message = "Type created successfully!";
            int statusCode = (int)HttpStatusCode.OK;
            try
            {
                IActionResult response = Unauthorized();

                if (type.Description == null || type.Capacity == 0 || type.Price_Per_Night == 0)
                {
                    message = "Please fill all fields";
                    statusCode = (int)HttpStatusCode.PreconditionFailed;
                    response = StatusCode((int)HttpStatusCode.PreconditionFailed, new { statusCode, message });
                    return response;
                }

                connection.Execute("INSERT INTO room_type(Description, Capacity, Price_Per_Night) VALUES (@description, @capacity, @price)",
                    new { description = type.Description, capacity = type.Capacity, price = type.Price_Per_Night });


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
        [Route("")]
        public IActionResult update([FromBody] TypesDto type, [FromServices] MySqlConnection connection)
        {
            string message = "Type updated successfully!";
            int statusCode = (int)HttpStatusCode.OK;
            try
            {
                IActionResult response = Unauthorized();

                if (type.Type_ID == 0)
                {
                    message = "Please provide a type ID";
                    statusCode = (int)HttpStatusCode.PreconditionFailed;
                    response = StatusCode((int)HttpStatusCode.PreconditionFailed, new { statusCode, message });
                }

                var typesFound = connection.Query<RoomTypes>("SELECT * FROM room_type WHERE Type_ID = @id", new { id = type.Type_ID }).ToList();

                if (typesFound.Count == 0)
                {
                    message = "Type not found";
                    statusCode = (int)HttpStatusCode.NotFound;
                    response = StatusCode((int)HttpStatusCode.NotFound, new { statusCode, message });
                    return response;
                }

                connection.Execute("UPDATE room_type SET Description = @description, Capacity = @capacity, Price_Per_Night = @price WHERE Type_ID = @id",
                    new { description = type.Description, capacity = type.Capacity, price = type.Price_Per_Night, id = type.Type_ID });

                response = Ok(new { statusCode, message });


                return response;

            } catch (Exception e)
            {
                statusCode = (int)HttpStatusCode.InternalServerError;
                message = "An error has ocurred: " + e.Message;
                return StatusCode((int)HttpStatusCode.InternalServerError, new { statusCode, message });

            }
        }


        [Authorize]
        [HttpDelete]
        [Route("")]
        public IActionResult delete([FromBody] int id, [FromServices]MySqlConnection connection)
        {
            string message = "Type deleted successfully!";
            int statusCode = (int)HttpStatusCode.OK;
            try
            {
                IActionResult response = Unauthorized();
                var room = connection.Query<Rooms>("SELECT * FROM room WHERE Type_ID = @id", new { id }).ToList();

                if(room.Count > 0)
                {
                    message= "Type is being used by a room";
                    statusCode = (int)HttpStatusCode.PreconditionFailed;
                    response = StatusCode((int)HttpStatusCode.PreconditionFailed, new { statusCode, message });
                }

                connection.Execute("DELETE FROM room_type WHERE Type_ID = @id", new { id });

                response = Ok(new { statusCode, message });

                return response;
            }catch(Exception e)
            {
                statusCode = (int)HttpStatusCode.InternalServerError;
                message = "An error has ocurred: " + e.Message;
                return StatusCode((int)HttpStatusCode.InternalServerError, new { statusCode, message });
            }
        }

    }
}
