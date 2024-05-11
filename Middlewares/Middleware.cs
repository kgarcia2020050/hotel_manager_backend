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

namespace ApiHoteleria.Middlewares
{
    public class Middleware
    {
        private readonly RequestDelegate _next;

        public Middleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context,[FromServices] MySqlConnection connection)
        {
            try
            {
                var authorization = context.Request.Headers[HeaderNames.Authorization];


                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(authorization);
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

                if (userId == null)
                {
                    throw new Exception("User not found");
                }

                var user = connection.Query<Users>("SELECT * FROM user WHERE User_ID = @user_id", new { user_id = userId }).FirstOrDefault();

                if (user == null)
                {
                    throw new Exception("User not found");
                }


                await _next(context);
            }
            catch(Exception e)
            {
                await context.Response.WriteAsJsonAsync(e);
            }
        }

    }
}
