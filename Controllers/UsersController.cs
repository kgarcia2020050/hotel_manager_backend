﻿using ApiHoteleria.Dtos;
using ApiHoteleria.Models;
using ApiHoteleria.Services.Interfaces;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace ApiHoteleria.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {

        private IConfiguration _config;

        public UsersController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost]
        [Route("login")]
        public IActionResult Login([FromBody] Login login, [FromServices] MySqlConnection connection)
        {
            string message = "Login success!";
            int statusCode = (int)HttpStatusCode.OK;
            try
            {
                IActionResult response = Unauthorized();

                var existingUser = connection.Query<Users>("SELECT p.Person_ID, u.Role, u.User_ID, u.Username, u.Password, p.Email, h.Hotel_ID as hotel_id, h.Name as hotel_name " +
                    "FROM user u INNER JOIN person p ON p.User_ID = u.User_ID LEFT JOIN hotel h ON h.Hotel_ID = u.Hotel_ID WHERE p.Email " +
                    "= @email", new { login.email }).FirstOrDefault();

                if (existingUser == null)
                {
                    message = "User not found";
                    statusCode = (int)HttpStatusCode.NotFound;
                    return StatusCode((int)HttpStatusCode.NotFound, new {statusCode, message});
                }

                System.Diagnostics.Debug.WriteLine(existingUser.ToString());

                var dataUser = existingUser;

                if (BCrypt.Net.BCrypt.EnhancedVerify(login.password, existingUser.password))
                {
                    var user = AuthenticateUser(existingUser, login);
                    if (user != null)
                    {
                        dataUser.password = null;

                        return Ok(new {statusCode, message, token = GenerateJSONWebToken(existingUser), dataUser });
                    }
                }
                else
                {
                    message = "Invalid credentials";
                    statusCode = (int)HttpStatusCode.Unauthorized;
                    return StatusCode((int)HttpStatusCode.Unauthorized, "Invalid credentials");
                }
                message = "Invalid credentials";
                statusCode = (int)HttpStatusCode.Unauthorized;
                return response;
            }
            catch(Exception e)
            {
                message = "An error has ocurred: " + e.Message;
                statusCode = (int)HttpStatusCode.InternalServerError;
                return StatusCode((int)HttpStatusCode.InternalServerError,new {statusCode, message});
            }
        }



        [HttpPost]
        [Route("register")]
        public IActionResult Register([FromBody] Register login, [FromServices] MySqlConnection connection)
        {
            string message = "User created successfully!";
            int statusCode = (int)HttpStatusCode.OK;
            try
            {
                IActionResult response = Unauthorized();

                var username = connection.Query<string>("SELECT Username FROM User WHERE Username" +
                    "= @username", new { login.username }).FirstOrDefault();
                if (username != null)
                {
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = "Username already exists!";
                    return StatusCode((int)HttpStatusCode.NotFound,new {statusCode, message});
                }

                var userEmail = connection.Query<string>("SELECT Email FROM Person WHERE Email" +
                                   "= @email", new { login.email }).FirstOrDefault();

                if (userEmail != null)
                {
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = "Email already exists!";
                    return StatusCode((int)HttpStatusCode.NotFound, new {statusCode, message});
                }


                if (login.hotel_id != 0)
                {
                    var existingHotel = connection.Query<int>("SELECT Hotel_ID FROM hotel WHERE Hotel_ID" +
                                       "= @hotel_id", new { login.hotel_id }).FirstOrDefault();

                    if (existingHotel == 0)
                    {
                        statusCode = (int)HttpStatusCode.NotFound;
                        message = "Hotel not found!";
                        return StatusCode((int)HttpStatusCode.NotFound, new { statusCode, message });
                    }
                }

                login.password = BCrypt.Net.BCrypt.EnhancedHashPassword(login.password);

                if(login.hotel_id == 0)
                {
                    connection.Execute("INSERT INTO user(Username, Password, Role) VALUES(@username, @password, @role)",
                                           new { login.username, login.password, login.role });
                }
                else
                {
                    connection.Execute("INSERT INTO user(Username, Password, Role, hotel_id) VALUES(@username, @password, @role, @hotel_id)",
                                           new { login.username, login.password, login.role, login.hotel_id });
                }


                int userId = connection.QuerySingle<int>("SELECT User_ID FROM user order by User_ID DESC LIMIT 1");

                connection.Execute("INSERT INTO person(User_ID, Name, Identity_Document, " +
                    "Phone, Email, Address) " +
                    "VALUES(@userId, @name, @identity_document, @phone, @email, @address)"
                    , new { userId, login.name, login.identity_document, login.phone, login.email, login.address });

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


        private string GenerateJSONWebToken(Users userInfo)
        {
            var claims = new[] {
        new Claim(JwtRegisteredClaimNames.UniqueName, userInfo.username),
        new Claim(JwtRegisteredClaimNames.Email, userInfo.email),
        new Claim(JwtRegisteredClaimNames.Sub, userInfo.user_id.ToString()),
        new Claim(JwtRegisteredClaimNames.Jti,userInfo.Person_ID.ToString())
    };  
            var jwtToken = new JwtSecurityToken(
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(60),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(
                       Encoding.UTF8.GetBytes(_config["Jwt:Key"])
                        ),
                    SecurityAlgorithms.HmacSha256Signature)
                );
            return new JwtSecurityTokenHandler().WriteToken(jwtToken);
        }

       private Users AuthenticateUser(Users login, Login person)
        {
            Users user = null;
            user = new Users { username = login.username,user_id=login.user_id, email=person.email };
            return user;
        }



        [Authorize]
        [HttpGet]
        [Route("")]
        public IActionResult getUsers([FromQuery] int hotel_id, [FromServices] MySqlConnection connection)
        {
            string message = "get users successfully!";
            int statusCode = (int)HttpStatusCode.OK;
            try
            {
                IActionResult response = Unauthorized();
                List<Persons> users = new List<Persons>();
                if (hotel_id == 0)
                {
                    users = connection.Query<Persons>("SELECT p.*, u.Role, h.Name as Hotel_Name " +
                        "FROM person p " +
                        "INNER JOIN user u " +
                        "ON u.User_ID = p.User_ID " +
                        "LEFT JOIN hotel h " +
                        "ON h.Hotel_ID = u.hotel_id").ToList();
                }
                else
                {
                    users = connection.Query<Persons>("SELECT p.*, u.Role, h.Name AS Hotel_Name " +
                        "FROM person p " +
                        "INNER JOIN user u " +
                        "ON u.User_ID = p.User_ID " +
                        "INNER JOIN hotel h " +
                        "ON h.Hotel_ID = u.hotel_id WHERE u.hotel_id = @hotel_id", new { hotel_id }).ToList();
                }

                response = Ok(new { statusCode, message, users});

                return response;
            }
            catch (Exception e)
            {
                message = "An error has ocurred: " + e.Message;
                statusCode = (int)HttpStatusCode.InternalServerError;
                return StatusCode((int)HttpStatusCode.InternalServerError, new { statusCode, message });
            }

        }

    }
}
