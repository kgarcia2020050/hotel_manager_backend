using ApiHoteleria.Dtos;
using ApiHoteleria.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using MySqlConnector;
using System.Collections.Specialized;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text.Json.Nodes;

namespace ApiHoteleria.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ReservationController : ControllerBase
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


        private string getPersonId(string token)
        {

            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(token);
            var tokenS = jsonToken as JwtSecurityToken;
            var claims = tokenS.Claims.Select(claim => (claim.Type, claim.Value)).ToList();
            string userId = "";
            for (int i = 0; i < claims.Count; i++)
            {
                System.Diagnostics.Debug.WriteLine("EL TYPE DEL TOKEN ES " + claims[i].Type);
                if (claims[i].Type == "jti")
                {
                    userId = claims[i].Value;
                }
            }
            System.Diagnostics.Debug.WriteLine("EL PERSON DEL TOKEN ES " + userId);
            return userId;
        }


        [Authorize]
        [HttpGet]
        [Route("getReservationsDetails")]
        public IActionResult getReservationsDetails([FromServices] MySqlConnection connection)
        {
            try
            {
                IActionResult response = Unauthorized();
                var reservatiosnResponse = connection.Query<string>("SELECT JSON_OBJECT( 'Reservation_ID', rs.Reservation_ID, 'Reservation_Status', rs.Status, 'Total_Cost'," +
                    " rs.Total_Cost, 'Details', JSON_ARRAYAGG( JSON_OBJECT( 'Room_Number', r.Room_Number, 'Room_Status', r.Status, 'Type_Description'," +
                    " rt.Description, 'Type_Capacity', rt.Capacity, 'Type_Price_Per_Night', rt.Price_Per_Night, 'Check_In_Date', rd.Check_In_Date, 'Check_Out_Date'," +
                    " rd.Check_Out_Date ) ) ) AS Reservation_Details FROM reservation rs INNER JOIN reservation_detail rd " +
                    "ON rd.Reservation_ID = rs.Reservation_ID INNER JOIN room r ON r.Room_ID = rd.Room_ID INNER JOIN room_type" +
                    " rt ON rt.Type_ID = r.Type_ID INNER JOIN hotel h ON r.Hotel_ID = h.Hotel_ID GROUP BY rs.Reservation_ID");

                if(reservatiosnResponse == null)
                {
                    return NotFound(new { statusCode = (int)HttpStatusCode.NotFound, message = "Reservations not found" });
                }

                response = Ok(new { statusCode = (int)HttpStatusCode.OK, data = reservatiosnResponse });

                return response;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return BadRequest(
                    new
                    {
                        statusCode = (int)HttpStatusCode.BadRequest,
                        message = "An error has ocurred: " + e.Message
                    });
            }
        }

        [Authorize]
        [HttpPost]
        [Route("create")]
        public IActionResult Create([FromBody] Reservations hotel, [FromServices] MySqlConnection connection)
        {
            string message = "Reservation created successfully!";
            int statusCode = (int)HttpStatusCode.OK;

            try
            {
                int reservationId = 0;
                IActionResult response = Unauthorized();

                if (String.IsNullOrEmpty(hotel.Hotel_ID.ToString()) || String.IsNullOrEmpty(hotel.Check_In_Date.ToString()) ||
                     String.IsNullOrEmpty(hotel.Check_In_Date.ToString()) || String.IsNullOrEmpty(hotel.Room_ID.ToString()))
                {
                    statusCode = (int)HttpStatusCode.Forbidden;
                    message = "Incomplete request";
                    return StatusCode((int)HttpStatusCode.Forbidden, new { statusCode, message });
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

                var room = connection.Query<Rooms>("SELECT r.Room_ID, rt.Price_Per_Night FROM room r " +
                    "INNER JOIN room_type rt on r.Type_ID = rt.Type_ID WHERE r.Room_ID = @room_id AND Status = 'Disponible'", new { room_id = hotel.Room_ID }).ToList();

                if (room == null || room.Count == 0)
                {
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = "Room not found";
                    return StatusCode((int)HttpStatusCode.NotFound, new { statusCode, message });
                }

                string personId = getPersonId(authorization.ToString().Replace("Bearer ", ""));

                if (personId == null)
                {
                    statusCode = (int)HttpStatusCode.Forbidden;
                    message = "Invalid token";
                    return StatusCode((int)HttpStatusCode.Forbidden, new { statusCode, message });
                }


                var existingRoom = connection.Query<Reservation>("SELECT r.Status FROM reservation r " +
                    "INNER JOIN room rm ON rm.Hotel_ID = r.Hotel_ID " +
                    "WHERE r.Status = 'Pendiente' AND r.Client_ID = @clientId AND rm.Room_ID = @roomId",new { clientId=personId, roomId = hotel.Room_ID}).ToList();

                if(existingRoom.Count > 0&& existingRoom[0].Status != "Pendiente")
                {
                    message = "Room already reserved";
                    statusCode = (int)HttpStatusCode.Forbidden;
                    return StatusCode((int)HttpStatusCode.Forbidden, new { statusCode, message });
                }




                var userReservation = connection.Query<Reservation>("SELECT * FROM reservation WHERE Client_ID = @client_id AND Status = 'Pendiente'", 
                    new { client_id = personId }).ToList();

                DateTime fechaUno = Convert.ToDateTime(hotel.Check_In_Date).Date;
                DateTime fechaDos = Convert.ToDateTime(hotel.Check_Out_Date).Date;

                TimeSpan difFechas = fechaDos - fechaUno;

                int days = (int)difFechas.TotalDays;

                string dias = Convert.ToString(days);

                double totalCost = room[0].Price_Per_Night * int.Parse(dias);

                if (userReservation == null || userReservation.Count == 0)
                {

                    connection.Execute("INSERT INTO reservation (Hotel_ID, Client_ID, Total_Cost, Status) " +
                        "VALUES (@hotel_id, @client_id, @total_cost, 'Pendiente')",
                    new { hotel.Hotel_ID, client_id = personId, total_cost = totalCost });
                    int userId = connection.QuerySingle<int>("SELECT Reservation_ID FROM reservation order by Reservation_ID DESC LIMIT 1");

                    reservationId = userId;

                }
                else
                {
                    double newCost = userReservation[0].Total_Cost + totalCost;
                    reservationId = userReservation[0].Reservation_ID;
                    message = "Reservation updated successfully!";
                    connection.Execute("UPDATE reservation SET Total_Cost =  @total_cost WHERE Reservation_ID = @reservation_id",
                        new { total_cost = newCost, reservation_id = userReservation[0].Reservation_ID });
                }

                connection.Execute("INSERT INTO reservation_detail (Reservation_ID, Check_In_Date, Check_Out_Date, Room_ID) VALUES (@reservation_id, " +
                    "@check_in_date, @check_out_date, @room_id)",
                                       new { reservation_id = reservationId, hotel.Check_In_Date, hotel.Check_Out_Date, hotel.Room_ID });

                response = Ok(new { statusCode, message, reservationId});

                return response;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                message = "An error has ocurred: " + e.Message;
                statusCode = (int)HttpStatusCode.InternalServerError;
                return StatusCode((int)HttpStatusCode.InternalServerError, new { statusCode, message });
            }

        }





        [Authorize]
        [HttpPut]
        [Route("confirm")]
        public IActionResult Confirm([FromBody] Reservation hotel, [FromServices] MySqlConnection connection)
        {
            string message = "Reservation created successfully!";
            int statusCode = (int)HttpStatusCode.OK;

            try
            {
                IActionResult response = Unauthorized();

                if (String.IsNullOrEmpty(hotel.Reservation_ID.ToString()))
                {
                    statusCode = (int)HttpStatusCode.Forbidden;
                    message = "Incomplete request";
                    return StatusCode((int)HttpStatusCode.Forbidden, new { statusCode, message });
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

                string personId = getPersonId(authorization.ToString().Replace("Bearer ", ""));

                if (personId == null)
                {
                    statusCode = (int)HttpStatusCode.Forbidden;
                    message = "Invalid token";
                    return StatusCode((int)HttpStatusCode.Forbidden, new { statusCode, message });
                }

                var userReservation = connection.Query<Reservation>("SELECT * FROM reservation WHERE Client_ID = @client_id AND Status = 'Pendiente'",
                    new { client_id = personId }).ToList();

                if (userReservation == null || userReservation.Count == 0)
                {
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = "Reservation not found";
                    return StatusCode((int)HttpStatusCode.NotFound, new { statusCode, message });
                }

                var availableRooms = connection.Query<Rooms>("SELECT rd.Check_In_Date, rd.Check_Out_Date, rd.Room_ID, r.Room_Number, r.Status  FROM reservation_detail rd " +
                    "INNER JOIN room r ON r.Room_ID = rd.Room_ID " +
                    "INNER JOIN reservation rv ON rv.Reservation_ID = rd.Reservation_ID " +
                    "INNER JOIN room_type rt ON rt.Type_ID = r.Type_ID  WHERE rv.Client_ID " +
                    "= @user_id AND rv.Reservation_ID = @reservation_id", new { user_id = personId, reservation_id = userReservation[0].Reservation_ID }).ToList();


                if (availableRooms == null || availableRooms.Count == 0)
                {
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = "Rooms not found";
                    return StatusCode((int)HttpStatusCode.NotFound, new { statusCode, message });
                }

                var availableRoomDetails = connection.Query<Rooms>("select r.Room_Number, rd.Check_In_Date, rd.Check_Out_Date FROM room r " +
                    "INNER JOIN reservation_detail rd ON r.Room_ID = r.Room_ID and r.Status = 'Confirmada'").ToList();
                if (availableRoomDetails.Count > 0)
                {
                    for (int i = 0; i < availableRoomDetails.Count; i++)
                    {
                        DateTime checkInDate = availableRoomDetails[i].Check_In_Date;
                        DateTime checkOutDate = availableRoomDetails[i].Check_Out_Date;

                        foreach (var room in availableRooms)
                        {
                            if ((checkInDate < room.Check_Out_Date && checkOutDate > room.Check_In_Date))
                            {
                                statusCode = (int)HttpStatusCode.NotFound;
                                message = "Room " + availableRoomDetails[i].Room_Number + " not available";
                                return StatusCode((int)HttpStatusCode.NotFound, new { statusCode, message });
                            }
                        }
                    }
                }


                for (int i = 0; i < availableRooms.Count; i++)
                {
                    if (availableRooms[i].Status != "Disponible"  )
                    {
                        statusCode = (int)HttpStatusCode.NotFound;
                        message = "Room " + availableRooms[i].Room_Number + " not available";
                        return StatusCode((int)HttpStatusCode.NotFound, new { statusCode, message });
                    }
                }


                connection.Execute("UPDATE reservation SET Status =  @status WHERE Reservation_ID = @reservation_id",
                    new { status = "Confirmada", reservation_id = userReservation[0].Reservation_ID });

                string totalCost = "Costo total de la reservacion: Q" + userReservation[0].Total_Cost.ToString();

                string detail = "";

                for (int i = 0; i < availableRooms.Count; i++)
                {
                    detail += "Habitacion " + availableRooms[i].Room_Number + " \n";
                }

                response = Ok(new { statusCode, message, detail, totalCost });

                return response;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                message = "An error has ocurred: " + e.Message;
                statusCode = (int)HttpStatusCode.InternalServerError;
                return StatusCode((int)HttpStatusCode.InternalServerError, new { statusCode, message });
            }

        }

        [Authorize]
        [HttpDelete]
        [Route("deleteReservation")]
        public IActionResult Delete(int reservation_id, [FromServices] MySqlConnection connection)
        {
            string message = "Reservation deleted successfully!";
            int statusCode = (int)HttpStatusCode.OK;
            try
            {
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

                IActionResult response = Unauthorized();

                if(String.IsNullOrEmpty(reservation_id.ToString()))
                {
                    statusCode = (int)HttpStatusCode.Forbidden;
                    message = "Incomplete request";
                    return StatusCode((int)HttpStatusCode.Forbidden, new { statusCode, message });
                }


               var reservation = connection.Query<Reservations>("SELECT Room_ID FROM reservation_detail WHERE Reservation_ID = @reservation_id", new { reservation_id }).ToList();

                if (reservation.Count > 0)
                {
                       for (int i = 0; i < reservation.Count; i++)
                    {
                        connection.Execute("UPDATE room SET Status = 'Disponible' WHERE Room_ID = @room_id", new { room_id = reservation[i].Room_ID });
                    }
                }

                connection.Execute("DELETE FROM reservation_detail WHERE Reservation_ID = @reservation_id", new { reservation_id });
                connection.Execute("DELETE FROM reservation WHERE Reservation_ID = @reservation_id", new { reservation_id });

                response = Ok(new { statusCode, message });

                return response;
            }
            catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                message = "An error has ocurred: " + e.Message;
                statusCode = (int)HttpStatusCode.InternalServerError;
                return StatusCode((int)HttpStatusCode.InternalServerError, new { statusCode, message });
            }

        }


        [Authorize]
        [HttpDelete]
        [Route("deleteRoom")]
        public IActionResult DeleteRoom(int room_id, [FromServices] MySqlConnection connection)
        {
            string message = "Reservation detail deleted successfully!";
            int statusCode = (int)HttpStatusCode.OK;
            try
            {
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


                string personId = getPersonId(authorization.ToString().Replace("Bearer ", ""));

                if (personId == null)
                {
                    statusCode = (int)HttpStatusCode.Forbidden;
                    message = "Invalid token";
                    return StatusCode((int)HttpStatusCode.Forbidden, new { statusCode, message });
                }

  
                var currentReservation = connection.Query<Reservation>("SELECT * FROM reservation WHERE Client_ID = @personId " +
                    "AND Status = 'Pendiente'", new { personId}).ToList();

                if (currentReservation == null || currentReservation.Count == 0)
                {
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = "Person not found";
                    return StatusCode((int)HttpStatusCode.NotFound, new { statusCode, message });
                }

                var room = connection.Query<Rooms>("SELECT r.*, rt.Capacity, rt.Price_Per_Night, rt.Description as type FROM room r INNER JOIN room_type rt " +
                    "ON rt.Type_ID = r.Type_ID AND Room_ID=@room_id", new { room_id }).ToList();

                var detail = connection.Query<Details>("SELECT * FROM reservation_detail WHERE Reservation_ID = @currentReservation", 
                    new { currentReservation = currentReservation[0].Reservation_ID }).ToList();

                DateTime fechaUno = Convert.ToDateTime(detail[0].Check_In_Date).Date;
                DateTime fechaDos = Convert.ToDateTime(detail[0].Check_Out_Date).Date;
                TimeSpan difFechas = fechaDos - fechaUno;
                int days = (int)difFechas.TotalDays;
                string dias = Convert.ToString(days);
                double totalCost = room[0].Price_Per_Night * int.Parse(dias);
                double totalReservation = currentReservation[0].Total_Cost - totalCost;

                System.Diagnostics.Debug.WriteLine(totalReservation);
                System.Diagnostics.Debug.WriteLine(currentReservation[0].Reservation_ID);

                connection.Execute("DELETE FROM reservation_detail WHERE Reservation_ID = @currentReservation AND Room_ID=@room_id",
                    new { currentReservation = currentReservation[0].Reservation_ID, room_id });
                connection.Execute("UPDATE reservation rv SET Total_Cost = @totalReservation WHERE rv.Reservation_ID = @currentReservation",
                    new { totalReservation, currentReservation = currentReservation[0].Reservation_ID });

                IActionResult response = Ok(new { statusCode, message });
                return response;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                message = "An error has ocurred: " + e.Message;
                statusCode = (int)HttpStatusCode.InternalServerError;
                return StatusCode((int)HttpStatusCode.InternalServerError, new { statusCode, message });
            }

        }


        [Authorize]
        [HttpGet]
        [Route("reservation")]
        public IActionResult Reservation([FromQuery(Name = "client_id")] int client_id, [FromServices] MySqlConnection connection)
        {
            string message = "Reservation fetched successfully!";
            int statusCode = (int)HttpStatusCode.OK;
            try
            {
                System.Diagnostics.Debug.WriteLine("ID ES "+client_id);

                IActionResult response = Unauthorized();

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

                var reservation_detail = new List<Reservation>();
                System.Diagnostics.Debug.WriteLine("ID ES " + client_id);

                string personId = getPersonId(authorization.ToString().Replace("Bearer ", ""));

                if (personId == null)
                {
                    statusCode = (int)HttpStatusCode.Forbidden;
                    message = "Invalid token";
                    return StatusCode((int)HttpStatusCode.Forbidden, new { statusCode, message });
                }


                if (client_id != 0)
                {
                    System.Diagnostics.Debug.WriteLine("OBTIENE ESPECIFICOS");

                    reservation_detail = connection.Query<Reservation>("SELECT * FROM reservation WHERE Client_ID = @client_id", new { client_id }).ToList();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("OBTIENE TODOS");

                    reservation_detail = connection.Query<Reservation>("SELECT * FROM reservation").ToList();
                }

                if (reservation_detail == null || reservation_detail.Count == 0)
                {
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = "Reservations not found";
                    return StatusCode((int)HttpStatusCode.NotFound, new { statusCode, message });
                }


                response = Ok(new { statusCode, message, data = reservation_detail });

                return response;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                message = "An error has ocurred: " + e.Message;
                statusCode = (int)HttpStatusCode.InternalServerError;
                return StatusCode((int)HttpStatusCode.InternalServerError, new { statusCode, message });
            }

        }


        [Authorize]
        [HttpGet]
        [Route("details")]
        public IActionResult details([FromQuery(Name = "reservation_id")] int reservation_id, [FromServices] MySqlConnection connection)
        {
            string message = "Details fetched successfully!";
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

                var user = connection.Query<Users>("SELECT * FROM user WHERE User_ID = @user_id", new { user_id = clientId }).FirstOrDefault();

                if (user == null)
                {
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = "User not found";
                    return StatusCode((int)HttpStatusCode.NotFound, new { statusCode, message });
                }


                var reservation_detail  = new List<Details>();

                if (reservation_id == 0)
                {
                    reservation_detail = connection.Query<Details>("SELECT * FROM reservation_detail").ToList();
                }
                else
                {
                    reservation_detail = connection.Query<Details>("SELECT * FROM reservation_detail WHERE Reservation_ID = @reservation_id", new { reservation_id }).ToList();
                }


                if (reservation_detail == null || reservation_detail.Count == 0)
                {
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = "Details not found";
                    return StatusCode((int)HttpStatusCode.NotFound, new { statusCode, message });
                }


                response = Ok(new { statusCode, message, data = reservation_detail });

                return response;
            }catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                message = "An error has ocurred: " + e.Message;
                statusCode = (int)HttpStatusCode.InternalServerError;
                return StatusCode((int)HttpStatusCode.InternalServerError, new { statusCode, message });
            }

        }


        [Authorize]
        [HttpGet]
        [Route("getExistingReservation")]
        public IActionResult getExistingReservation([FromQuery] int user_id,[FromServices] MySqlConnection connection)
        {
            try
            {
                IActionResult response = Unauthorized();
                var reservatiosnResponse = connection.Query<Reservation>("SELECT * FROM reservation WHERE Client_ID = @clientId " +
                    "AND Status = 'Pendiente'", new {clientId = user_id}).ToList();

                if(reservatiosnResponse.Count > 0)
                {
                    var rooms = connection.Query<Rooms>("SELECT * FROM room r INNER JOIN reservation_detail rd " +
                        "ON rd.Room_ID = r.Room_ID INNER JOIN reservation rs " +
                        "ON rs.Reservation_ID = rd.Reservation_ID WHERE rs.Client_ID = @clientId " +
                        "AND rs.Reservation_ID = @reservationId", new {clientId = user_id, reservationId = reservatiosnResponse[0].Reservation_ID }).ToList();


                    return Ok(new
                    {
                        statusCode = (int)HttpStatusCode.OK,
                        data = reservatiosnResponse,
                        rooms
                    });

                }


                return Ok(new
                {
                    statusCode = (int)HttpStatusCode.OK,
                    data = reservatiosnResponse
                });
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return BadRequest(
                    new
                    {
                        statusCode = (int)HttpStatusCode.BadRequest,
                        message = "An error has ocurred: " + e.Message
                    });
            }
        }


    }
}
