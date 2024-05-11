using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ApiHoteleria.Models
{
    public class Details
    {

public int Reservation_Detail_ID { get; set; }
        public int Reservation_ID { get; set; }
        public int Room_ID { get; set; }
        public DateTime Check_In_Date { get; set; }
        public DateTime Check_Out_Date { get; set; }


        public Details()
        {

        }


        public Details(int Reservation_ID, int Room_ID, DateTime Check_In_Date, DateTime Check_Out_Date)
        {
            this.Reservation_ID = Reservation_ID;
            this.Room_ID = Room_ID;
            this.Check_In_Date = Check_In_Date;
            this.Check_Out_Date = Check_Out_Date;
        }

    }
}
