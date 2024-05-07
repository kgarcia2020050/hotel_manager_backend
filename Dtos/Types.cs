namespace ApiHoteleria.Dtos
{
    public class Types
    {
        public string Description { get; set; }
        public int Capacity { get; set; }
        public double Price_Per_Night { get; set; }

        public Types()
        {

        }

        public Types(string Description, int Capacity, double Price_Per_Night)
        {
            this.Description = Description;
            this.Capacity = Capacity;
            this.Price_Per_Night = Price_Per_Night;
        }
    }
}
