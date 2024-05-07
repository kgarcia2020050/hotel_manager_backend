using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ApiHoteleria.Dtos
{
    public class Reservations
    {
        public int Client_ID { get; set; }
        public int Employee_ID { get; set; }
        public int Hotel_ID { get; set; }
        public DateTime Check_Out_Date { get; set; }
        public DateTime Check_In_Date { get; set; }
        public int Room_ID { get; set; }

        public Reservations()
        {

        }


    }
}
