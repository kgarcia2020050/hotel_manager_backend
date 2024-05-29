namespace ApiHoteleria.Dtos
{
    public class Register
    {
        public string username { get; set; }
        public string password { get; set; }
        public string role { get; set; }
        public string name { get; set; }
        public string identity_document { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public string address { get; set; }
        public int hotel_id { get; set; }

        public Register()
        {

        }

        public Register(int user_id, string username, string password, string role, string name, string identity_document, string phone, string email, string address, int hotel_id)
        {
            this.username = username;
            this.password = password;
            this.role = role;
            this.name = name;
            this.identity_document = identity_document;
            this.phone = phone;
            this.email = email;
            this.address = address;
            this.hotel_id = hotel_id;
        }   
    }
}
