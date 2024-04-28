namespace ApiHoteleria.Models
{
    public class Hotel
    {
        public int Hotel_ID { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }

        public Hotel(int hotel_ID, string name, string address, string phone, string email)
        {
            Hotel_ID = hotel_ID;
            Name = name;
            Address = address;
            Phone = phone;
            Email = email;
        }

        public Hotel()
        {
        }
    }

}
