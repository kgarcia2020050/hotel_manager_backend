namespace ApiHoteleria.Models
{
    public class Reservation
    {
        public int Reservation_ID { get; set; }
        public int Client_ID { get; set; }
        public int Employee_ID { get; set; }
        public int Hotel_ID { get; set; }
        public double Total_Cost { get; set; }
        public string Status { get; set; }

        public Reservation()
        {

        }

        public Reservation(int Client_ID, int Employee_ID, int Hotel_ID, double Total_Cost, string status)
        {
            this.Client_ID = Client_ID;
            this.Employee_ID = Employee_ID;
            this.Hotel_ID = Hotel_ID;
            this.Total_Cost = Total_Cost;
            this.Status = status;
        }   
    }
}
