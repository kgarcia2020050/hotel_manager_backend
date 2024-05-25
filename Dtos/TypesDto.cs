namespace ApiHoteleria.Dtos
{
    public class TypesDto
    {
        public int Type_ID { get; set; }
        public string Description { get; set; }
        public int Capacity { get; set; }
        public double Price_Per_Night { get; set; }

        public TypesDto()
        {

        }

        public TypesDto(string Description, int Capacity, double Price_Per_Night, int Type_ID)
        {
            this.Description = Description;
            this.Capacity = Capacity;
            this.Price_Per_Night = Price_Per_Night;
            this.Type_ID = Type_ID;
        }
    }
}
