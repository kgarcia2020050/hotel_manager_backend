namespace ApiHoteleria.Models
{
    public class RoomTypes
    {
        public int Type_ID { get; set; }
        public string Description { get; set; }
        public int Capacity { get; set; }
        public double Price_Per_Night { get; set; }

        public RoomTypes()
        {

        }

        public RoomTypes(int Type_ID, string Description, int Capacity, double Price_Per_Night)
        {
            this.Type_ID = Type_ID;
            this.Description = Description;
            this.Capacity = Capacity;
            this.Price_Per_Night = Price_Per_Night;
        }
    }
}
