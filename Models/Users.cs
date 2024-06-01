﻿namespace ApiHoteleria.Models
{
    public class Users
    {
        public int user_id  { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string role { get; set; }

        public string email { get; set; }
        public int hotel_id { get; set; }
        public string hotel_name { get; set; }
        public int Person_ID { get; set; }

        public Users()
        {

        }


        public Users(string username, string password, string role, string email, int hotel_id, int person_id)
        {
            this.username = username;
            this.password = password;
            this.role = role;
            this.email = email;
            this.hotel_id = hotel_id;
            this.Person_ID = person_id;
        }

    }
}
