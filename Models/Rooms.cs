namespace ApiHoteleria.Models
{
    public class Rooms
    {
        public int Room_ID { get; set; }
        public int Hotel_ID { get; set; }
        public int Type_ID { get; set; }
        public string Room_Number { get; set; }
        public string Status { get; set; }
        public double Price_Per_Night { get; set; }

        public Rooms()
        {

        }

        public Rooms(int Room_ID, int Hotel_ID, int Type_ID, string Room_Number, string Status, double price_Per_Night)
        {
            this.Room_ID = Room_ID;
            this.Hotel_ID = Hotel_ID;
            this.Type_ID = Type_ID;
            this.Room_Number = Room_Number;
            this.Status = Status;
            Price_Per_Night = price_Per_Night;
        }
    }
}
