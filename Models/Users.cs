namespace ApiHoteleria.Models
{
    public class Users
    {

        public string name { get; set; }
        public string email { get; set; }

        public Users()
        {

        }


        public Users(string name, string email)
        {
            this.name = name;
            this.email = email;
        }

    }
}
