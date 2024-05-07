﻿using ApiHoteleria.Dtos;
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

namespace ApiHoteleria.Controllers
{
    [Route("api/[controller]")]
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
            for(int i = 0; i < claims.Count; i++)
            {
                if (claims[i].Type == "sub")
                {
                    userId =  claims[i].Value;
                }
            }
            System.Diagnostics.Debug.WriteLine("EL ID DEL TOKEN ES "+userId);
            return userId;
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
                    "INNER JOIN room_type rt on r.Type_ID = rt.Type_ID WHERE r.Room_ID = @room_id", new { room_id = hotel.Room_ID }).ToList();

                if (room == null || room.Count == 0)
                {
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = "Room not found";
                    return StatusCode((int)HttpStatusCode.NotFound, new { statusCode, message });
                }

                var userReservation = connection.Query<Reservation>("SELECT * FROM reservation WHERE Client_ID = @client_id AND Status = 'Pendiente'", new { client_id = clientId }).ToList();

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
                    new { hotel.Hotel_ID, client_id = clientId, total_cost = totalCost });
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

                connection.Execute("INSERT INTO reservation_detail (Reservation_ID, Check_In_Date, Check_Out_Date, Room_ID) VALUES (@reservation_id, @check_in_date, @check_out_date, @room_id)",
                                       new { reservation_id = reservationId, hotel.Check_In_Date, hotel.Check_Out_Date, hotel.Room_ID });

                response = Ok(new { statusCode, message });

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
            string message = "Reservation updated successfully!";
            int statusCode = (int)HttpStatusCode.OK;

            try
            {
                int reservationId = 0;
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

                var userReservation = connection.Query<Reservation>("SELECT * FROM reservation WHERE Client_ID = @client_id AND Status = 'Pendiente'", new { client_id = clientId }).ToList();

                if (userReservation == null || userReservation.Count == 0)
                {
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = "Reservation not found";
                    return StatusCode((int)HttpStatusCode.NotFound, new { statusCode, message });

                }
                    connection.Execute("UPDATE reservation SET Status =  @status WHERE Reservation_ID = @reservation_id",
                        new { status = "Confirmada", reservation_id = userReservation[0].Reservation_ID });

                response = Ok(new { statusCode, message });

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

    }
}