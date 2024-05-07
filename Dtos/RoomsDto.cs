namespace ApiHoteleria.Dtos
{
    public class RoomsDto
    {
        public int Hotel_ID { get; set; }
        public int Type_ID { get; set; }
        public string Room_Number { get; set; }
        public string Status { get; set; }

        public RoomsDto()
        {

        }

        public RoomsDto(int Hotel_ID, int Type_ID, string Room_Number, string Status)
        {
            this.Hotel_ID = Hotel_ID;
            this.Type_ID = Type_ID;
            this.Room_Number = Room_Number;
            this.Status = Status;
        }
    }
}
